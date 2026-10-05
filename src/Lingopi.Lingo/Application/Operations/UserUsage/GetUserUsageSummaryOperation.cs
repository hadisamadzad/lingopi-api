using Lingopi.Lingo.Application.Extensions.Mappers;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Interfaces.Services;
using Lingopi.Lingo.Application.Models.Configs;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.ReadModels;
using Microsoft.Extensions.Options;

namespace Lingopi.Lingo.Application.Operations.UserUsage;

public sealed class GetUserUsageSummaryOperation(
    IRepositoryManager repository,
    TimeProvider timeProvider,
    IIdentityEntitlementClient identityEntitlementClient,
    IOptions<LingoEntitlementOptions> entitlementOptions) :
    IOperation<GetUserUsageSummaryCommand, UserUsageSummaryModel>
{
    public async Task<OperationResult<UserUsageSummaryModel>> ExecuteAsync(
        GetUserUsageSummaryCommand command,
        CancellationToken? cancellation = null)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            return OperationResult<UserUsageSummaryModel>.ValidationFailure("UserId is required.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var lastMonthDate = now.AddMonths(-1);
        var lastMonthStart = new DateTime(
            lastMonthDate.Year,
            lastMonthDate.Month,
            lastMonthDate.Day,
            0,
            0,
            0,
            DateTimeKind.Utc);

        var total = GetPeriodAsync(command.UserId, null, null);
        var lastMonth = GetPeriodAsync(command.UserId, lastMonthStart, now);
        var settingsTask = repository.UserSettings.GetByUserIdAsync(command.UserId);

        var subscription = await identityEntitlementClient.GetAsync(
            command.UserId,
            cancellation ?? CancellationToken.None);

        await Task.WhenAll(total, lastMonth, settingsTask);

        var entity = settingsTask.Result;
        var plan = LingoPlan.Free;
        SubscriptionStatus? subscriptionStatus = null;
        DateTime? subscriptionStartedAt = null;
        DateTime? subscriptionExpiresAt = null;

        if (subscription.Status != OperationStatus.Completed ||
            subscription.Value is null)
        {
            return OperationResult<UserUsageSummaryModel>.Failure(
                subscription.Error?.Messages?.FirstOrDefault() ??
                "Unable to load subscription entitlement.");
        }

        var entitlement = subscription.Value;
        var isActive = entitlement.SubscriptionStatus == SubscriptionStatus.Active &&
            (entitlement.SubscriptionStartedAt is null || entitlement.SubscriptionStartedAt <= now) &&
            (entitlement.SubscriptionExpiresAt is null || entitlement.SubscriptionExpiresAt > now);
        plan = isActive ? entitlement.Plan : entitlementOptions.Value.DefaultPlan;
        subscriptionStatus = entitlement.SubscriptionStatus;
        subscriptionStartedAt = entitlement.SubscriptionStartedAt;
        subscriptionExpiresAt = entitlement.SubscriptionExpiresAt;

        var model = entity.ToModel(
            plan,
            subscriptionStatus,
            subscriptionStartedAt,
            subscriptionExpiresAt);

        return OperationResult<UserUsageSummaryModel>.Success(
            model.ToModel(command.UserId, total.Result, lastMonth.Result));
    }

    private async Task<UserUsagePeriodModel> GetPeriodAsync(
        string userId,
        DateTime? periodStart,
        DateTime? periodEnd)
    {
        var captures = repository.Captures.CountByUserIdAsync(userId, periodStart, periodEnd);
        var lingos = repository.Lingos.CountByUserIdAsync(userId, periodStart, periodEnd);
        var encounters = repository.Lingos.CountEncountersByUserIdAsync(userId, periodStart, periodEnd);
        var usage = repository.Usage.GetSummaryAsync(userId, periodStart, periodEnd);

        await Task.WhenAll(captures, lingos, encounters, usage);

        var lingoCount = lingos.Result;
        return usage.Result.MapToUserUsagePeriodModel(captures.Result, lingoCount, encounters.Result);
    }
}

public sealed record GetUserUsageSummaryCommand(string UserId) : IOperationCommand<UserUsageSummaryModel>;
