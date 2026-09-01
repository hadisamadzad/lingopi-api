using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Types.Entities;
using Lingopi.Identity.Application.Types.Models.Subscriptions;
using Minimals.Operations;

namespace Lingopi.Identity.Application.Operations.Subscriptions;

public sealed class GetSubscriptionOperation(
    IRepositoryManager repository,
    TimeProvider? timeProvider = null) :
    IOperation<GetSubscriptionCommand, SubscriptionModel>
{
    public async Task<OperationResult<SubscriptionModel>> ExecuteAsync(
        GetSubscriptionCommand command,
        CancellationToken? cancellation = null)
    {
        var hasUserId = !string.IsNullOrWhiteSpace(command.UserId);
        if (!hasUserId)
        {
            return OperationResult<SubscriptionModel>.ValidationFailure("UserId is required.");
        }

        var user = await repository.Users.GetByIdAsync(command.UserId);
        if (user is null)
        {
            return OperationResult<SubscriptionModel>.NotFoundFailure("User not found.");
        }

        var subscription = await repository.Subscriptions.GetByUserIdAsync(command.UserId);
        if (subscription is null)
        {
            return OperationResult<SubscriptionModel>.Success(
                new SubscriptionModel(command.UserId, SubscriptionPlan.Free, null, null, null, null, null));
        }

        var now = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        var isActive = subscription.Status == SubscriptionStatus.Active &&
            subscription.StartedAt <= now &&
            (subscription.ExpiresAt is null || subscription.ExpiresAt > now);
        var status = subscription.Status == SubscriptionStatus.Active &&
            subscription.ExpiresAt is { } expiresAt &&
            expiresAt <= now
                ? SubscriptionStatus.Expired
                : subscription.Status;

        return OperationResult<SubscriptionModel>.Success(
            Map(subscription, isActive ? subscription.Plan : SubscriptionPlan.Free, status));
    }

    private static SubscriptionModel Map(
        SubscriptionEntity subscription,
        SubscriptionPlan effectivePlan,
        SubscriptionStatus effectiveStatus) =>
        new(
            subscription.UserId,
            effectivePlan,
            effectiveStatus,
            subscription.StartedAt,
            subscription.ExpiresAt,
            subscription.CreatedAt,
            subscription.UpdatedAt);
}

public sealed record GetSubscriptionCommand(string UserId) : IOperationCommand;
