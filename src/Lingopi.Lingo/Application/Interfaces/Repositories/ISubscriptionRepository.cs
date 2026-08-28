using Lingopi.Core.Interfaces;
using Lingopi.Lingo.Application.Models.Entities;

namespace Lingopi.Lingo.Application.Interfaces.Repositories;

public interface ISubscriptionRepository : IRepository<SubscriptionEntity>
{
    Task<SubscriptionEntity?> GetByUserIdAsync(string userId);
    Task EnsureIndexesAsync(CancellationToken cancellationToken = default);
}
