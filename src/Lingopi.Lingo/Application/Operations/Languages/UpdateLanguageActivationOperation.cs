using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.ReadModels;

namespace Lingopi.Lingo.Application.Operations.Languages;

public sealed class UpdateLanguageActivationOperation(IRepositoryManager repository) :
    IOperation<UpdateLanguageActivationCommand, LanguageReadModel>
{
    public async Task<OperationResult<LanguageReadModel>> ExecuteAsync(
        UpdateLanguageActivationCommand command, CancellationToken? cancellation = null)
    {
        var entity = await repository.Languages.GetByIdAsync(command.Id);
        if (entity is null)
        {
            return OperationResult<LanguageReadModel>.NotFoundFailure(
                $"Language '{command.Id}' was not found.");
        }

        var now = DateTime.UtcNow;
        var isActivating = command.IsActive && !entity.IsActive;
        entity.IsActive = command.IsActive;
        entity.UpdatedAt = now;
        if (isActivating)
        {
            entity.LastActivatedAt = now;
        }

        var updated = await repository.Languages.UpdateAsync(entity);
        if (!updated)
        {
            return OperationResult<LanguageReadModel>.Failure($"Failed to update language '{command.Id}'.");
        }

        return OperationResult<LanguageReadModel>.Success(entity.ToReadModel());
    }
}

public record UpdateLanguageActivationCommand(string Id, bool IsActive) : IOperationCommand<LanguageReadModel>;
