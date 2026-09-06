using Lingopi.Core.Persistence.MongoDB;
using Lingopi.Identity.Application.Interfaces.Repositories;
using Lingopi.Identity.Application.Types.Entities;
using MongoDB.Driver;

namespace Lingopi.Identity.Infrastructure.Database.Repositories;

public sealed class SubscriptionRepository(IMongoDatabase database) :
    MongoDbRepositoryBase<SubscriptionEntity>(database, "identity.subscriptions"),
    ISubscriptionRepository
{
    public async Task<SubscriptionEntity?> GetByUserIdAsync(string userId)
    {
        return await _collection
            .Find(subscription => subscription.UserId == userId)
            .FirstOrDefaultAsync();
    }

    public async Task<SubscriptionEntity?> MarkExpiredAsync(string userId, DateTime now)
    {
        var update = Builders<SubscriptionEntity>.Update
            .Set(subscription => subscription.Status, SubscriptionStatus.Expired)
            .Set(subscription => subscription.UpdatedAt, now);

        return await _collection.FindOneAndUpdateAsync(
            subscription =>
                subscription.UserId == userId &&
                subscription.Status == SubscriptionStatus.Active &&
                subscription.ExpiresAt != null &&
                subscription.ExpiresAt <= now,
            update,
            new FindOneAndUpdateOptions<SubscriptionEntity>
            {
                ReturnDocument = ReturnDocument.Before
            });
    }

    public async Task<bool> UpsertAsync(SubscriptionEntity subscription)
    {
        var result = await _collection.ReplaceOneAsync(
            item => item.UserId == subscription.UserId,
            subscription,
            new ReplaceOptions { IsUpsert = true });
        return result.IsAcknowledged;
    }

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        await _collection.Indexes.CreateOneAsync(
            new CreateIndexModel<SubscriptionEntity>(
                Builders<SubscriptionEntity>.IndexKeys.Ascending(subscription => subscription.UserId),
                new CreateIndexOptions
                {
                    Name = "subscription_user_id",
                    Unique = true
                }),
            cancellationToken: cancellationToken);
    }
}
