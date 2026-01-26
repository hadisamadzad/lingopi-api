using Lingopi.Core.Utilities.OperationResult;
using Lingopi.Lingo.Application.Interfaces;
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

        // Map to model
        var model = entity.MapToLingoModel();

        return OperationResult<LingoModel>.Success(model);
    }
}

public record GetLingoByIdCommand(string LingoId) : IOperationCommand;
