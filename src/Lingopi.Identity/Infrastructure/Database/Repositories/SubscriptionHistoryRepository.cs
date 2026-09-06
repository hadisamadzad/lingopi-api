using Lingopi.Core.Persistence.MongoDB;
using Lingopi.Identity.Application.Interfaces.Repositories;
using Lingopi.Identity.Application.Types.Entities;
using MongoDB.Driver;

namespace Lingopi.Identity.Infrastructure.Database.Repositories;

public sealed class SubscriptionHistoryRepository(IMongoDatabase database) :
    MongoDbRepositoryBase<SubscriptionHistoryEntity>(database, "identity.subscription-history"),
    ISubscriptionHistoryRepository
{
    public async Task<List<SubscriptionHistoryEntity>> GetByUserIdAsync(string userId)
    {
        return await _collection
            .Find(history => history.UserId == userId)
            .SortByDescending(history => history.RecordedAt)
            .ThenByDescending(history => history.Id)
            .ToListAsync();
    }

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        await _collection.Indexes.CreateOneAsync(
            new CreateIndexModel<SubscriptionHistoryEntity>(
                Builders<SubscriptionHistoryEntity>.IndexKeys
                    .Ascending(history => history.UserId)
                    .Descending(history => history.RecordedAt),
                new CreateIndexOptions
                {
                    Name = "subscription_history_user_recorded_at"
                }),
            cancellationToken: cancellationToken);
    }
}
