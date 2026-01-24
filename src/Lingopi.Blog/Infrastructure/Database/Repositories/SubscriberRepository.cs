using Lingopi.Blog.Application.Interfaces.Repositories;
using Lingopi.Blog.Application.Types.Entities;
using Lingopi.Core.Persistence.MongoDB;
using MongoDB.Driver;

namespace Lingopi.Blog.Infrastructure.Database.Repositories;

public class SubscriberRepository(IMongoDatabase database, string collectionName) :
    MongoDbRepositoryBase<SubscriberEntity>(database, collectionName), ISubscriberRepository
{
    public async Task<SubscriberEntity> GetByEmailAsync(string email)
    {
        return await _collection.Find(x => x.Email == email).SingleOrDefaultAsync();
    }
}
