using Lingopi.Core.Helpers;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Interfaces.Services;
using Lingopi.Lingo.Application.Models.Configs;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.Services;
using Microsoft.Extensions.Options;

namespace Lingopi.Lingo.Infrastructure.Usage;

public sealed class EnrichmentUsageService(
    IRepositoryManager repository,
    IOptions<LingoEntitlementOptions> options,
    ILogger<EnrichmentUsageService> logger) : IEnrichmentUsageService
{
    private readonly LingoEntitlementOptions _options = options.Value;

    public async Task<EnrichmentAuthorization> AuthorizeAsync(string userId, DateTime now,
        CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanAsync(userId, now);
        var limits = _options.GetLimits(plan);
        var (periodStart, periodEnd) = GetCurrentPeriod(now);
        var usage = await repository.Usage.GetSummaryAsync(userId, periodStart, periodEnd);

        if (limits.MonthlyLingoLimit is { } lingoLimit &&
            usage.EnrichmentCount >= lingoLimit)
        {
            return new EnrichmentAuthorization(
                false,
                "monthly_enrichment_limit_reached",
                $"The '{plan}' plan allows {lingoLimit} enrichments per month.");
        }

        if (limits.MonthlyCostLimit is { } costLimit &&
            usage.EstimatedCost >= costLimit)
        {
            return new EnrichmentAuthorization(
                false,
                "monthly_cost_limit_reached",
                $"The '{plan}' plan allows provider usage up to {costLimit:0.####} USD per month.");
        }

        return new EnrichmentAuthorization(true, null, null);
    }

    public async Task<bool> RecordAsync(EnrichmentJobEntity job, TranslationResult translation, DateTime occurredAt)
    {
        var record = new UsageRecordEntity
        {
            Id = UidHelper.GenerateNewId("usage"),
            UserId = job.UserId,
            UsageType = TokenUsageType.Enrichment,
            LingoId = job.LingoId,
            EnrichmentJobId = job.Id,

            Provider = "openai",
            TrackingId = translation.RequestId,
            Model = translation.Model,
            PromptVersion = translation.PromptVersion,
            InputTokens = translation.InputTokens,
            OutputTokens = translation.OutputTokens,
            EstimatedCost = translation.EstimatedCost,
            OccurredAt = occurredAt
        };

        var recorded = await repository.Usage.RecordAsync(record);
        if (!recorded)
        {
            logger.LogError("Failed to record usage for user {UserId}, enrichment job {JobId}, and request {RequestId}.",
                job.UserId, job.Id, translation.RequestId);
        }

        return recorded;
    }

    private async Task<LingoPlan> GetPlanAsync(string userId, DateTime now)
    {
        var subscription = await repository.Subscriptions.GetByUserIdAsync(userId);
        if (subscription is null ||
            subscription.Status != SubscriptionStatus.Active ||
            subscription.StartedAt > now ||
            (subscription.ExpiresAt is { } expiresAt && expiresAt <= now))
        {
            return _options.DefaultPlan;
        }

        return subscription.Plan;
    }

    private static (DateTime Start, DateTime End) GetCurrentPeriod(DateTime now)
    {
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return (start, start.AddMonths(1));
    }
}
