using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.ReadModels;

namespace Lingopi.Lingo.Application.Extensions.Mappers;

public static class UserUsageSummaryModelMapperExtension
{
    public static UserUsageSummaryModel ToModel(this UserUsageAccountModel account,
        string userId,
        UserUsagePeriodModel total,
        UserUsagePeriodModel lastMonth)
    {
        return new UserUsageSummaryModel(
            UserId: userId,
            Account: account,
            Total: total,
            LastMonth: lastMonth);
    }

    public static UserUsageAccountModel ToModel(this UserSettingsEntity? settings,
        LingoPlan plan,
        SubscriptionStatus? subscriptionStatus,
        DateTime? subscriptionStartedAt,
        DateTime? subscriptionExpiresAt)
    {
        return new UserUsageAccountModel(
            Plan: plan,
            SubscriptionStatus: subscriptionStatus,
            SubscriptionStartedAt: subscriptionStartedAt,
            SubscriptionExpiresAt: subscriptionExpiresAt,
            TargetLocaleCode: settings?.TargetLocaleCode,
            SourceLocaleCodes: settings?.SourceLocaleCodes ?? [],
            SettingsCreatedAt: settings?.CreatedAt,
            SettingsUpdatedAt: settings?.UpdatedAt);
    }

    public static UserUsagePeriodModel MapToUserUsagePeriodModel(
        this UsageSummary usage,
        long captures,
        long lingos,
        long encounters)
    {
        return new UserUsagePeriodModel(
            Captures: captures,
            Lingos: lingos,
            Encounters: encounters,
            AverageEncountersPerLingo: lingos == 0 ? 0m : (decimal)encounters / lingos,
            Enrichments: usage.EnrichmentCount,
            InputTokens: usage.InputTokens,
            OutputTokens: usage.OutputTokens,
            EstimatedCost: usage.EstimatedCost,
            UsageByModel: usage.Models);
    }
}
