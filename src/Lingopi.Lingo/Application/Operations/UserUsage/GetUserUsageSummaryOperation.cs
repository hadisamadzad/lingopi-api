using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.ReadModels;
using Minimals.Operations;

namespace Lingopi.Lingo.Application.Operations.UserUsage;

public sealed class GetUserUsageSummaryOperation(
    IRepositoryManager repository,
    TimeProvider timeProvider) : IOperation<GetUserUsageSummaryCommand, UserUsageSummaryModel>
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
        var subscription = repository.Subscriptions.GetByUserIdAsync(command.UserId);
        var settings = repository.UserSettings.GetByUserIdAsync(command.UserId);

        await Task.WhenAll(total, lastMonth, subscription, settings);

        var subscriptionValue = subscription.Result;
        var settingsValue = settings.Result;
        var account = new UserUsageAccountModel(
            subscriptionValue?.Plan ?? LingoPlan.Free,
            subscriptionValue?.Status,
            subscriptionValue?.StartedAt,
            subscriptionValue?.ExpiresAt,
            settingsValue?.TargetLocaleCode,
            settingsValue?.SourceLocaleCodes ?? [],
            settingsValue?.CreatedAt,
            settingsValue?.UpdatedAt);

        return OperationResult<UserUsageSummaryModel>.Success(
            new UserUsageSummaryModel(
                command.UserId,
                account,
                total.Result,
                lastMonth.Result));
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
        return new UserUsagePeriodModel(
            captures.Result,
            lingoCount,
            encounters.Result,
            lingoCount == 0 ? 0m : (decimal)encounters.Result / lingoCount,
            usage.Result.EnrichmentCount,
            usage.Result.InputTokens,
            usage.Result.OutputTokens,
            usage.Result.EstimatedCost,
            usage.Result.Models);
    }
}

public sealed record GetUserUsageSummaryCommand(string UserId) : IOperationCommand;
