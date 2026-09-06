using Lingopi.Identity.Application.Types.Entities;

namespace Lingopi.Identity.Application.Interfaces.Repositories;

public interface ISubscriptionHistoryRepository
{
    Task<List<SubscriptionHistoryEntity>> GetByUserIdAsync(string userId);
    Task InsertAsync(SubscriptionHistoryEntity history);
    Task EnsureIndexesAsync(CancellationToken cancellationToken = default);
}
