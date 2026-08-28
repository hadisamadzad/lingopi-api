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
}
