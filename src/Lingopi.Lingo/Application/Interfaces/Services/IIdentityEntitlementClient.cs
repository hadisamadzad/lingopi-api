using Lingopi.Lingo.Application.Models.Services;
using Minimals.Operations;

namespace Lingopi.Lingo.Application.Interfaces.Services;

public interface IIdentityEntitlementClient
{
    Task<OperationResult<IdentityEntitlement>> GetAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
