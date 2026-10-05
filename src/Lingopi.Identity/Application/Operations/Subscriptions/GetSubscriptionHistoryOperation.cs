using Lingopi.Identity.Application.Extensions.Mappers;
using Lingopi.Identity.Application.Interfaces;
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

        var entity = await repository.Users.GetByIdAsync(command.UserId);
        if (entity is null)
        {
            return OperationResult<List<SubscriptionHistoryModel>>.NotFoundFailure("User not found.");
        }

        var entities = await repository.SubscriptionHistory.GetByUserIdAsync(command.UserId);
        var models = entities.ConvertAll(entity => entity.ToModel());
        return OperationResult<List<SubscriptionHistoryModel>>.Success(models);
    }
}

public sealed record GetSubscriptionHistoryCommand(string UserId) :
    IOperationCommand<List<SubscriptionHistoryModel>>;
