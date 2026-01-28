using Lingopi.Core.Interfaces;
using Lingopi.Lingo.Application.Models.Entities;

namespace Lingopi.Lingo.Application.Interfaces.Repositories;

public interface ILanguageRepository : IRepository<LanguageEntity>
{
    Task<LanguageEntity?> GetByIdAsync(string langId);
    Task<List<LanguageEntity>> GetActiveLanguagesAsync();
    Task<bool> ExistsByCodeAsync(string code);
}
