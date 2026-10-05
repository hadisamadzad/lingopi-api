using Lingopi.Core.Persistence.MongoDB;
using Lingopi.Identity.Application.Interfaces.Repositories;
using Lingopi.Identity.Application.Types.Entities;
using Lingopi.Identity.Application.Types.Models.Users;
using Lingopi.Identity.Infrastructure.Database.Extensions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Lingopi.Identity.Infrastructure.Database.Repositories;

public class UserRepository(IMongoDatabase database, string collectionName) :
    MongoDbRepositoryBase<UserEntity>(database, collectionName), IUserRepository
{
    public async Task<bool> AnyAsync()
    {
        return await _collection.Find(x => true).AnyAsync();
    }

    public async Task<UserEntity?> GetByIdAsync(string id)
    {
        return await _collection.Find(x => x.Id == id).SingleOrDefaultAsync();
    }

    public async Task<UserEntity?> GetByEmailAsync(string email)
    {
        email = email.ToLower();
        return await _collection.Find(x => x.Email.ToLower() == email).SingleOrDefaultAsync();
    }

    public async Task<List<UserEntity>> GetByFilterAsync(UserFilter filter)
    {
        var query = _collection.AsQueryable()
            .ApplyFilter(filter)
            .ApplySort(filter.SortBy);

        if (filter.HasPagination)
        {
            query = query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize);
        }

        return await query.ToListAsync();
    }

    public async Task<long> CountByFilterAsync(UserFilter filter)
    {
        var query = _collection.AsQueryable().ApplyFilter(filter);
        return await query.LongCountAsync();
    }
}
