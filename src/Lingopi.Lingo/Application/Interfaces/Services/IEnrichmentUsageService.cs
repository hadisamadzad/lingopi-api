using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Services;

namespace Lingopi.Lingo.Application.Interfaces.Services;

public interface IEnrichmentUsageService
{
    Task<EnrichmentAuthorization> AuthorizeAsync(string userId, DateTime now, CancellationToken cancellationToken = default);

    Task<bool> RecordAsync(EnrichmentJobEntity job, TranslationResult translation, DateTime occurredAt);
}

public sealed record EnrichmentAuthorization(bool IsAllowed, string? ErrorCode, string? ErrorMessage);
