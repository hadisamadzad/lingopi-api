namespace Lingopi.Lingo.Application.Interfaces.Services;

public interface IEntitlementService : IEnrichmentUsageService
{
    Task<EnrichmentAuthorization> AuthorizeCaptureAsync(
        string userId,
        DateTime now,
        CancellationToken cancellationToken = default);
}
