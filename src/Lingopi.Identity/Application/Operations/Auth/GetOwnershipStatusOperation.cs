using Lingopi.Core.Utilities.OperationResult;
using Lingopi.Identity.Application.Interfaces;

namespace Lingopi.Identity.Application.Operations.Auth;

public class GetOwnershipStatusOperation(IRepositoryManager repository)
    : IOperation<GetOwnershipStatusCommand, bool>
{
    public async Task<OperationResult<bool>> ExecuteAsync(
        GetOwnershipStatusCommand command, CancellationToken? cancellation = null)
    {
        var isAlreadyOwned = await repository.Users.AnyAsync();

        return OperationResult<bool>.Success(isAlreadyOwned);
    }
}

public record GetOwnershipStatusCommand() : IOperationCommand;