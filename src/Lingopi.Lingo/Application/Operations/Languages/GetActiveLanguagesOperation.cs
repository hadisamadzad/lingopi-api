using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.ReadModels;

namespace Lingopi.Lingo.Application.Operations.Languages;

public sealed class GetActiveLanguagesOperation(IRepositoryManager repository) :
    IOperation<GetActiveLanguagesCommand, List<LanguageReadModel>>
{
    public async Task<OperationResult<List<LanguageReadModel>>> ExecuteAsync(
        GetActiveLanguagesCommand command, CancellationToken? cancellation = null)
    {
        var entities = await repository.Languages.GetActiveLanguagesAsync();

        var readModels = entities.ConvertAll(language => language.ToReadModel());

        return OperationResult<List<LanguageReadModel>>.Success(readModels);
    }
}

public record GetActiveLanguagesCommand : IOperationCommand<List<LanguageReadModel>>;
