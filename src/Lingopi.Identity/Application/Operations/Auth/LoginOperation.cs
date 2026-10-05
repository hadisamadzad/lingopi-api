using FluentValidation;
using Lingopi.Core.Extensions;
using Lingopi.Identity.Application.Helpers;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Types.Entities;
using Lingopi.Identity.Application.Types.Models.Auth;

namespace Lingopi.Identity.Application.Operations.Auth;

public class LoginOperation(IRepositoryManager repository) :
    IOperation<LoginCommand, LoginResult>
{
    public async Task<OperationResult<LoginResult>> ExecuteAsync(
        LoginCommand command, CancellationToken? cancellation = null)
    {
        // Validation
        var validation = new LoginValidator().Validate(command);
        if (!validation.IsValid)
        {
            return OperationResult<LoginResult>.ValidationFailure([.. validation.GetErrorMessages()]);
        }

        // Get
        var entity = await repository.Users.GetByEmailAsync(command.Email);
        if (entity is null)
        {
            return OperationResult<LoginResult>.NotFoundFailure("User not found");
        }

        // Lockout check
        if (entity.IsLockedOutOrNotActive())
        {
            return OperationResult<LoginResult>.AuthorizationFailure("User is locked out or not active");
        }

        // Login check via password
        var isLoginSuccessful = PasswordHelper.CheckPasswordHash(entity.PasswordHash, command.Password);

        // Lockout history
        if (!isLoginSuccessful)
        {
            entity.TryToLockout();
            _ = await repository.Users.UpdateAsync(entity);
            return OperationResult<LoginResult>.AuthorizationFailure("Invalid credentials");
        }

        /* Here user is authenticated */
        entity.LastLoginDate = DateTime.UtcNow;
        entity.ResetLockoutHistory();
        _ = await repository.Users.UpdateAsync(entity);

        var (Token, Entity) = RefreshTokenHelper.Create(entity.Id, TokenHelper.RefreshTokenLifetime);
        await repository.RefreshTokens.InsertAsync(Entity);

        var result = new LoginResult
        (
            Email: entity.Email,
            FullName: entity.GetFullName(),
            AccessToken: entity.CreateJwtAccessToken(),
            RefreshToken: Token,
            RefreshTokenLifetime: TokenHelper.RefreshTokenLifetime
        );

        return OperationResult<LoginResult>.Success(result);
    }
}

public record LoginCommand(
    string Email,
    string Password) : IOperationCommand<LoginResult>;

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}
