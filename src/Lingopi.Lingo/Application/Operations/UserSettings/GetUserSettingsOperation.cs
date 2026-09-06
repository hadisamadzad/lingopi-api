using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.ReadModels;

namespace Lingopi.Lingo.Application.Operations.UserSettings;

public sealed class GetUserSettingsOperation(IRepositoryManager repository) :
    IOperation<GetUserSettingsCommand, UserSettingsModel>
{
    public async Task<OperationResult<UserSettingsModel>> ExecuteAsync(
        GetUserSettingsCommand command, CancellationToken? cancellation = null)
    {
        // Validate the command
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            return OperationResult<UserSettingsModel>.ValidationFailure("UserId is required.");
        }

        var entity = await repository.UserSettings.GetByUserIdAsync(command.UserId);
        return entity is null
            ? OperationResult<UserSettingsModel>.NotFoundFailure($"Lingo settings for user '{command.UserId}' were not found.")
            : OperationResult<UserSettingsModel>.Success(entity.ToModel());
    }
}

public record GetUserSettingsCommand(string UserId) : IOperationCommand<UserSettingsModel>;

internal static class UserSettingsModelMapper
{
    public static UserSettingsModel ToModel(this Models.Entities.UserSettingsEntity entity)
    {
        return new UserSettingsModel(
            entity.UserId,
            entity.TargetLocaleCode,
            entity.SourceLocaleCodes,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
