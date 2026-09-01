using Lingopi.Core.Helpers;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Interfaces.Services;
using Lingopi.Lingo.Application.Models.Configs;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.Services;
using Microsoft.Extensions.Options;
using Minimals.Operations;

namespace Lingopi.Lingo.Infrastructure.Usage;

public sealed class EnrichmentUsageService(
    IRepositoryManager repository,
    IOptions<LingoEntitlementOptions> options,
    ILogger<EnrichmentUsageService> logger,
    IIdentityEntitlementClient identityEntitlementClient) : IEntitlementService
{
    private readonly LingoEntitlementOptions _options = options.Value;

    public async Task<EnrichmentAuthorization> AuthorizeAsync(string userId, DateTime now,
        CancellationToken cancellationToken = default)
    {
        var planResult = await GetPlanAsync(userId, now, cancellationToken);
        if (planResult.Status != OperationStatus.Completed)
        {
            return new EnrichmentAuthorization(
                false,
                "entitlement_unavailable",
                planResult.Error?.Messages?.FirstOrDefault() ??
                $"Unable to evaluate entitlements for user '{userId}'.");
        }

        var plan = planResult.Value;
        var limits = _options.GetLimits(plan);
        var (periodStart, periodEnd) = GetCurrentPeriod(now);
        var lingoCountTask = repository.Lingos.CountByUserIdAsync(
            userId, periodStart, periodEnd);
        var usage = await repository.Usage.GetSummaryAsync(userId, periodStart, periodEnd);
        var lingoCount = await lingoCountTask;

        if (limits.MonthlyLingoLimit is { } lingoLimit &&
            lingoCount >= lingoLimit)
        {
            return new EnrichmentAuthorization(
                false,
                "monthly_lingo_limit_reached",
                $"The '{plan}' plan allows {lingoLimit} lingos per month.");
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

    public async Task<EnrichmentAuthorization> AuthorizeCaptureAsync(
        string userId,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var planResult = await GetPlanAsync(userId, now, cancellationToken);
        if (planResult.Status != OperationStatus.Completed)
        {
            return new EnrichmentAuthorization(
                false,
                "entitlement_unavailable",
                planResult.Error?.Messages?.FirstOrDefault() ??
                $"Unable to evaluate entitlements for user '{userId}'.");
        }

        var plan = planResult.Value;
        var limits = _options.GetLimits(plan);
        var (periodStart, periodEnd) = GetCurrentPeriod(now);
        var lingoCount = await repository.Lingos.CountByUserIdAsync(
            userId, periodStart, periodEnd);

        if (limits.MonthlyLingoLimit is { } lingoLimit &&
            lingoCount >= lingoLimit)
        {
            return new EnrichmentAuthorization(
                false,
                "monthly_lingo_limit_reached",
                $"The '{plan}' plan allows {lingoLimit} lingos per month.");
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

    private async Task<OperationResult<LingoPlan>> GetPlanAsync(
        string userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var entitlement = await identityEntitlementClient.GetAsync(userId, cancellationToken);
        if (entitlement.Status != OperationStatus.Completed)
        {
            return OperationResult<LingoPlan>.Failure(
                entitlement.Error?.Messages?.FirstOrDefault() ?? "Entitlement lookup failed.");
        }

        var value = entitlement.Value!;
        var isActive = value.SubscriptionStatus == SubscriptionStatus.Active &&
            (value.SubscriptionStartedAt is null || value.SubscriptionStartedAt <= now) &&
            (value.SubscriptionExpiresAt is null || value.SubscriptionExpiresAt > now);

        return OperationResult<LingoPlan>.Success(
            isActive ? value.Plan : _options.DefaultPlan);
    }

    private static (DateTime Start, DateTime End) GetCurrentPeriod(DateTime now)
    {
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return (start, start.AddMonths(1));
    }
}
