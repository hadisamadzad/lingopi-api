using Lingopi.Core.Persistence.MongoDB;
using Lingopi.Lingo.Application.Interfaces.Repositories;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.ReadModels;
using MongoDB.Driver;

namespace Lingopi.Lingo.Infrastructure.Database.Repositories;

public sealed class UsageRepository(IMongoDatabase database) :
    MongoDbRepositoryBase<UsageRecordEntity>(database, "lingo.usage"),
    IUsageRepository
{
    public async Task<UsageSummary> GetSummaryAsync(
        string userId,
        DateTime? periodStart = null,
        DateTime? periodEnd = null)
    {
        var filter = Builders<UsageRecordEntity>.Filter.Eq(record => record.UserId, userId);
        if (periodStart is { } start)
        {
            filter &= Builders<UsageRecordEntity>.Filter.Gte(record => record.OccurredAt, start);
        }

        if (periodEnd is { } end)
        {
            filter &= Builders<UsageRecordEntity>.Filter.Lt(record => record.OccurredAt, end);
        }

        var records = await _collection
            .Find(filter)
            .ToListAsync();

        return new UsageSummary(
            records.Count(record => record.UsageType == TokenUsageType.Enrichment),
            records.Sum(record => record.InputTokens),
            records.Sum(record => record.OutputTokens),
            records.Sum(record => record.EstimatedCost ?? 0m),
            records
                .GroupBy(record => record.Model)
                .Select(group => new ModelUsageSummary(
                    group.Key,
                    group.Sum(record => record.InputTokens),
                    group.Sum(record => record.OutputTokens),
                    group.Sum(record => record.EstimatedCost ?? 0m)))
                .OrderByDescending(model => model.EstimatedCost)
                .ThenBy(model => model.ModelId)
                .ToArray());
    }

    public async Task<bool> RecordAsync(UsageRecordEntity record)
    {
        var existing = await _collection
            .Find(item => item.Id == record.Id)
            .FirstOrDefaultAsync();
        if (existing is not null)
        {
            return true;
        }

        try
        {
            await _collection.InsertOneAsync(record);
            return true;
        }
        catch (MongoWriteException exception) when (
            exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return true;
        }
    }

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        await _collection.Indexes.CreateOneAsync(
            new CreateIndexModel<UsageRecordEntity>(
                Builders<UsageRecordEntity>.IndexKeys
                    .Ascending(record => record.UserId)
                    .Ascending(record => record.OccurredAt),
                new CreateIndexOptions { Name = "usage_user_occurred_at" }),
            cancellationToken: cancellationToken);
    }
}
