using Lingopi.Core.Persistence.MongoDB;
using Lingopi.Lingo.Application.Helpers;
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

    public async Task<bool> ExistsByLocaleCodeAsync(string localeCode, string? excludedId = null)
    {
        var filter = Builders<LanguageEntity>.Filter.Eq(x => x.LocaleCode, localeCode);

        var hasExcludedId = !string.IsNullOrWhiteSpace(excludedId);
        if (hasExcludedId)
        {
            filter = Builders<LanguageEntity>.Filter.And(
                filter,
                Builders<LanguageEntity>.Filter.Ne(x => x.Id, excludedId));
        }

        return await _collection.Find(filter).AnyAsync();
    }

    public async Task<bool> IsActiveLocaleAsync(string localeCode)
    {
        var activeLocales = await GetActiveLanguagesAsync();
        if (activeLocales.Count == 0)
        {
            return true;
        }

        var normalizedLocaleCode = LocaleCodeNormalizer.NormalizeCanonical(localeCode);
        return activeLocales.Any(locale =>
            string.Equals(locale.LocaleCode, normalizedLocaleCode, StringComparison.OrdinalIgnoreCase));
    }

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        var indexCursor = await _collection.Indexes.ListAsync(cancellationToken);
        var existingIndexes = await indexCursor.ToListAsync(cancellationToken);
        foreach (var index in existingIndexes)
        {
            var indexName = index["name"].AsString;
            var isObsoleteIndex = indexName is
                "language_code" or "language_locale_code" or "language_code_region";
            if (isObsoleteIndex)
            {
                await _collection.Indexes.DropOneAsync(indexName, cancellationToken);
            }
        }

        await _collection.Indexes.CreateOneAsync(
            new CreateIndexModel<LanguageEntity>(
                Builders<LanguageEntity>.IndexKeys.Ascending(language => language.LocaleCode),
                new CreateIndexOptions
                {
                    Name = "locale_code",
                    Unique = true
                }),
            cancellationToken: cancellationToken);
    }
}
