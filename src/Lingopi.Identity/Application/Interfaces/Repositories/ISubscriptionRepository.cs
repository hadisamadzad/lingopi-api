using Lingopi.Identity.Application.Types.Entities;

namespace Lingopi.Identity.Application.Interfaces.Repositories;

public interface ISubscriptionRepository
{
    Task<SubscriptionEntity?> GetByUserIdAsync(string userId);
    Task<SubscriptionEntity?> MarkExpiredAsync(string userId, DateTime now);
    Task<bool> UpsertAsync(SubscriptionEntity subscription);
    Task EnsureIndexesAsync(CancellationToken cancellationToken = default);
}
