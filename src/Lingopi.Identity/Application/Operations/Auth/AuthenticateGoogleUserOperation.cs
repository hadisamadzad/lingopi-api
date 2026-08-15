using System.Security.Cryptography;
using System.Text;
using Lingopi.Core.Helpers;
using Lingopi.Identity.Application.Helpers;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Types.Entities;
using Minimals.Operations;

namespace Lingopi.Identity.Application.Operations.Auth;

public class AuthenticateGoogleUserOperation(
    IRepositoryManager repository,
    IConfiguration configuration) :
    IOperation<AuthenticateGoogleUserCommand, AuthenticateGoogleUserResult>
{
    public async Task<OperationResult<AuthenticateGoogleUserResult>> ExecuteAsync(
        AuthenticateGoogleUserCommand command, CancellationToken? cancellation = null)
    {
        if (!IsAuthorized(command.InternalAuthSecret))
        {
            return OperationResult<AuthenticateGoogleUserResult>.AuthorizationFailure(
                "Invalid internal authentication");
        }

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return OperationResult<AuthenticateGoogleUserResult>.ValidationFailure(
                "Email is required");
        }

        var email = command.Email.Trim().ToLowerInvariant();
        var user = await repository.Users.GetByEmailAsync(email);
        if (user is null)
        {
            var isFirstUser = !await repository.Users.AnyAsync();
            user = new UserEntity
            {
                Id = UidHelper.GenerateNewId("user"),
                Email = email,
                IsEmailConfirmed = true,
                FirstName = command.FirstName,
                LastName = command.LastName,
                PasswordHash = PasswordHelper.Hash(Guid.NewGuid().ToString("N")),
                Status = UserState.Active,
                Role = isFirstUser ? Role.Owner : Role.User,
                SecurityStamp = UserHelper.CreateUserStamp(),
                ConcurrencyStamp = UserHelper.CreateUserStamp(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await repository.Users.InsertAsync(user);
        }

        if (user.IsLockedOutOrNotActive())
        {
            return OperationResult<AuthenticateGoogleUserResult>.AuthorizationFailure(
                "User is locked out or not active");
        }

        user.LastLoginDate = DateTime.UtcNow;
        await repository.Users.UpdateAsync(user);

        var (Token, Entity) = RefreshTokenHelper.Create(user.Id, TokenHelper.RefreshTokenLifetime);
        await repository.RefreshTokens.InsertAsync(Entity);

        return OperationResult<AuthenticateGoogleUserResult>.Success(new(
            user.CreateJwtAccessToken(),
            Token,
            TokenHelper.RefreshTokenLifetime));
    }

    private bool IsAuthorized(string providedSecret)
    {
        var expectedSecret = configuration["InternalAuthSecret"];
        if (string.IsNullOrEmpty(expectedSecret))
        {
            return false;
        }

        var expectedBytes = Encoding.UTF8.GetBytes(expectedSecret);
        var providedBytes = Encoding.UTF8.GetBytes(providedSecret);
        return expectedBytes.Length == providedBytes.Length &&
            CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}

public record AuthenticateGoogleUserCommand(
    string InternalAuthSecret,
    string Email,
    string? FirstName,
    string? LastName) : IOperationCommand;

public record AuthenticateGoogleUserResult(
    string AccessToken,
    string RefreshToken,
    TimeSpan RefreshTokenLifetime);
