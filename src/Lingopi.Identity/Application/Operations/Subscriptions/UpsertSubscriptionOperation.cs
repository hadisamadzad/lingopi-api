using System.Security.Cryptography;
using System.Text;
using Lingopi.Core.Helpers;
using Lingopi.Identity.Application.Helpers;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Types.Configs;
using Lingopi.Identity.Application.Types.Entities;
using Lingopi.Identity.Application.Types.Models.Subscriptions;
using Minimals.Operations;

namespace Lingopi.Identity.Application.Operations.Subscriptions;

public sealed class UpsertSubscriptionOperation(
    IRepositoryManager repository,
    IConfiguration configuration,
    TimeProvider timeProvider) :
    IOperation<UpsertSubscriptionCommand, SubscriptionModel>
{
    public async Task<OperationResult<SubscriptionModel>> ExecuteAsync(
        UpsertSubscriptionCommand command,
        CancellationToken? cancellation = null)
    {
        var isAuthorized = IsAuthorized(command.InternalAuthSecret);
        if (!isAuthorized)
        {
            return OperationResult<SubscriptionModel>.AuthorizationFailure("Invalid internal authentication.");
        }

        var isPlanDefined = Enum.IsDefined(command.Plan);
        var isStatusDefined = Enum.IsDefined(command.Status);
        if (!isPlanDefined || !isStatusDefined)
        {
            return OperationResult<SubscriptionModel>.ValidationFailure("Subscription plan or status is invalid.");
        }

        var paymentsEnabled = PaymentGatewayConfig.IsEnabled(configuration);
        var isPaidPlan = command.Plan != SubscriptionPlan.Free;
        if (paymentsEnabled && isPaidPlan)
        {
            return OperationResult<SubscriptionModel>.Failure(
                "Paid subscriptions require a payment gateway checkout.");
        }

        var hasStartDate = command.StartedAt != default;
        if (!hasStartDate)
        {
            return OperationResult<SubscriptionModel>.ValidationFailure("Subscription start date is required.");
        }

        var hasUserId = !string.IsNullOrWhiteSpace(command.UserId);
        if (!hasUserId)
        {
            return OperationResult<SubscriptionModel>.ValidationFailure("UserId is required.");
        }

        if (command.ExpiresAt is { } expiresAt && expiresAt <= command.StartedAt)
        {
            return OperationResult<SubscriptionModel>.ValidationFailure(
                "Subscription expiration must be later than its start date.");
        }

        var user = await repository.Users.GetByIdAsync(command.UserId);
        if (user is null)
        {
            return OperationResult<SubscriptionModel>.NotFoundFailure("User not found.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var existing = await repository.Subscriptions.GetByUserIdAsync(command.UserId);
        var historyEventType = command.Status == SubscriptionStatus.Expired
            ? SubscriptionHistoryEventType.Expired
            : existing is null
                ? SubscriptionHistoryEventType.Created
                : SubscriptionHistoryEventType.Updated;
        var subscription = existing ?? new SubscriptionEntity
        {
            Id = UidHelper.GenerateNewId("subscription"),
            UserId = command.UserId,
            CreatedAt = now
        };

        subscription.Plan = command.Plan;
        subscription.Status = command.Status;
        subscription.StartedAt = command.StartedAt;
        subscription.ExpiresAt = command.ExpiresAt;
        subscription.UpdatedAt = now;

        var persisted = await repository.Subscriptions.UpsertAsync(subscription);
        if (!persisted)
        {
            return OperationResult<SubscriptionModel>.Failure(
                $"Failed to persist subscription for user '{command.UserId}'.");
        }

        var history = SubscriptionHistoryEntityFactory.Create(
            subscription,
            historyEventType,
            now);
        await repository.SubscriptionHistory.InsertAsync(history);

        return OperationResult<SubscriptionModel>.Success(
            new SubscriptionModel(
                subscription.UserId,
                subscription.Plan,
                subscription.Status,
                subscription.StartedAt,
                subscription.ExpiresAt,
                subscription.CreatedAt,
                subscription.UpdatedAt));
    }

    private bool IsAuthorized(string providedSecret)
    {
        var expectedSecret = configuration["InternalAuthSecret"];
        if (expectedSecret is null || expectedSecret.Length == 0)
        {
            return false;
        }

        var expectedBytes = Encoding.UTF8.GetBytes(expectedSecret);
        var providedBytes = Encoding.UTF8.GetBytes(providedSecret);
        return expectedBytes.Length == providedBytes.Length &&
            CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}

public sealed record UpsertSubscriptionCommand(
    string InternalAuthSecret,
    string UserId,
    SubscriptionPlan Plan,
    SubscriptionStatus Status,
    DateTime StartedAt,
    DateTime? ExpiresAt) : IOperationCommand;
