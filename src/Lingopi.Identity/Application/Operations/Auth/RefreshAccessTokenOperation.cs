using Lingopi.Identity.Application.Helpers;
using Lingopi.Identity.Application.Interfaces;
using Minimals.Operations;

namespace Lingopi.Identity.Application.Operations.Auth;

public class RefreshAccessTokenOperation(IRepositoryManager repository) :
    IOperation<RefreshAccessTokenCommand, RefreshAccessTokenResult>
{
    public async Task<OperationResult<RefreshAccessTokenResult>> ExecuteAsync(
        RefreshAccessTokenCommand command, CancellationToken? cancellation = null)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return OperationResult<RefreshAccessTokenResult>.ValidationFailure("Invalid refresh token");
        }

        // Consume first: this is an atomic compare-and-set, so a token cannot be replayed.
        var (Token, Entity) = RefreshTokenHelper.Create(string.Empty, TokenHelper.RefreshTokenLifetime);
        var consumed = await repository.RefreshTokens.ConsumeAsync(
            RefreshTokenHelper.Hash(command.RefreshToken), DateTime.UtcNow, Entity.Id);

        if (consumed is null)
        {
            return OperationResult<RefreshAccessTokenResult>.ValidationFailure("Invalid refresh token");
        }

        var user = await repository.Users.GetByIdAsync(consumed.UserId);
        if (user is null)
        {
            return OperationResult<RefreshAccessTokenResult>.NotFoundFailure("User not found");
        }

        if (user.IsLockedOutOrNotActive())
        {
            return OperationResult<RefreshAccessTokenResult>.AuthorizationFailure("User is locked out or not active");
        }

        Entity.UserId = user.Id;
        await repository.RefreshTokens.InsertAsync(Entity);

        return OperationResult<RefreshAccessTokenResult>.Success(new(
            user.CreateJwtAccessToken(), Token, TokenHelper.RefreshTokenLifetime));
    }
}

public record RefreshAccessTokenCommand(string RefreshToken) : IOperationCommand;
public record RefreshAccessTokenResult(
    string AccessToken,
    string RefreshToken,
    TimeSpan RefreshTokenLifetime);
