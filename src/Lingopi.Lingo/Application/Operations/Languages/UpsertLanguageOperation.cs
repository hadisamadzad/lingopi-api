using Lingopi.Core.Helpers;
using Lingopi.Lingo.Application.Helpers;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.ReadModels;
using Lingopi.Lingo.Application.Models.ValueObjects;

namespace Lingopi.Lingo.Application.Operations.Languages;

public sealed class UpsertLanguageOperation(IRepositoryManager repository) :
    IOperation<UpsertLanguageCommand, LanguageReadModel>
{
    public async Task<OperationResult<LanguageReadModel>> ExecuteAsync(
        UpsertLanguageCommand command, CancellationToken? cancellation = null)
    {
        var validationError = Validate(command);
        if (validationError is not null)
        {
            return OperationResult<LanguageReadModel>.ValidationFailure(validationError);
        }

        var code = command.Code.Trim().ToLowerInvariant();

        var languageId = string.IsNullOrWhiteSpace(command.Id) ? UidHelper.GenerateNewId("lang") : command.Id;
        var existing = await repository.Languages.GetByIdAsync(languageId);

        var duplicateLanguage = string.IsNullOrWhiteSpace(command.Id)
            ? await repository.Languages.ExistsByCodeAsync(code)
            : await repository.Languages.ExistsByCodeAsync(code, languageId);
        if (duplicateLanguage)
        {
            return OperationResult<LanguageReadModel>.ValidationFailure(
                $"Language code '{code}' is already in use.");
        }

        var locales = command.Locales.ToList().ConvertAll(locale => new LocaleValue(
            LocaleCodeNormalizer.NormalizeCanonical(locale.Code),
            locale.Region.Trim().ToUpperInvariant(),
            locale.Name.Trim(),
            locale.NativeName.Trim(),
            locale.IsRightToLeft,
            locale.IsActive));

        var duplicateLocale = locales
            .GroupBy(locale => locale.Code, StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1);
        if (duplicateLocale)
        {
            return OperationResult<LanguageReadModel>.ValidationFailure(
                "Locale codes must be unique within a language.");
        }

        foreach (var locale in locales)
        {
            var localeExists = await repository.Languages.ExistsByLocaleCodeAsync(locale.Code, languageId);
            if (localeExists)
            {
                return OperationResult<LanguageReadModel>.ValidationFailure(
                    $"Locale code '{locale.Code}' is already assigned to another language.");
            }
        }

        var entity = new LanguageEntity
        {
            Id = languageId,
            Code = code,
            Name = command.Name.Trim(),
            NativeName = command.NativeName.Trim(),
            IsActive = command.IsActive,
            Locales = locales
        };

        if (existing is null)
        {
            await repository.Languages.InsertAsync(entity);
            return OperationResult<LanguageReadModel>.Success(entity.ToReadModel());
        }

        var updated = await repository.Languages.UpdateAsync(entity);
        return updated
            ? OperationResult<LanguageReadModel>.Success(entity.ToReadModel())
            : OperationResult<LanguageReadModel>.Failure(
                $"Failed to update language '{languageId}'.");
    }

    private static string? Validate(UpsertLanguageCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Code) ||
            string.IsNullOrWhiteSpace(command.Name) ||
            string.IsNullOrWhiteSpace(command.NativeName))
        {
            return "Language code, name, and native name are required.";
        }

        if (command.Locales.Any(locale =>
            string.IsNullOrWhiteSpace(locale.Code) ||
            string.IsNullOrWhiteSpace(locale.Region) ||
            string.IsNullOrWhiteSpace(locale.Name) ||
            string.IsNullOrWhiteSpace(locale.NativeName)))
        {
            return "Every locale requires a code, region, name, and native name.";
        }

        return null;
    }
}

public record UpsertLanguageCommand(
    string? Id,
    string Code,
    string Name,
    string NativeName,
    bool IsActive,
    IReadOnlyList<LocaleRequest> Locales) : IOperationCommand<LanguageReadModel>;

public record LocaleRequest(
    string Code,
    string Region,
    string Name,
    string NativeName,
    bool IsRightToLeft,
    bool IsActive);
