using Lingopi.Lingo.Application.Extensions.Mappers;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.ReadModels;

namespace Lingopi.Lingo.Application.Operations.Languages;

public sealed class GetManagedLanguagesOperation(IRepositoryManager repository) :
    IOperation<GetManagedLanguagesCommand, List<LanguageReadModel>>
{
    public async Task<OperationResult<List<LanguageReadModel>>> ExecuteAsync(
        GetManagedLanguagesCommand command, CancellationToken? cancellation = null)
    {
        var entities = await repository.Languages.GetAllAsync();

        var readModels = entities.ConvertAll(entity => entity.ToReadModel());

        return OperationResult<List<LanguageReadModel>>.Success(readModels);
    }
}

public record GetManagedLanguagesCommand : IOperationCommand<List<LanguageReadModel>>;
