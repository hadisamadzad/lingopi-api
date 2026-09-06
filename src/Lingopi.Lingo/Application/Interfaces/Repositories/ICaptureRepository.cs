using Lingopi.Core.Interfaces;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Filters;

namespace Lingopi.Lingo.Application.Interfaces.Repositories;

public interface ICaptureRepository : IRepository<CaptureEntity>
{
    Task<CaptureEntity?> GetByIdAsync(string captureId);
    Task<long> CountByUserIdAsync(
        string userId,
        DateTime? periodStart = null,
        DateTime? periodEnd = null);
    Task<CaptureEntity?> ClaimNextAndUpdateAsync(
        CaptureClaimFilter filter,
        CancellationToken cancellationToken = default);
    Task EnsureIndexesAsync(CancellationToken cancellationToken = default);
}
