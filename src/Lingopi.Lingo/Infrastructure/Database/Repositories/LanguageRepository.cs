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

    public async Task<List<LanguageEntity>> GetAllAsync()
    {
        return await _collection.Find(_ => true).ToListAsync();
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

    public async Task<bool> ExistsByCodeAsync(string code, string excludedId)
    {
        var filter = Builders<LanguageEntity>.Filter.And(
            Builders<LanguageEntity>.Filter.Eq(x => x.Code, code),
            Builders<LanguageEntity>.Filter.Ne(x => x.Id, excludedId));
        return await _collection.Find(filter).AnyAsync();
    }

    public async Task<bool> ExistsByLocaleCodeAsync(string localeCode, string? excludedLanguageId = null)
    {
        var filter = Builders<LanguageEntity>.Filter.ElemMatch(
            x => x.Locales,
            locale => locale.Code == localeCode);

        if (!string.IsNullOrWhiteSpace(excludedLanguageId))
        {
            filter = Builders<LanguageEntity>.Filter.And(
                filter,
                Builders<LanguageEntity>.Filter.Ne(x => x.Id, excludedLanguageId));
        }

        return await _collection.Find(filter).AnyAsync();
    }

    public async Task<bool> IsActiveLocaleAsync(string localeCode)
    {
        var languages = await GetActiveLanguagesAsync();
        return languages.Count == 0 || languages.Any(language =>
            language.Locales.Any(locale =>
                locale.IsActive &&
                string.Equals(locale.Code, localeCode, StringComparison.OrdinalIgnoreCase)));
    }

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        await _collection.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<LanguageEntity>(
                Builders<LanguageEntity>.IndexKeys.Ascending(x => x.Code),
                new CreateIndexOptions { Name = "language_code", Unique = true }),
            new CreateIndexModel<LanguageEntity>(
                Builders<LanguageEntity>.IndexKeys.Ascending("locales.code"),
                new CreateIndexOptions { Name = "language_locale_code" })
        ], cancellationToken);
    }
}
