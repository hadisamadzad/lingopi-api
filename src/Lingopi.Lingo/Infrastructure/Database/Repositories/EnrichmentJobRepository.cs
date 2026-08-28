using Lingopi.Core.Persistence.MongoDB;
using Lingopi.Lingo.Application.Interfaces.Repositories;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.Filters;
using MongoDB.Driver;

namespace Lingopi.Lingo.Infrastructure.Database.Repositories;

public class EnrichmentJobRepository(IMongoDatabase database) :
    MongoDbRepositoryBase<EnrichmentJobEntity>(database, "lingo.enrichment-jobs"),
    IEnrichmentJobRepository
{
    public async Task<EnrichmentJobEntity?> GetByIdAsync(string jobId)
    {
        return await _collection
            .Find(job => job.Id == jobId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<EnrichmentJobEntity>> GetByLingoIdAsync(string lingoId)
    {
        return await _collection
            .Find(job => job.LingoId == lingoId)
            .ToListAsync();
    }

    public async Task<EnrichmentJobEntity?> ClaimNextAndUpdateAsync(EnrichmentJobClaimFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var builder = Builders<EnrichmentJobEntity>.Filter;
        var queuedFilter = builder.And(
            builder.Eq(job => job.Status, JobProcessingStatus.Queued),
            builder.Lt(job => job.AttemptCount, filter.MaxAttempts),
            builder.Or(
                builder.Eq(job => job.NextAttemptAt, (DateTime?)null),
                builder.Lte(job => job.NextAttemptAt, filter.EligibleAt)));

        var staleRunningFilter = builder.And(
            builder.Eq(job => job.Status, JobProcessingStatus.Running),
            builder.Lt(job => job.AttemptCount, filter.MaxAttempts),
            builder.Ne(job => job.StartedAt, (DateTime?)null),
            builder.Lte(job => job.StartedAt, filter.RunningStartedBefore));

        var update = Builders<EnrichmentJobEntity>.Update
            .Set(job => job.Status, JobProcessingStatus.Running)
            .Set(job => job.StartedAt, filter.EligibleAt)
            .Set(job => job.UpdatedAt, filter.EligibleAt)
            .Set(job => job.CompletedAt, null)
            .Set(job => job.NextAttemptAt, null)
            .Set(job => job.ErrorCode, null)
            .Set(job => job.ErrorMessage, null);

        var options = new FindOneAndUpdateOptions<EnrichmentJobEntity>
        {
            ReturnDocument = ReturnDocument.After,
            Sort = Builders<EnrichmentJobEntity>.Sort
                .Ascending(job => job.NextAttemptAt)
                .Ascending(job => job.StartedAt)
                .Ascending(job => job.CreatedAt)
        };

        return await _collection.FindOneAndUpdateAsync(
            builder.Or(queuedFilter, staleRunningFilter),
            update,
            options,
            cancellationToken);
    }

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        await _collection.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<EnrichmentJobEntity>(
                    Builders<EnrichmentJobEntity>.IndexKeys.Ascending(job => job.LingoId),
                    new CreateIndexOptions { Name = "enrichment_job_lingo_id" }),
                new CreateIndexModel<EnrichmentJobEntity>(
                    Builders<EnrichmentJobEntity>.IndexKeys
                        .Ascending(job => job.Status)
                        .Ascending(job => job.AttemptCount)
                        .Ascending(job => job.NextAttemptAt)
                        .Ascending(job => job.CreatedAt),
                    new CreateIndexOptions { Name = "enrichment_job_claim_queued" }),
                new CreateIndexModel<EnrichmentJobEntity>(
                    Builders<EnrichmentJobEntity>.IndexKeys
                        .Ascending(job => job.Status)
                        .Ascending(job => job.AttemptCount)
                        .Ascending(job => job.StartedAt)
                        .Ascending(job => job.CreatedAt),
                    new CreateIndexOptions { Name = "enrichment_job_claim_running" })
            ],
            cancellationToken);
    }
}
