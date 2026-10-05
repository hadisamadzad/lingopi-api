using FluentValidation;
using Lingopi.Core.Extensions;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Types.Entities;

namespace Lingopi.Identity.Application.Operations.Users;

public sealed class UpdateAdminUserStatusOperation(IRepositoryManager repository) :
    IOperation<UpdateAdminUserStatusCommand, NoResult>
{
    public async Task<OperationResult<NoResult>> ExecuteAsync(
        UpdateAdminUserStatusCommand command, CancellationToken? cancellation = null)
    {
        var validation = new UpdateAdminUserStatusValidator().Validate(command);
        if (!validation.IsValid)
        {
            return OperationResult.ValidationFailure([.. validation.GetErrorMessages()]);
        }

        var requesterEntity = await repository.Users.GetByIdAsync(command.AdminUserId);
        if (requesterEntity is null)
        {
            return OperationResult.AuthorizationFailure("Administrator access required.");
        }

        var hasAdminAccess = requesterEntity.HasAdminRole() &&
            requesterEntity.Status == UserState.Active;
        if (!hasAdminAccess)
        {
            return OperationResult.AuthorizationFailure("Administrator access required.");
        }

        var entity = await repository.Users.GetByIdAsync(command.UserId);
        if (entity is null)
        {
            return OperationResult.NotFoundFailure("User not found.");
        }

        var statusChanged = entity.Status != command.Status;
        if (!statusChanged)
        {
            return OperationResult.Success();
        }

        entity.Status = command.Status;
        entity.UpdatedAt = DateTime.UtcNow;

        var isUpdated = await repository.Users.UpdateAsync(entity);
        if (!isUpdated)
        {
            return OperationResult.Failure($"Failed to update status for user '{entity.Id}'.");
        }

        return OperationResult.Success();
    }
}

public sealed class UpdateAdminUserStatusValidator : AbstractValidator<UpdateAdminUserStatusCommand>
{
    public UpdateAdminUserStatusValidator()
    {
        RuleFor(command => command.AdminUserId).NotEmpty();
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.Status).IsInEnum();
    }
}

public sealed record UpdateAdminUserStatusCommand(
    string AdminUserId,
    string UserId,
    UserState Status
) : IOperationCommand<NoResult>;
