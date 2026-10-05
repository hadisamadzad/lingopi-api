using Lingopi.Identity.Application.Helpers;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Types.Entities;

namespace Lingopi.Identity.Application.Operations.PasswordReset;

public class GetPasswordResetEmailOperation(
    IRepositoryManager repository)
    : IOperation<GetPasswordResetEmailCommand, string>
{
    public async Task<OperationResult<string>> ExecuteAsync(
        GetPasswordResetEmailCommand command, CancellationToken? cancellation = null)
    {
        var (succeeded, email) = PasswordResetTokenHelper.ReadPasswordResetToken(command.Token);
        if (!succeeded)
        {
            return OperationResult<string>.ValidationFailure("Invalid token");
        }

        var entity = await repository.Users.GetByEmailAsync(email);
        if (entity == null)
        {
            //logger.LogWarning("No user found for password reset token: {Token}", command.Token);
            return OperationResult<string>.NotFoundFailure("No user found for the provided token");
        }

        if (entity.IsLockedOutOrNotActive())
        {
            return OperationResult<string>.AuthorizationFailure("User is locked out or not active");
        }

        if (!string.Equals(entity.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<string>.AuthorizationFailure("Invalid token");
        }

        return OperationResult<string>.Success(entity.Email);
    }
}

public record GetPasswordResetEmailCommand(string Token) : IOperationCommand<string>;
