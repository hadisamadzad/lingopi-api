using Lingopi.Core.Persistence.MongoDB;
using Lingopi.Lingo.Application.Interfaces.Repositories;
using Lingopi.Lingo.Application.Models.Entities;
using MongoDB.Driver;

namespace Lingopi.Lingo.Infrastructure.Database.Repositories;

public class LanguageRepository(IMongoDatabase database)
    : MongoDbRepositoryBase<LanguageEntity>(database, "lingo.languages"), ILanguageRepository
{
    public async Task<LanguageEntity?> GetByIdAsync(string langId)
    {
        var filter = Builders<LanguageEntity>.Filter.Eq(x => x.Id, langId);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<List<LanguageEntity>> GetActiveLanguagesAsync()
    {
        var filter = Builders<LanguageEntity>.Filter.Eq(x => x.IsActive, true);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<bool> ExistsByCodeAsync(string code)
    {
        var filter = Builders<LanguageEntity>.Filter.Eq(x => x.Code, code);
        return await _collection.Find(filter).AnyAsync();
    }
}
