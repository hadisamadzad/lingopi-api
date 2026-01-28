using FluentValidation;
using Lingopi.Core.Helpers;
using Lingopi.Identity.Application.Interfaces;
using Minimals.Operations;

namespace Lingopi.Identity.Application.Operations.Auth;

public class CheckUsernameOperation(IRepositoryManager repository) :
    IOperation<CheckUsernameCommand, bool>
{
    public async Task<OperationResult<bool>> ExecuteAsync(
        CheckUsernameCommand command, CancellationToken? cancellation = null)
    {
        // Validation
        var validation = new CheckUsernameValidator().Validate(command);
        if (!validation.IsValid)
        {
            return OperationResult<bool>.ValidationFailure([.. validation.GetErrorMessages()]);
        }

        // Get
        var user = await repository.Users.GetByEmailAsync(command.Email);

        var isAvailable = user is null;

        return OperationResult<bool>.Success(isAvailable);
    }
}

public record CheckUsernameCommand(string Email) : IOperationCommand;

public class CheckUsernameValidator : AbstractValidator<CheckUsernameCommand>
{
    public CheckUsernameValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
