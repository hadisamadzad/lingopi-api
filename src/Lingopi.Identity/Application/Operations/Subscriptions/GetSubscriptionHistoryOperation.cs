using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Types.Entities;
using Lingopi.Identity.Application.Types.Models.Subscriptions;

namespace Lingopi.Identity.Application.Operations.Subscriptions;

public sealed class GetSubscriptionHistoryOperation(IRepositoryManager repository) :
    IOperation<GetSubscriptionHistoryCommand, List<SubscriptionHistoryModel>>
{
    public async Task<OperationResult<List<SubscriptionHistoryModel>>> ExecuteAsync(
        GetSubscriptionHistoryCommand command,
        CancellationToken? cancellation = null)
    {
        var hasUserId = !string.IsNullOrWhiteSpace(command.UserId);
        if (!hasUserId)
        {
            return OperationResult<List<SubscriptionHistoryModel>>.ValidationFailure("UserId is required.");
        }

        var user = await repository.Users.GetByIdAsync(command.UserId);
        if (user is null)
        {
            return OperationResult<List<SubscriptionHistoryModel>>.NotFoundFailure("User not found.");
        }

        var history = await repository.SubscriptionHistory.GetByUserIdAsync(command.UserId);
        var models = history.Select(Map).ToList();
        return OperationResult<List<SubscriptionHistoryModel>>.Success(models);
    }

    private static SubscriptionHistoryModel Map(SubscriptionHistoryEntity history) =>
        new(
            history.Id,
            history.SubscriptionId,
            history.UserId,
            history.EventType,
            history.Plan,
            history.Status,
            history.StartedAt,
            history.ExpiresAt,
            history.SubscriptionCreatedAt,
            history.SubscriptionUpdatedAt,
            history.RecordedAt);
}

public sealed record GetSubscriptionHistoryCommand(string UserId) :
    IOperationCommand<List<SubscriptionHistoryModel>>;
