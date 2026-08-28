using System.Text.RegularExpressions;
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

    public async Task<List<LingoEntity>> GetByCanonicalExpressionAsync(
        string userId,
        string sourceLocaleCode,
        string targetLocaleCode,
        string canonicalExpression)
    {
        var expressionPattern = $"^{Regex.Escape(canonicalExpression.Trim())}$";
        return await _collection
            .Find(Builders<LingoEntity>.Filter.And(
                Builders<LingoEntity>.Filter.Eq(lingo => lingo.UserId, userId),
                Builders<LingoEntity>.Filter.AnyEq(lingo => lingo.SourceLocaleCodes, sourceLocaleCode),
                Builders<LingoEntity>.Filter.Eq(lingo => lingo.TargetLocaleCode, targetLocaleCode),
                Builders<LingoEntity>.Filter.Regex(
                    lingo => lingo.Expression,
                    new BsonRegularExpression(expressionPattern, "i"))))
            .ToListAsync();
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

        if (indexes.Any(index =>
                index.TryGetValue("name", out var nameValue) &&
                nameValue.IsString &&
                nameValue.AsString == EmbeddingVectorIndexName))
        {
            return;
        }

        var indexModel = new CreateVectorSearchIndexModel<LingoEntity>(
            lingo => lingo.Embedding!.Vector,
            EmbeddingVectorIndexName,
            VectorSimilarity.Cosine,
            EmbeddingDimensions,
            [lingo => lingo.UserId]);

        await _collection.SearchIndexes.CreateOneAsync(indexModel, cancellationToken);
    }
}
