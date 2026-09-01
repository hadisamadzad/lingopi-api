using Lingopi.Core.Persistence.MongoDB;
using Lingopi.Lingo.Application.Interfaces.Repositories;
using Lingopi.Lingo.Application.Models.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Lingopi.Lingo.Infrastructure.Database.Repositories;

public class LingoRepository(IMongoDatabase database) :
    MongoDbRepositoryBase<LingoEntity>(database, "lingo.lingos"), ILingoRepository
{
    private const string EmbeddingVectorIndexName = "lingo_embedding_vector";
    private const int EmbeddingDimensions = 1536;
    private const int MaxSimilarLingoResults = 5;

    public async Task<LingoEntity?> GetByIdAsync(string lingoId)
    {
        return await _collection
            .Find(l => l.Id == lingoId)
            .FirstOrDefaultAsync();
    }

    public async Task<LingoEntity?> GetByCaptureIdAsync(string captureId)
    {
        return await _collection
            .Find(lingo => lingo.Encounters.Any(encounter => encounter.CaptureId == captureId))
            .FirstOrDefaultAsync();
    }

    public async Task<List<LingoEntity>> GetByUserIdAsync(string userId)
    {
        return await _collection
            .Find(l => l.UserId == userId)
            .ToListAsync();
    }

    public async Task<List<LingoEntity>> GetTopSimilarByEmbeddingAsync(
        string userId,
        string sourceLanguageCode,
        string targetLocaleCode,
        IReadOnlyList<float> embedding,
        CancellationToken cancellationToken = default)
    {
        var eligibleFilter = Builders<LingoEntity>.Filter.And(
            Builders<LingoEntity>.Filter.Eq(lingo => lingo.UserId, userId),
            Builders<LingoEntity>.Filter.Eq(lingo => lingo.SourceLanguageCode, sourceLanguageCode),
            Builders<LingoEntity>.Filter.Eq(lingo => lingo.TargetLocaleCode, targetLocaleCode),
            Builders<LingoEntity>.Filter.Exists(lingo => lingo.Embedding!.Vector));

        var eligibleCount = await _collection.CountDocumentsAsync(eligibleFilter, cancellationToken: cancellationToken);
        if (eligibleCount == 0)
        {
            return [];
        }

        var limit = (int)Math.Min(eligibleCount, MaxSimilarLingoResults);
        var options = new VectorSearchOptions<LingoEntity>
        {
            Filter = Builders<LingoEntity>.Filter.And(
                Builders<LingoEntity>.Filter.Eq(lingo => lingo.UserId, userId),
                Builders<LingoEntity>.Filter.Eq(lingo => lingo.SourceLanguageCode, sourceLanguageCode),
                Builders<LingoEntity>.Filter.Eq(lingo => lingo.TargetLocaleCode, targetLocaleCode)),
            IndexName = EmbeddingVectorIndexName,
            NumberOfCandidates = (int)Math.Min(eligibleCount, int.MaxValue)
        };

        return await _collection
            .Aggregate()
            .VectorSearch(
                lingo => lingo.Embedding!.Vector,
                new QueryVector(embedding.ToArray()),
                limit,
                options)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> AppendEncounterIfMissingAsync(string lingoId, EncounterValue encounter, DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<LingoEntity>.Filter.And(
            Builders<LingoEntity>.Filter.Eq(lingo => lingo.Id, lingoId),
            Builders<LingoEntity>.Filter.Not(
                Builders<LingoEntity>.Filter.ElemMatch(
                    lingo => lingo.Encounters,
                    existing => existing.CaptureId == encounter.CaptureId)));
        var updates = new List<UpdateDefinition<LingoEntity>>
        {
            Builders<LingoEntity>.Update
            .AddToSet(lingo => lingo.Encounters, encounter)
            .Set(lingo => lingo.Audit.UpdatedAt, updatedAt)
            .Inc(lingo => lingo.Audit.Version, 1)
        };
        if (!string.IsNullOrWhiteSpace(encounter.SourceLocaleCode))
        {
            updates.Add(
                Builders<LingoEntity>.Update.AddToSet(
                    lingo => lingo.SourceLocaleCodes,
                    encounter.SourceLocaleCode));
        }
        if (!string.IsNullOrWhiteSpace(encounter.SourceLanguageCode))
        {
            updates.Add(
                Builders<LingoEntity>.Update.Set(
                    lingo => lingo.SourceLanguageCode,
                    encounter.SourceLanguageCode));
        }

        var update = Builders<LingoEntity>.Update.Combine(updates);
        var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.IsAcknowledged && result.MatchedCount == 1;
    }

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        await _collection.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<LingoEntity>(
                    Builders<LingoEntity>.IndexKeys.Ascending(lingo => lingo.UserId),
                    new CreateIndexOptions { Name = "lingo_user_id" }),
                new CreateIndexModel<LingoEntity>(
                    Builders<LingoEntity>.IndexKeys
                        .Ascending(lingo => lingo.UserId)
                        .Ascending(lingo => lingo.SourceLocaleCodes)
                        .Ascending(lingo => lingo.TargetLocaleCode)
                        .Ascending(lingo => lingo.Expression),
                    new CreateIndexOptions { Name = "lingo_user_locale_expression" }),
                new CreateIndexModel<LingoEntity>(
                    Builders<LingoEntity>.IndexKeys.Ascending("Encounters.CaptureId"),
                    new CreateIndexOptions { Name = "lingo_encounter_capture_id" })
            ],
            cancellationToken);

        await EnsureEmbeddingVectorIndexAsync(cancellationToken);
    }

    private async Task EnsureEmbeddingVectorIndexAsync(CancellationToken cancellationToken)
    {
        var indexCursor = await _collection.SearchIndexes
            .ListAsync(cancellationToken: cancellationToken);
        var indexes = await indexCursor.ToListAsync(cancellationToken);

        var existingIndex = indexes.FirstOrDefault(index =>
            index.TryGetValue("name", out var nameValue) &&
            nameValue.IsString &&
            nameValue.AsString == EmbeddingVectorIndexName);
        if (existingIndex is not null)
        {
            if (!HasFilterField(existingIndex, nameof(LingoEntity.SourceLanguageCode)))
            {
                await _collection.SearchIndexes.UpdateAsync(
                    EmbeddingVectorIndexName,
                    CreateEmbeddingVectorIndexDefinition(),
                    cancellationToken);
            }

            return;
        }

        var indexModel = new CreateVectorSearchIndexModel<LingoEntity>(
            lingo => lingo.Embedding!.Vector,
            EmbeddingVectorIndexName,
            VectorSimilarity.Cosine,
            EmbeddingDimensions,
            lingo => lingo.UserId,
            lingo => lingo.SourceLanguageCode,
            lingo => lingo.TargetLocaleCode
            );

        await _collection.SearchIndexes.CreateOneAsync(indexModel, cancellationToken);
    }

    private static bool HasFilterField(BsonDocument index, string fieldName)
    {
        if (!index.TryGetValue("latestDefinition", out var definitionValue) ||
            !definitionValue.IsBsonDocument ||
            !definitionValue.AsBsonDocument.TryGetValue("fields", out var fieldsValue) ||
            !fieldsValue.IsBsonArray)
        {
            return false;
        }

        return fieldsValue.AsBsonArray.Any(fieldValue =>
            fieldValue.IsBsonDocument &&
            fieldValue.AsBsonDocument.TryGetValue("type", out var typeValue) &&
            typeValue.IsString &&
            typeValue.AsString == "filter" &&
            fieldValue.AsBsonDocument.TryGetValue("path", out var pathValue) &&
            pathValue.IsString &&
            pathValue.AsString == fieldName);
    }

    private static BsonDocument CreateEmbeddingVectorIndexDefinition() =>
        new()
        {
            {
                "fields",
                new BsonArray
                {
                    new BsonDocument
                    {
                        { "type", "vector" },
                        { "path", "Embedding.Vector" },
                        { "numDimensions", EmbeddingDimensions },
                        { "similarity", "cosine" }
                    },
                    new BsonDocument
                    {
                        { "type", "filter" },
                        { "path", nameof(LingoEntity.UserId) }
                    },
                    new BsonDocument
                    {
                        { "type", "filter" },
                        { "path", nameof(LingoEntity.SourceLanguageCode) }
                    },
                    new BsonDocument
                    {
                        { "type", "filter" },
                        { "path", nameof(LingoEntity.TargetLocaleCode) }
                    }
                }
            }
        };
}
