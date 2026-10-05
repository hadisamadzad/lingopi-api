using Lingopi.Core.Helpers;
using Lingopi.Identity.Application.Helpers;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Types.Entities;

namespace Lingopi.Identity.Application.Operations.Auth;

public partial class AuthenticateGoogleUserOperation(
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
        var entity = await repository.Users.GetByEmailAsync(email);
        if (entity is null)
        {
            var isFirstUser = !await repository.Users.AnyAsync();
            entity = new UserEntity
            {
                Id = UidHelper.GenerateNewId("user"),
                Email = email,
                IsEmailConfirmed = true,
                FirstName = command.FirstName,
                LastName = command.LastName,
                PasswordHash = PasswordHelper.Hash(Guid.NewGuid().ToString("N")),
                Status = UserState.Active,
                Role = isFirstUser ? Role.Owner : Role.User,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await repository.Users.InsertAsync(entity);

            // Create a free subscription for the user
            var subscription = SubscriptionEntityFactory.CreateFree(entity.Id, entity.CreatedAt);
            var subscriptionPersisted = await repository.Subscriptions.UpsertAsync(subscription);
            if (!subscriptionPersisted)
            {
                return OperationResult<AuthenticateGoogleUserResult>.Failure(
                    $"Failed to create the Free subscription for user '{entity.Id}'.");
            }

            var subscriptionHistory = SubscriptionHistoryEntityFactory.Create(
                subscription,
                SubscriptionHistoryEventType.Created,
                entity.CreatedAt);
            await repository.SubscriptionHistory.InsertAsync(subscriptionHistory);
        }

        if (entity.IsLockedOutOrNotActive())
        {
            return OperationResult<AuthenticateGoogleUserResult>.AuthorizationFailure(
                "User is locked out or not active");
        }

        entity.LastLoginDate = DateTime.UtcNow;
        await repository.Users.UpdateAsync(entity);

        var (Token, Entity) = RefreshTokenHelper.Create(entity.Id, TokenHelper.RefreshTokenLifetime);
        await repository.RefreshTokens.InsertAsync(Entity);

        return OperationResult<AuthenticateGoogleUserResult>.Success(new(
            entity.CreateJwtAccessToken(),
            Token,
            TokenHelper.RefreshTokenLifetime));
    }

}

public record AuthenticateGoogleUserCommand(
    string InternalAuthSecret,
    string Email,
    string? FirstName,
    string? LastName) : IOperationCommand<AuthenticateGoogleUserResult>;

public record AuthenticateGoogleUserResult(
    string AccessToken,
    string RefreshToken,
    TimeSpan RefreshTokenLifetime);
