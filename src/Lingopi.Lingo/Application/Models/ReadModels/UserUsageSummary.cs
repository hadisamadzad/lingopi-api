using Lingopi.Lingo.Application.Models.Enums;

namespace Lingopi.Lingo.Application.Models.ReadModels;

public sealed record UserUsageSummaryModel(
    string UserId,
    UserUsageAccountModel Account,
    UserUsagePeriodModel Total,
    UserUsagePeriodModel LastMonth);

public sealed record UserUsageAccountModel(
    LingoPlan Plan,
    SubscriptionStatus? SubscriptionStatus,
    DateTime? SubscriptionStartedAt,
    DateTime? SubscriptionExpiresAt,
    string? TargetLocaleCode,
    List<string> SourceLocaleCodes,
    DateTime? SettingsCreatedAt,
    DateTime? SettingsUpdatedAt);

public sealed record UserUsagePeriodModel(
    long Captures,
    long Lingos,
    long Encounters,
    decimal AverageEncountersPerLingo,
    long Enrichments,
    int InputTokens,
    int OutputTokens,
    decimal EstimatedCost,
    IReadOnlyList<ModelUsageSummary> UsageByModel);
