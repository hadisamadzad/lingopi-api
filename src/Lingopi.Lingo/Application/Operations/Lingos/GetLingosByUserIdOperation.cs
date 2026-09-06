using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.ReadModels;

namespace Lingopi.Lingo.Application.Operations.Lingos;

public class GetLingosByUserIdOperation(IRepositoryManager repository) :
    IOperation<GetLingosByUserIdCommand, List<LingoModel>>
{
    public async Task<OperationResult<List<LingoModel>>> ExecuteAsync(
        GetLingosByUserIdCommand command, CancellationToken? cancellation = null)
    {
        var entities = await repository.Lingos.GetByUserIdAsync(command.UserId);

        if (entities.Count == 0)
        {
            return OperationResult<List<LingoModel>>.Success([]);
        }

        var models = entities.Select(entity => entity.MapToLingoModel()).ToList();

        return OperationResult<List<LingoModel>>.Success(models);
    }
}

public record GetLingosByUserIdCommand(string UserId) : IOperationCommand<List<LingoModel>>;
