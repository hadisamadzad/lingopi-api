using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.ReadModels;
using Minimals.Operations;

namespace Lingopi.Lingo.Application.Operations.Lingos;

public class GetLingoByIdOperation(IRepositoryManager repository) :
    IOperation<GetLingoByIdCommand, LingoModel>
{
    public async Task<OperationResult<LingoModel>> ExecuteAsync(GetLingoByIdCommand command,
        CancellationToken? cancellation = null)
    {
        var validationResult = await new GetLingoByIdCommandValidator()
            .ValidateAsync(command, cancellation ?? CancellationToken.None);
        if (!validationResult.IsValid)
        {
            return OperationResult<LingoModel>.ValidationFailure([.. validationResult.Errors.Select(e => e.ErrorMessage)]);
        }

        var entity = await repository.Lingos.GetByIdAsync(command.LingoId);
        if (entity is null || entity.UserId != command.UserId)
        {
            return OperationResult<LingoModel>.NotFoundFailure("Lingo not found");
        }

        var result = entity.MapToLingoModel();

        return OperationResult<LingoModel>.Success(result);
    }
}

public record GetLingoByIdCommand(string UserId, string LingoId) : IOperationCommand;
