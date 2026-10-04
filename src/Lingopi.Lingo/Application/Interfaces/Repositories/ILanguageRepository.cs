using Lingopi.Lingo.Application.Models.Entities;

namespace Lingopi.Lingo.Application.Interfaces.Repositories;

public interface ILanguageRepository : IRepository<LanguageEntity>
{
    Task<LanguageEntity?> GetByIdAsync(string langId);
    Task<List<LanguageEntity>> GetAllAsync();
    Task<List<LanguageEntity>> GetActiveLanguagesAsync();
    Task<bool> ExistsByLocaleCodeAsync(string localeCode, string? excludedId = null);
    Task<bool> IsActiveLocaleAsync(string localeCode);
    Task EnsureIndexesAsync(CancellationToken cancellationToken = default);
}
