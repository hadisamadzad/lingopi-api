using FluentValidation;
using Lingopi.Lingo.Application.Extensions.Mappers;
using Lingopi.Lingo.Application.Helpers;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.ReadModels;

namespace Lingopi.Lingo.Application.Operations.UserSettings;

public sealed class SaveUserSettingsOperation(
    IRepositoryManager repository) :
    IOperation<SaveUserSettingsCommand, UserSettingsModel>
{
    public async Task<OperationResult<UserSettingsModel>> ExecuteAsync(
        SaveUserSettingsCommand command,
        CancellationToken? cancellation = null)
    {
        // Validate the command
        var validation = new SaveUserSettingsCommandValidator().Validate(command);
        if (!validation.IsValid)
        {
            return OperationResult<UserSettingsModel>.ValidationFailure(
                [.. validation.Errors.Select(error => error.ErrorMessage)]);
        }

        var targetLocaleCode = LocaleCodeNormalizer.Normalize(command.TargetLocaleCode);
        var sourceLocaleCodes = command.SourceLocaleCodes
            .Select(LocaleCodeNormalizer.Normalize)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (string.IsNullOrWhiteSpace(targetLocaleCode))
        {
            return OperationResult<UserSettingsModel>.ValidationFailure(
                "Target locale code is invalid.");
        }

        if (sourceLocaleCodes.Count == 0)
        {
            return OperationResult<UserSettingsModel>.ValidationFailure(
                "At least one source locale code is required.");
        }

        var entities = await repository.Languages.GetActiveLanguagesAsync();
        if (entities.Count > 0)
        {
            var targetLocaleIsActive = await repository.Languages.IsActiveLocaleAsync(targetLocaleCode);
            if (!targetLocaleIsActive)
            {
                return OperationResult<UserSettingsModel>.ValidationFailure(
                    $"Target locale '{targetLocaleCode}' is not active in the language catalog.");
            }

            foreach (var sourceLocaleCode in sourceLocaleCodes)
            {
                var sourceLocaleIsActive = await repository.Languages.IsActiveLocaleAsync(sourceLocaleCode);
                if (!sourceLocaleIsActive)
                {
                    return OperationResult<UserSettingsModel>.ValidationFailure(
                        $"Source locale '{sourceLocaleCode}' is not active in the language catalog.");
                }
            }
        }

        // Persist the user settings
        var existingEntity = await repository.UserSettings.GetByUserIdAsync(command.UserId);
        var now = DateTime.UtcNow;
        var settings = new UserSettingsEntity
        {
            Id = existingEntity?.Id ?? $"user-setting-{command.UserId}",
            UserId = command.UserId,
            TargetLocaleCode = targetLocaleCode,
            SourceLocaleCodes = sourceLocaleCodes,
            CreatedAt = existingEntity?.CreatedAt ?? now,
            UpdatedAt = now
        };

        var updated = await repository.UserSettings.UpsertAsync(settings);
        return !updated
            ? OperationResult<UserSettingsModel>.Failure($"Failed to persist lingo settings for user '{command.UserId}'.")
            : OperationResult<UserSettingsModel>.Success(settings.ToModel());
    }
}

public record SaveUserSettingsCommand(
    string UserId,
    string TargetLocaleCode,
    IReadOnlyList<string> SourceLocaleCodes) : IOperationCommand<UserSettingsModel>;

public sealed class SaveUserSettingsCommandValidator :
    AbstractValidator<SaveUserSettingsCommand>
{
    public SaveUserSettingsCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");
        RuleFor(command => command.TargetLocaleCode)
            .NotEmpty()
            .WithMessage("TargetLocaleCode is required.");
        RuleFor(command => command.SourceLocaleCodes)
            .NotNull()
            .NotEmpty()
            .WithMessage("SourceLocaleCodes are required.");
    }
}
