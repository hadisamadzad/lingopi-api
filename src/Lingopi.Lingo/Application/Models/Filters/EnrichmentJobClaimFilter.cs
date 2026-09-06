namespace Lingopi.Lingo.Application.Models.Filters;

public sealed record EnrichmentJobClaimFilter(
    DateTime EligibleAt,
    DateTime RunningStartedBefore,
    int MaxAttempts);
