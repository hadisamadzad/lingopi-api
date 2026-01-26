using Lingopi.Core.Utilities.OperationResult;
using Lingopi.Lingo.Application.Interfaces;
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

        // Map to models
        var models = entities.MapToLingoModels().ToList();

        return OperationResult<List<LingoModel>>.Success(models);
    }
}

public record GetLingosByUserIdCommand(string UserId) : IOperationCommand;
