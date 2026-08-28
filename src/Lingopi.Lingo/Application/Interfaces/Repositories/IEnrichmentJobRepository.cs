using Lingopi.Core.Interfaces;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Filters;

namespace Lingopi.Lingo.Application.Interfaces.Repositories;

public interface IEnrichmentJobRepository : IRepository<EnrichmentJobEntity>
{
    Task<EnrichmentJobEntity?> GetByIdAsync(string jobId);
    Task<List<EnrichmentJobEntity>> GetByLingoIdAsync(string lingoId);
    Task<EnrichmentJobEntity?> ClaimNextAndUpdateAsync(
        EnrichmentJobClaimFilter filter,
        CancellationToken cancellationToken = default);
}
