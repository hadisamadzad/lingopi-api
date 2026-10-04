using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.ReadModels;

namespace Lingopi.Lingo.Application.Operations.Languages;

public sealed class CreateLanguageOperation(IRepositoryManager repository) :
    IOperation<CreateLanguageCommand, LanguageReadModel>
{
    public async Task<OperationResult<LanguageReadModel>> ExecuteAsync(
        CreateLanguageCommand command, CancellationToken? cancellation = null)
    {
        var validationError = Validate(command);
        if (validationError is not null)
        {
            return OperationResult<LanguageReadModel>.ValidationFailure(validationError);
        }

        var languageCode = command.LanguageCode.Trim().ToLowerInvariant();
        var regionCode = command.RegionCode.Trim().ToUpperInvariant();
        var localeCode = $"{languageCode}-{regionCode}";
        var localeExists = await repository.Languages.ExistsByLocaleCodeAsync(localeCode);
        if (localeExists)
        {
            return OperationResult<LanguageReadModel>.ValidationFailure(
                $"Locale '{localeCode}' is already in use.");
        }

        var now = DateTime.UtcNow;
        var generatedId = $"locale-{localeCode}".ToLowerInvariant();
        var entity = new LanguageEntity
        {
            Id = generatedId,
            LocaleCode = localeCode,
            LanguageCode = languageCode,
            RegionCode = regionCode,
            Name = command.Name.Trim(),
            NativeName = command.NativeName.Trim(),
            IsRightToLeft = command.IsRightToLeft,
            IsActive = command.IsActive,
            CreatedAt = now,
            UpdatedAt = now,
            LastActivatedAt = command.IsActive ? now : null
        };

        await repository.Languages.InsertAsync(entity);
        return OperationResult<LanguageReadModel>.Success(entity.ToReadModel());
    }

    private static string? Validate(CreateLanguageCommand command)
    {
        var isLanguageCodeMissing = string.IsNullOrWhiteSpace(command.LanguageCode);
        var isRegionCodeMissing = string.IsNullOrWhiteSpace(command.RegionCode);
        var isNameMissing = string.IsNullOrWhiteSpace(command.Name);
        var isNativeNameMissing = string.IsNullOrWhiteSpace(command.NativeName);

        if (isLanguageCodeMissing || isRegionCodeMissing || isNameMissing || isNativeNameMissing)
        {
            return "Language code, region code, name, and native name are required.";
        }

        var languageCode = command.LanguageCode.Trim();
        var regionCode = command.RegionCode.Trim();
        var hasValidCodeLength = languageCode.Length is 2 or 3;
        var codeContainsOnlyLetters = languageCode.All(char.IsAsciiLetter);
        var isValidRegionCode = regionCode.Length == 2 && regionCode.All(char.IsAsciiLetter);
        var isValidNumericRegion = regionCode.Length == 3 && regionCode.All(char.IsAsciiDigit);

        if (!hasValidCodeLength || !codeContainsOnlyLetters)
        {
            return "Language code must contain two or three letters.";
        }

        if (!isValidRegionCode && !isValidNumericRegion)
        {
            return "Region code must contain two letters or three digits.";
        }

        return null;
    }
}

public record CreateLanguageCommand(
    string LanguageCode,
    string RegionCode,
    string Name,
    string NativeName,
    bool IsRightToLeft,
    bool IsActive) : IOperationCommand<LanguageReadModel>;
