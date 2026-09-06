using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Types.Entities;

namespace Lingopi.Identity.Application.Operations.Users;

public sealed class UpdateUserThemeOperation(IRepositoryManager repository) :
    IOperation<UpdateUserThemeCommand, NoResult>
{
    public async Task<OperationResult<NoResult>> ExecuteAsync(
        UpdateUserThemeCommand command,
        CancellationToken? cancellation = null)
    {
        var hasUserId = !string.IsNullOrWhiteSpace(command.UserId);
        if (!hasUserId)
        {
            return OperationResult.ValidationFailure("UserId is required.");
        }

        var user = await repository.Users.GetByIdAsync(command.UserId);
        if (user is null)
        {
            return OperationResult.NotFoundFailure("User not found.");
        }

        user.Settings ??= new UserSettings();
        user.Settings.Theme = command.Theme;
        user.UpdatedAt = DateTime.UtcNow;

        var updated = await repository.Users.UpdateAsync(user);
        if (!updated)
        {
            return OperationResult.Failure($"Failed to update theme for user '{command.UserId}'.");
        }

        return OperationResult.Success();
    }
}

public sealed record UpdateUserThemeCommand(string UserId, ThemePreference? Theme) : IOperationCommand<NoResult>;
