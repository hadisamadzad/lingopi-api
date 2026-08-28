using Lingopi.Core.Persistence.MongoDB;
using Lingopi.Lingo.Application.Interfaces.Repositories;
using Lingopi.Lingo.Application.Models.Entities;
using MongoDB.Driver;

namespace Lingopi.Lingo.Infrastructure.Database.Repositories;

public sealed class SubscriptionRepository(IMongoDatabase database) :
    MongoDbRepositoryBase<SubscriptionEntity>(database, "lingo.subscriptions"),
    ISubscriptionRepository
{
    public async Task<SubscriptionEntity?> GetByUserIdAsync(string userId)
    {
        return await _collection
            .Find(subscription => subscription.UserId == userId)
            .FirstOrDefaultAsync();
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
