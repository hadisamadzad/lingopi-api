using FluentValidation;
using Lingopi.Core.Extensions;
using Lingopi.Identity.Application.Helpers;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Types.Configs;
using Lingopi.Identity.Application.Types.Entities;
using Microsoft.Extensions.Options;

namespace Lingopi.Identity.Application.Operations.PasswordReset;

public class ResetPasswordOperation(IRepositoryManager repository,
    IOptions<PasswordResetConfig> passwordResetConfig)
    : IOperation<ResetPasswordCommand, NoResult>
{
    private readonly PasswordResetConfig _passwordResetConfig = passwordResetConfig.Value;

    public async Task<OperationResult<NoResult>> ExecuteAsync(
        ResetPasswordCommand command, CancellationToken? cancellation = default)
    {
        // Validation
        var validation = new ResetPasswordValidator().Validate(command);
        if (!validation.IsValid)
        {
            return OperationResult.ValidationFailure([.. validation.GetErrorMessages()]);
        }

        // Get
        var (succeeded, email) = PasswordResetTokenHelper.ReadPasswordResetToken(command.Token);
        if (!succeeded)
        {
            return OperationResult.AuthorizationFailure("Invalid token");
        }

        var entity = await repository.Users.GetByEmailAsync(email);
        if (entity is null)
        {
            return OperationResult.NotFoundFailure("User not found");
        }

        if (entity.IsLockedOutOrNotActive())
        {
            return OperationResult.AuthorizationFailure("User is locked out or not active");
        }

        entity.PasswordHash = PasswordHelper.Hash(command.NewPassword);

        _ = await repository.Users.UpdateAsync(entity);

        return OperationResult.Success();
    }
}

public record ResetPasswordCommand(string Token, string NewPassword) : IOperationCommand<NoResult>;

public class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty();

        RuleFor(x => x.NewPassword)
            .Must(x => PasswordHelper.CheckStrength(x) >= PasswordScore.Medium)
            .WithMessage("Password is not strong enough");
    }
}
