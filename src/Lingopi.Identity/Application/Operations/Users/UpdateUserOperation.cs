using FluentValidation;
using Lingopi.Core.Extensions;
using Lingopi.Identity.Application.Interfaces;

namespace Lingopi.Identity.Application.Operations.Users;

public class UpdateUserOperation(IRepositoryManager repository) :
    IOperation<UpdateUserCommand, NoResult>
{
    public async Task<OperationResult<NoResult>> ExecuteAsync(
        UpdateUserCommand command, CancellationToken? cancellation = null)
    {
        // Validation
        var validation = new UpdateUserValidator().Validate(command);
        if (!validation.IsValid)
        {
            return OperationResult.ValidationFailure([.. validation.GetErrorMessages()]);
        }

        // Check if user is admin
        var requesterEntity = await repository.Users.GetByIdAsync(command.AdminUserId);
        if (requesterEntity is null)
        {
            return OperationResult.NotFoundFailure("Access denied");
        }

        // Get
        var entity = await repository.Users.GetByIdAsync(command.UserId);
        if (entity is null)
        {
            return OperationResult.NotFoundFailure("User not found");
        }

        // Update
        entity.FirstName = command.FirstName;
        entity.LastName = command.LastName;

        entity.UpdatedAt = DateTime.UtcNow;
        _ = await repository.Users.UpdateAsync(entity);

        return OperationResult.Success();
    }
}

public record UpdateUserCommand(
    string AdminUserId,
    string UserId,
    string FirstName,
    string LastName
) : IOperationCommand<NoResult>;

public class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserValidator()
    {
        // User id
        RuleFor(x => x.UserId)
            .NotEmpty();

        // First name
        RuleFor(x => x.FirstName)
            .Length(2, 80)
            .When(x => !string.IsNullOrEmpty(x.FirstName));

        // Last name
        RuleFor(x => x.LastName)
            .Length(2, 80)
            .When(x => !string.IsNullOrEmpty(x.LastName));
    }
}
