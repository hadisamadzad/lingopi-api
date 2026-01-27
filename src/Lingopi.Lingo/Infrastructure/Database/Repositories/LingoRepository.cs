using Lingopi.Core.Persistence.MongoDB;
using Lingopi.Lingo.Application.Interfaces.Repositories;
using Lingopi.Lingo.Application.Models.Entities;
using MongoDB.Driver;

namespace Lingopi.Lingo.Infrastructure.Database.Repositories;

public class LingoRepository(IMongoDatabase database) :
    MongoDbRepositoryBase<LingoEntity>(database, "lingo.lingos"), ILingoRepository
{
    public async Task<LingoEntity?> GetByIdAsync(string lingoId)
    {
        return await _collection
            .Find(l => l.Id == lingoId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<LingoEntity>> GetByUserIdAsync(string userId)
    {
        return await _collection
            .Find(l => l.UserId == userId)
            .ToListAsync();
    }
}
