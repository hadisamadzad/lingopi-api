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
        DateTime periodStart,
        DateTime periodEnd)
    {
        var records = await _collection
            .Find(record =>
                record.UserId == userId &&
                record.OccurredAt >= periodStart &&
                record.OccurredAt < periodEnd)
            .ToListAsync();

        return new UsageSummary(
            records.Count(record => record.UsageType == UsageType.Enrichment),
            records.Sum(record => record.InputTokens),
            records.Sum(record => record.OutputTokens),
            records.Sum(record => record.EstimatedCost ?? 0m));
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
}
