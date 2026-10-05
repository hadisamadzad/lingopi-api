using Lingopi.Lingo.Application.Models.Enums;

namespace Lingopi.Lingo.Api.Models;

public sealed record UserUsageSummaryResponse(
    string UserId,
    UserUsageAccountResponse Account,
    UserUsagePeriodResponse Total,
    UserUsagePeriodResponse LastMonth);

public sealed record UserUsageAccountResponse(
    LingoPlan Plan,
    SubscriptionStatus? SubscriptionStatus,
    DateTime? SubscriptionStartedAt,
    DateTime? SubscriptionExpiresAt,
    string? TargetLocaleCode,
    List<string> SourceLocaleCodes,
    DateTime? SettingsCreatedAt,
    DateTime? SettingsUpdatedAt);

public sealed record UserUsagePeriodResponse(
    long Captures,
    long Lingos,
    long Encounters,
    decimal AverageEncountersPerLingo,
    long Enrichments,
    int InputTokens,
    int OutputTokens,
    decimal EstimatedCost,
    IReadOnlyList<ModelUsageResponse> UsageByModel);

public sealed record ModelUsageResponse(
    string ModelId,
    int InputTokens,
    int OutputTokens,
    decimal EstimatedCost);
