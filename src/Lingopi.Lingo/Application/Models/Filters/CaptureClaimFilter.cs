namespace Lingopi.Lingo.Application.Models.Filters;

public sealed record CaptureClaimFilter(
    DateTime EligibleAt,
    DateTime RunningStartedBefore,
    int MaxAttempts);
