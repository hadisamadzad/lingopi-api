using Lingopi.Core.Persistence.MongoDB;
using Lingopi.Lingo.Application.Interfaces.Repositories;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.Filters;
using MongoDB.Driver;

namespace Lingopi.Lingo.Infrastructure.Database.Repositories;

public sealed class CaptureRepository(IMongoDatabase database) :
    MongoDbRepositoryBase<CaptureEntity>(database, "lingo.captures"),
    ICaptureRepository
{
    public async Task<CaptureEntity?> GetByIdAsync(string captureId)
    {
        return await _collection.Find(capture => capture.Id == captureId).FirstOrDefaultAsync();
    }

    public async Task<CaptureEntity?> ClaimNextAndUpdateAsync(CaptureClaimFilter filter,
        CancellationToken cancellationToken = default)
    {
        var builder = Builders<CaptureEntity>.Filter;

        var stages = new[]
        {
            (CaptureAnalysisStatus.AnalysisQueued, CaptureAnalysisStatus.AnalysisRunning),
            (CaptureAnalysisStatus.EmbeddingQueued, CaptureAnalysisStatus.EmbeddingRunning),
            (CaptureAnalysisStatus.ResolutionQueued, CaptureAnalysisStatus.ResolutionRunning),
            (CaptureAnalysisStatus.AnalysisRunning, CaptureAnalysisStatus.AnalysisRunning),
            (CaptureAnalysisStatus.EmbeddingRunning, CaptureAnalysisStatus.EmbeddingRunning),
            (CaptureAnalysisStatus.ResolutionRunning, CaptureAnalysisStatus.ResolutionRunning)
        };

        foreach (var (availableStatus, claimedStatus) in stages)
        {
            var availableFilter = builder.And(
                builder.Eq(capture => capture.Status, availableStatus),
                builder.Lt(capture => capture.Audit.AttemptCount, filter.MaxAttempts),
                availableStatus is CaptureAnalysisStatus.AnalysisRunning
                    or CaptureAnalysisStatus.EmbeddingRunning
                    or CaptureAnalysisStatus.ResolutionRunning
                    ? builder.And(
                        builder.Ne(capture => capture.Audit.StartedAt, null),
                        builder.Lte(capture => capture.Audit.StartedAt, filter.RunningStartedBefore))
                    : builder.Or(
                        builder.Eq(capture => capture.Audit.NextAttemptAt, null),
                        builder.Lte(capture => capture.Audit.NextAttemptAt, filter.EligibleAt)));

            var update = Builders<CaptureEntity>.Update
                .Set(capture => capture.Status, claimedStatus)
                .Set(capture => capture.Audit.StartedAt, filter.EligibleAt)
                .Set(capture => capture.Audit.UpdatedAt, filter.EligibleAt)
                .Set(capture => capture.Audit.CompletedAt, null)
                .Set(capture => capture.Audit.NextAttemptAt, null)
                .Set(capture => capture.Error, null);

            var result = await _collection.FindOneAndUpdateAsync(
                availableFilter,
                update,
                new FindOneAndUpdateOptions<CaptureEntity>
                {
                    ReturnDocument = ReturnDocument.After,
                    Sort = Builders<CaptureEntity>.Sort
                        .Ascending(capture => capture.Audit.NextAttemptAt)
                        .Ascending(capture => capture.Audit.StartedAt)
                        .Ascending(capture => capture.Audit.CreatedAt)
                },
                cancellationToken);

            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        await _collection.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<CaptureEntity>(
                    Builders<CaptureEntity>.IndexKeys
                        .Ascending(capture => capture.Status)
                        .Ascending(capture => capture.Audit.NextAttemptAt)
                        .Ascending(capture => capture.Audit.CreatedAt),
                    new CreateIndexOptions { Name = "capture_status_next_attempt_created" }),
                new CreateIndexModel<CaptureEntity>(
                    Builders<CaptureEntity>.IndexKeys
                        .Ascending(capture => capture.Status)
                        .Ascending(capture => capture.Audit.AttemptCount)
                        .Ascending(capture => capture.Audit.NextAttemptAt)
                        .Ascending(capture => capture.Audit.CreatedAt),
                    new CreateIndexOptions { Name = "capture_claim_queued" }),
                new CreateIndexModel<CaptureEntity>(
                    Builders<CaptureEntity>.IndexKeys
                        .Ascending(capture => capture.Status)
                        .Ascending(capture => capture.Audit.AttemptCount)
                        .Ascending(capture => capture.Audit.StartedAt)
                        .Ascending(capture => capture.Audit.CreatedAt),
                    new CreateIndexOptions { Name = "capture_claim_running" })
            ],
            cancellationToken);
    }
}
