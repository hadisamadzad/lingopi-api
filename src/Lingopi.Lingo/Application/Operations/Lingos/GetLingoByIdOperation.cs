using Damas.Operations;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.ReadModels;

namespace Lingopi.Lingo.Application.Operations.Lingos;

public class GetLingoByIdOperation(IRepositoryManager repository) :
    IOperation<GetLingoByIdCommand, LingoModel>
{
    public async Task<OperationResult<LingoModel>> ExecuteAsync(
        GetLingoByIdCommand command, CancellationToken? cancellation = null)
    {
        // Validate command
        var validator = new GetLingoByIdCommandValidator();
        var validationResult = await validator.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            return OperationResult<LingoModel>.ValidationFailure(
                validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
        }

        // Get lingo by ID
        var entity = await repository.Lingos.GetByIdAsync(command.LingoId);
        if (entity is null)
        {
            return OperationResult<LingoModel>.NotFoundFailure("Lingo not found");
        }

        var sourceLanguageEntity = await repository.Languages
            .GetByIdAsync(entity.Languages.SourceLanguageId);
        var targetLanguageEntity = await repository.Languages
            .GetByIdAsync(entity.Languages.TargetLanguageId);

        // Map to model
        var languageEntities = new Dictionary<string, LanguageEntity>();
        if (sourceLanguageEntity != null)
            languageEntities[entity.Languages.SourceLanguageId] = sourceLanguageEntity;
        if (targetLanguageEntity != null)
            languageEntities[entity.Languages.TargetLanguageId] = targetLanguageEntity;

        var model = entity.MapToLingoModel(languageEntities);

        return OperationResult<LingoModel>.Success(model);
    }
}

public record GetLingoByIdCommand(string LingoId) : IOperationCommand;
