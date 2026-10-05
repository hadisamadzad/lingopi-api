using Lingopi.Lingo.Application.Extensions.Mappers;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.ReadModels;

namespace Lingopi.Lingo.Application.Operations.Languages;

public sealed class UpdateLanguageOperation(IRepositoryManager repository) :
    IOperation<UpdateLanguageCommand, LanguageReadModel>
{
    public async Task<OperationResult<LanguageReadModel>> ExecuteAsync(
        UpdateLanguageCommand command, CancellationToken? cancellation = null)
    {
        var validationError = Validate(command);
        if (validationError is not null)
        {
            return OperationResult<LanguageReadModel>.ValidationFailure(validationError);
        }

        var entity = await repository.Languages.GetByIdAsync(command.Id);
        if (entity is null)
        {
            return OperationResult<LanguageReadModel>.NotFoundFailure(
                $"Locale '{command.Id}' was not found.");
        }

        var now = DateTime.UtcNow;
        var isActivating = command.IsActive && !entity.IsActive;
        entity.Name = command.Name.Trim();
        entity.NativeName = command.NativeName.Trim();
        entity.IsRightToLeft = command.IsRightToLeft;
        entity.IsActive = command.IsActive;
        entity.UpdatedAt = now;
        if (isActivating)
        {
            entity.LastActivatedAt = now;
        }

        var updated = await repository.Languages.UpdateAsync(entity);
        return updated
            ? OperationResult<LanguageReadModel>.Success(entity.ToReadModel())
            : OperationResult<LanguageReadModel>.Failure(
                $"Failed to update locale '{entity.Id}'.");
    }

    private static string? Validate(UpdateLanguageCommand command)
    {
        var isIdMissing = string.IsNullOrWhiteSpace(command.Id);
        var isNameMissing = string.IsNullOrWhiteSpace(command.Name);
        var isNativeNameMissing = string.IsNullOrWhiteSpace(command.NativeName);

        if (isIdMissing || isNameMissing || isNativeNameMissing)
        {
            return "Locale id, name, and native name are required.";
        }

        return null;
    }
}

public record UpdateLanguageCommand(
    string Id,
    string Name,
    string NativeName,
    bool IsRightToLeft,
    bool IsActive) : IOperationCommand<LanguageReadModel>;
