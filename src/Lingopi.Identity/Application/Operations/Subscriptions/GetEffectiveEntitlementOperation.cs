using System.Security.Cryptography;
using System.Text;
using Lingopi.Identity.Application.Helpers;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Types.Entities;
using Minimals.Operations;

namespace Lingopi.Identity.Application.Operations.Subscriptions;

public sealed class GetEffectiveEntitlementOperation(
    IRepositoryManager repository,
    IConfiguration configuration,
    TimeProvider timeProvider) :
    IOperation<GetEffectiveEntitlementCommand, EffectiveEntitlementModel>
{
    public async Task<OperationResult<EffectiveEntitlementModel>> ExecuteAsync(
        GetEffectiveEntitlementCommand command,
        CancellationToken? cancellation = null)
    {
        var isAuthorized = IsAuthorized(command.InternalAuthSecret);
        if (!isAuthorized)
        {
            return OperationResult<EffectiveEntitlementModel>.AuthorizationFailure(
                "Invalid internal authentication.");
        }

        var hasUserId = !string.IsNullOrWhiteSpace(command.UserId);
        if (!hasUserId)
        {
            return OperationResult<EffectiveEntitlementModel>.ValidationFailure("UserId is required.");
        }

        var user = await repository.Users.GetByIdAsync(command.UserId);
        if (user is null)
        {
            return OperationResult<EffectiveEntitlementModel>.NotFoundFailure("User not found.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var subscription = await repository.Subscriptions.GetByUserIdAsync(command.UserId);
        var isExpiredByDate = subscription is not null &&
            subscription.Status == SubscriptionStatus.Active &&
            subscription.ExpiresAt is { } expirationAt &&
            expirationAt <= now;
        if (isExpiredByDate)
        {
            var expiredSubscription = await repository.Subscriptions.MarkExpiredAsync(
                command.UserId,
                now);
            var expirationMarked = expiredSubscription is not null;
            if (expirationMarked)
            {
                var history = SubscriptionHistoryEntityFactory.Create(
                    expiredSubscription!,
                    SubscriptionHistoryEventType.Expired,
                    now,
                    SubscriptionStatus.Expired);
                await repository.SubscriptionHistory.InsertAsync(history);
            }
        }

        var isActive = subscription is not null &&
            subscription.Status == SubscriptionStatus.Active &&
            subscription.StartedAt <= now &&
            (subscription.ExpiresAt is null || subscription.ExpiresAt > now);
        var status = subscription?.Status == SubscriptionStatus.Active &&
            subscription.ExpiresAt is { } expiresAt &&
            expiresAt <= now ? SubscriptionStatus.Expired : subscription?.Status;

        return OperationResult<EffectiveEntitlementModel>.Success(
            new EffectiveEntitlementModel(
                command.UserId,
                isActive ? subscription!.Plan : SubscriptionPlan.Free,
                status,
                subscription?.StartedAt,
                subscription?.ExpiresAt));
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

public sealed record GetEffectiveEntitlementCommand(
    string InternalAuthSecret,
    string UserId) : IOperationCommand;

public sealed record EffectiveEntitlementModel(
    string UserId,
    SubscriptionPlan Plan,
    SubscriptionStatus? SubscriptionStatus,
    DateTime? SubscriptionStartedAt,
    DateTime? SubscriptionExpiresAt);
