using Lingopi.Core.Persistence.MongoDB;
using Lingopi.Identity.Application.Interfaces.Repositories;
using Lingopi.Identity.Application.Types.Entities;
using MongoDB.Driver;

namespace Lingopi.Identity.Infrastructure.Database.Repositories;

public class UserRepository(IMongoDatabase database, string collectionName) :
    MongoDbRepositoryBase<UserEntity>(database, collectionName), IUserRepository
{
    public async Task<bool> AnyAsync()
    {
        return await _collection.Find(x => true).AnyAsync();
    }

    public async Task<UserEntity> GetByIdAsync(string id)
    {
        return await _collection.Find(x => x.Id == id).SingleOrDefaultAsync();
    }

    public async Task<UserEntity> GetByEmailAsync(string email)
    {
        email = email.ToLower();
        return await _collection.Find(x => x.Email.ToLower() == email).SingleOrDefaultAsync();
    }
}
