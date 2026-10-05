using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Types.Entities;

namespace Lingopi.Identity.Application.Operations.Users;

public sealed class UpdateUserTimezoneOperation(IRepositoryManager repository) :
    IOperation<UpdateUserTimezoneCommand, NoResult>
{
    public async Task<OperationResult<NoResult>> ExecuteAsync(
        UpdateUserTimezoneCommand command,
        CancellationToken? cancellation = null)
    {
        var hasUserId = !string.IsNullOrWhiteSpace(command.UserId);
        if (!hasUserId)
        {
            return OperationResult.ValidationFailure("UserId is required.");
        }

        var normalizedTimeZoneId = command.TimeZoneId?.Trim();
        var hasTimeZoneId = !string.IsNullOrWhiteSpace(normalizedTimeZoneId);
        var isValidTimeZone = !hasTimeZoneId || IsValidTimeZone(normalizedTimeZoneId!);
        if (!isValidTimeZone)
        {
            return OperationResult.ValidationFailure("TimeZoneId must be a valid IANA timezone identifier.");
        }

        var entity = await repository.Users.GetByIdAsync(command.UserId);
        if (entity is null)
        {
            return OperationResult.NotFoundFailure("User not found.");
        }

        entity.Settings ??= new UserSettings();
        entity.Settings.TimeZoneId = hasTimeZoneId ? normalizedTimeZoneId : null;
        entity.UpdatedAt = DateTime.UtcNow;

        var updated = await repository.Users.UpdateAsync(entity);
        if (!updated)
        {
            return OperationResult.Failure($"Failed to update timezone for user '{command.UserId}'.");
        }

        return OperationResult.Success();
    }

    private static bool IsValidTimeZone(string timeZoneId)
    {
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}

public sealed record UpdateUserTimezoneCommand(string UserId, string? TimeZoneId) : IOperationCommand<NoResult>;
