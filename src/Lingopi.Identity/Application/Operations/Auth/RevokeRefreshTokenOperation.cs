using Lingopi.Identity.Application.Helpers;
using Lingopi.Identity.Application.Interfaces;
using Minimals.Operations;

namespace Lingopi.Identity.Application.Operations.Auth;

public class RevokeRefreshTokenOperation(IRepositoryManager repository) :
    IOperation<RevokeRefreshTokenCommand, NoResult>
{
    public async Task<OperationResult<NoResult>> ExecuteAsync(
        RevokeRefreshTokenCommand command, CancellationToken? cancellation = null)
    {
        if (!string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            await repository.RefreshTokens.RevokeAsync(
                RefreshTokenHelper.Hash(command.RefreshToken),
                DateTime.UtcNow);
        }

        return OperationResult.Success();
    }
}

public record RevokeRefreshTokenCommand(string? RefreshToken) : IOperationCommand;
