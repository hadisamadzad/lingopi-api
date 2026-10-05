using System.Security.Cryptography;
using System.Text;
using Lingopi.Identity.Application.Extensions.Mappers;
using Lingopi.Identity.Application.Helpers;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Types.Entities;
using Lingopi.Identity.Application.Types.Models.Subscriptions;

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

        var entity = await repository.Users.GetByIdAsync(command.UserId);
        if (entity is null)
        {
            return OperationResult<EffectiveEntitlementModel>.NotFoundFailure("User not found.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var subscriptionEntity = await repository.Subscriptions.GetByUserIdAsync(command.UserId);
        var isExpiredByDate = subscriptionEntity is not null &&
            subscriptionEntity.Status == SubscriptionStatus.Active &&
            subscriptionEntity.ExpiresAt is { } expirationAt &&
            expirationAt <= now;
        if (isExpiredByDate)
        {
            var expiredSubscriptionEntity = await repository.Subscriptions.MarkExpiredAsync(
                command.UserId,
                now);
            var expirationMarked = expiredSubscriptionEntity is not null;
            if (expirationMarked)
            {
                var history = SubscriptionHistoryEntityFactory.Create(
                    expiredSubscriptionEntity!,
                    SubscriptionHistoryEventType.Expired,
                    now,
                    SubscriptionStatus.Expired);
                await repository.SubscriptionHistory.InsertAsync(history);
            }
        }

        var isActive = subscriptionEntity is not null &&
            subscriptionEntity.Status == SubscriptionStatus.Active &&
            subscriptionEntity.StartedAt <= now &&
            (subscriptionEntity.ExpiresAt is null || subscriptionEntity.ExpiresAt > now);
        var status = subscriptionEntity?.Status == SubscriptionStatus.Active &&
            subscriptionEntity.ExpiresAt is { } expiresAt &&
            expiresAt <= now ? SubscriptionStatus.Expired : subscriptionEntity?.Status;

        var effectivePlan = isActive ? subscriptionEntity!.Plan : SubscriptionPlan.Free;
        var model = entity.ToModel(
            effectivePlan,
            status,
            subscriptionEntity?.StartedAt,
            subscriptionEntity?.ExpiresAt);

        return OperationResult<EffectiveEntitlementModel>.Success(model);
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
    string UserId) : IOperationCommand<EffectiveEntitlementModel>;
