using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.ReadModels;

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

public static class UserUsageSummaryResponseMapper
{
    public static UserUsageSummaryResponse ToResponse(this UserUsageSummaryModel model) =>
        new(
            model.UserId,
            new UserUsageAccountResponse(
                model.Account.Plan,
                model.Account.SubscriptionStatus,
                model.Account.SubscriptionStartedAt,
                model.Account.SubscriptionExpiresAt,
                model.Account.TargetLocaleCode,
                model.Account.SourceLocaleCodes,
                model.Account.SettingsCreatedAt,
                model.Account.SettingsUpdatedAt),
            model.Total.ToResponse(),
            model.LastMonth.ToResponse());

    private static UserUsagePeriodResponse ToResponse(this UserUsagePeriodModel model) =>
        new(
            model.Captures,
            model.Lingos,
            model.Encounters,
            model.AverageEncountersPerLingo,
            model.Enrichments,
            model.InputTokens,
            model.OutputTokens,
            model.EstimatedCost,
            model.UsageByModel
                .Select(usage => new ModelUsageResponse(
                    usage.ModelId,
                    usage.InputTokens,
                    usage.OutputTokens,
                    usage.EstimatedCost))
                .ToArray());
}
