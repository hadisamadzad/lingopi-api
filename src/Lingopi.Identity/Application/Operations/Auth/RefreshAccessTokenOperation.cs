using Lingopi.Identity.Application.Helpers;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Types.Entities;

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
        var refreshTokenEntity = await repository.RefreshTokens.ConsumeAsync(
            RefreshTokenHelper.Hash(command.RefreshToken), DateTime.UtcNow, Entity.Id);

        if (refreshTokenEntity is null)
        {
            return OperationResult<RefreshAccessTokenResult>.ValidationFailure("Invalid refresh token");
        }

        var entity = await repository.Users.GetByIdAsync(refreshTokenEntity.UserId);
        if (entity is null)
        {
            return OperationResult<RefreshAccessTokenResult>.NotFoundFailure("User not found");
        }

        if (entity.IsLockedOutOrNotActive())
        {
            return OperationResult<RefreshAccessTokenResult>.AuthorizationFailure("User is locked out or not active");
        }

        Entity.UserId = entity.Id;
        await repository.RefreshTokens.InsertAsync(Entity);

        return OperationResult<RefreshAccessTokenResult>.Success(new(
            entity.CreateJwtAccessToken(), Token, TokenHelper.RefreshTokenLifetime));
    }
}

public record RefreshAccessTokenCommand(string RefreshToken) : IOperationCommand<RefreshAccessTokenResult>;
public record RefreshAccessTokenResult(
    string AccessToken,
    string RefreshToken,
    TimeSpan RefreshTokenLifetime);
