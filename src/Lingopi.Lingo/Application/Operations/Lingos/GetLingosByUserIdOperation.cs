using Damas.Operations;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.ReadModels;

namespace Lingopi.Lingo.Application.Operations.Lingos;

public class GetLingosByUserIdOperation(IRepositoryManager repository) :
    IOperation<GetLingosByUserIdCommand, List<LingoModel>>
{
    public async Task<OperationResult<List<LingoModel>>> ExecuteAsync(
        GetLingosByUserIdCommand command, CancellationToken? cancellation = null)
    {
        // Get lingos by user ID
        var entities = await repository.Lingos.GetByUserIdAsync(command.UserId);

        if (entities.Count == 0)
        {
            return OperationResult<List<LingoModel>>.Success([]);
        }

        // Get unique language IDs from all entities
        var languageIds = entities
            .SelectMany(e => new[] { e.Languages.SourceLanguageId, e.Languages.TargetLanguageId })
            .Distinct()
            .ToList();

        // Fetch all required language entities
        var languageEntities = new Dictionary<string, LanguageEntity>();
        foreach (var languageId in languageIds)
        {
            var language = await repository.Languages.GetByIdAsync(languageId);
            if (language != null)
            {
                languageEntities[languageId] = language;
            }
        }

        // Map to models
        var models = entities.Select(entity => entity.MapToLingoModel(languageEntities)).ToList();

        return OperationResult<List<LingoModel>>.Success(models);
    }
}

public record GetLingosByUserIdCommand(string UserId) : IOperationCommand;
