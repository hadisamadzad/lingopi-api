using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.ReadModels;

namespace Lingopi.Lingo.Application.Operations.Languages;

public sealed class SetLanguageActivationOperation(IRepositoryManager repository) :
    IOperation<SetLanguageActivationCommand, LanguageReadModel>
{
    public async Task<OperationResult<LanguageReadModel>> ExecuteAsync(
        SetLanguageActivationCommand command, CancellationToken? cancellation = null)
    {
        var entity = await repository.Languages.GetByIdAsync(command.Id);
        if (entity is null)
        {
            return OperationResult<LanguageReadModel>.NotFoundFailure(
                $"Language '{command.Id}' was not found.");
        }

        entity.IsActive = command.IsActive;

        var updated = await repository.Languages.UpdateAsync(entity);
        if (!updated)
        {
            return OperationResult<LanguageReadModel>.Failure($"Failed to update language '{command.Id}'.");
        }

        return OperationResult<LanguageReadModel>.Success(entity.ToReadModel());
    }
}

public record SetLanguageActivationCommand(string Id, bool IsActive) : IOperationCommand<LanguageReadModel>;
