using System.Security.Cryptography;
using System.Text;
using Lingopi.Core.Helpers;
using Lingopi.Identity.Application.Extensions.Mappers;
using Lingopi.Identity.Application.Helpers;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Types.Configs;
using Lingopi.Identity.Application.Types.Entities;
using Lingopi.Identity.Application.Types.Models.Subscriptions;

namespace Lingopi.Identity.Application.Operations.Subscriptions;

public sealed class UpsertSubscriptionOperation(
    IRepositoryManager repository,
    IConfiguration configuration,
    TimeProvider timeProvider) :
    IOperation<UpsertSubscriptionCommand, SubscriptionReadModel>
{
    public async Task<OperationResult<SubscriptionReadModel>> ExecuteAsync(
        UpsertSubscriptionCommand command,
        CancellationToken? cancellation = null)
    {
        var isAuthorized = IsAuthorized(command.InternalAuthSecret);
        if (!isAuthorized)
        {
            return OperationResult<SubscriptionReadModel>.AuthorizationFailure(
                "Invalid internal authentication.");
        }

        var isPlanDefined = Enum.IsDefined(command.Plan);
        var isStatusDefined = Enum.IsDefined(command.Status);
        if (!isPlanDefined || !isStatusDefined)
        {
            return OperationResult<SubscriptionReadModel>.ValidationFailure(
                "Subscription plan or status is invalid.");
        }

        var paymentsEnabled = PaymentGatewayConfig.IsEnabled(configuration);
        var isPaidPlan = command.Plan != SubscriptionPlan.Free;
        if (paymentsEnabled && isPaidPlan)
        {
            return OperationResult<SubscriptionReadModel>.Failure(
                "Paid subscriptions require a payment gateway checkout.");
        }

        var hasStartDate = command.StartedAt != default;
        if (!hasStartDate)
        {
            return OperationResult<SubscriptionReadModel>.ValidationFailure(
                "Subscription start date is required.");
        }

        var hasUserId = !string.IsNullOrWhiteSpace(command.UserId);
        if (!hasUserId)
        {
            return OperationResult<SubscriptionReadModel>.ValidationFailure("UserId is required.");
        }

        if (command.ExpiresAt is { } expiresAt && expiresAt <= command.StartedAt)
        {
            return OperationResult<SubscriptionReadModel>.ValidationFailure(
                "Subscription expiration must be later than its start date.");
        }

        var entity = await repository.Users.GetByIdAsync(command.UserId);
        if (entity is null)
        {
            return OperationResult<SubscriptionReadModel>.NotFoundFailure("User not found.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var existingEntity = await repository.Subscriptions.GetByUserIdAsync(command.UserId);
        var historyEventType = command.Status == SubscriptionStatus.Expired
            ? SubscriptionHistoryEventType.Expired
            : existingEntity is null
                ? SubscriptionHistoryEventType.Created
                : SubscriptionHistoryEventType.Updated;
        var subscriptionEntity = existingEntity ?? new SubscriptionEntity
        {
            Id = UidHelper.GenerateNewId("subscription"),
            UserId = command.UserId,
            CreatedAt = now
        };

        subscriptionEntity.Plan = command.Plan;
        subscriptionEntity.Source = command.Plan == SubscriptionPlan.Free
            ? SubscriptionSource.SystemAssigned
            : SubscriptionSource.Purchased;
        subscriptionEntity.Status = command.Status;
        subscriptionEntity.StartedAt = command.StartedAt;
        subscriptionEntity.ExpiresAt = command.ExpiresAt;
        subscriptionEntity.UpdatedAt = now;

        var persisted = await repository.Subscriptions.UpsertAsync(subscriptionEntity);
        if (!persisted)
        {
            return OperationResult<SubscriptionReadModel>.Failure(
                $"Failed to persist subscription for user '{command.UserId}'.");
        }

        var history = SubscriptionHistoryEntityFactory.Create(
            subscriptionEntity,
            historyEventType,
            now);
        await repository.SubscriptionHistory.InsertAsync(history);

        return OperationResult<SubscriptionReadModel>.Success(
            subscriptionEntity.ToReadModel());
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
    DateTime? ExpiresAt) : IOperationCommand<SubscriptionReadModel>;
