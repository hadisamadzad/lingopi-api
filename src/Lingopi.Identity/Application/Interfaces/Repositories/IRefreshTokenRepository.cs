using Lingopi.Core.Interfaces;
using Lingopi.Identity.Application.Types.Entities;

namespace Lingopi.Identity.Application.Interfaces.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshTokenEntity>
{
    /// <summary>Atomically consumes an unexpired token, preventing replay.</summary>
    Task<RefreshTokenEntity?> ConsumeAsync(string tokenHash, DateTime now, string replacementTokenId);
    Task<bool> RevokeAsync(string tokenHash, DateTime now);
}
