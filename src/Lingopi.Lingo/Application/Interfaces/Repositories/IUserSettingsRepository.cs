using Lingopi.Core.Interfaces;
using Lingopi.Lingo.Application.Models.Entities;

namespace Lingopi.Lingo.Application.Interfaces.Repositories;

public interface IUserSettingsRepository : IRepository<UserSettingsEntity>
{
    Task<UserSettingsEntity?> GetByUserIdAsync(string userId);
    Task<bool> UpsertAsync(UserSettingsEntity settings);
    Task EnsureIndexesAsync(CancellationToken cancellationToken = default);
}
