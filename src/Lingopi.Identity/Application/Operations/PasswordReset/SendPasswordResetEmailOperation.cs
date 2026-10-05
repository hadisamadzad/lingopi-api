using FluentValidation;
using Lingopi.Core.Extensions;
using Lingopi.Identity.Application.Helpers;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Types.Configs;
using Lingopi.Identity.Application.Types.Entities;
using Microsoft.Extensions.Options;

namespace Lingopi.Identity.Application.Operations.PasswordReset;

public class SendPasswordResetEmailOperation(
    IRepositoryManager repository,
    IEmailService transactionalEmailService,
    IOptions<PasswordResetConfig> passwordResetConfig)
    : IOperation<SendPasswordResetEmailCommand, NoResult>
{
    private readonly PasswordResetConfig _passwordResetConfig = passwordResetConfig.Value;

    public async Task<OperationResult<NoResult>> ExecuteAsync(
        SendPasswordResetEmailCommand command, CancellationToken? cancellation = null)
    {
        // Validation
        var validation = new SendPasswordResetEmailValidator().Validate(command);
        if (!validation.IsValid)
        {
            return OperationResult.ValidationFailure([.. validation.GetErrorMessages()]);
        }

        // Get
        var entity = await repository.Users.GetByEmailAsync(command.Email);
        if (entity is null)
        {
            return OperationResult.NotFoundFailure("User not found");
        }

        if (entity.IsLockedOutOrNotActive())
        {
            return OperationResult.AuthorizationFailure("User is locked out or not active");
        }

        var expirationTime = ExpirationTimeHelper
            .GetExpirationTime(_passwordResetConfig.LinkLifetimeInDays);

        var token = PasswordResetTokenHelper
            .GeneratePasswordResetToken(entity.Email, expirationTime);

        var email = entity.Email;
        var passwordResetLink = string.Format(_passwordResetConfig.LinkFormat, token);

        var @params = new Dictionary<string, string>
            {
                { "Link", passwordResetLink }
            };

        _ = await transactionalEmailService.SendEmailByTemplateIdAsync(
            _passwordResetConfig.BrevoTemplateId, [email], @params);

        return OperationResult.Success();
    }
}

public record SendPasswordResetEmailCommand(string Email) : IOperationCommand<NoResult>;

public class SendPasswordResetEmailValidator : AbstractValidator<SendPasswordResetEmailCommand>
{
    public SendPasswordResetEmailValidator()
    {
        RuleFor(x => x.Email)
            .EmailAddress();
    }
}
