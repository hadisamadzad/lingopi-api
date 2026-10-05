using Lingopi.Identity.Application.Extensions.Mappers;
using Lingopi.Identity.Application.Helpers;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Types.Entities;
using Lingopi.Identity.Application.Types.Models.Subscriptions;

namespace Lingopi.Identity.Application.Operations.Subscriptions;

public sealed class GetSubscriptionOperation(
    IRepositoryManager repository,
    TimeProvider? timeProvider = null) :
    IOperation<GetSubscriptionCommand, SubscriptionReadModel>
{
    public async Task<OperationResult<SubscriptionReadModel>> ExecuteAsync(
        GetSubscriptionCommand command,
        CancellationToken? cancellation = null)
    {
        var hasUserId = !string.IsNullOrWhiteSpace(command.UserId);
        if (!hasUserId)
        {
            return OperationResult<SubscriptionReadModel>.ValidationFailure("UserId is required.");
        }

        var entity = await repository.Users.GetByIdAsync(command.UserId);
        if (entity is null)
        {
            return OperationResult<SubscriptionReadModel>.NotFoundFailure("User not found.");
        }

        var subscriptionEntity = await repository.Subscriptions.GetByUserIdAsync(command.UserId);
        if (subscriptionEntity is null)
        {
            return OperationResult<SubscriptionReadModel>.NotFoundFailure("Subscription not found.");
        }

        var now = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        var isExpiredByDate = subscriptionEntity.Status == SubscriptionStatus.Active &&
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

            var currentSubscriptionEntity =
                await repository.Subscriptions.GetByUserIdAsync(command.UserId);
            if (currentSubscriptionEntity is null)
            {
                return OperationResult<SubscriptionReadModel>.NotFoundFailure(
                    "Subscription not found.");
            }

            subscriptionEntity = currentSubscriptionEntity;
        }

        return OperationResult<SubscriptionReadModel>.Success(
            subscriptionEntity.ToReadModel());
    }

}

public sealed record GetSubscriptionCommand(string UserId) : IOperationCommand<SubscriptionReadModel>;
