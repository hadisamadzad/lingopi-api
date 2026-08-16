using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.ReadModels;
using Minimals.Operations;

namespace Lingopi.Lingo.Application.Operations.Lingos;

public class GetLingoByIdOperation(IRepositoryManager repository) :
    IOperation<GetLingoByIdCommand, LingoModel>
{
    public async Task<OperationResult<LingoModel>> ExecuteAsync(
        GetLingoByIdCommand command, CancellationToken? cancellation = null)
    {
        var validator = new GetLingoByIdCommandValidator();
        var validationResult = await validator.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            return OperationResult<LingoModel>.ValidationFailure(
                validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
        }

        var entity = await repository.Lingos.GetByIdAsync(command.LingoId);
        if (entity is null)
        {
            return OperationResult<LingoModel>.NotFoundFailure("Lingo not found");
        }

        return OperationResult<LingoModel>.Success(entity.MapToLingoModel());
    }
}

public record GetLingoByIdCommand(string LingoId) : IOperationCommand;
