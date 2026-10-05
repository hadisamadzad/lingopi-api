using Lingopi.Lingo.Application.Extensions.Mappers;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.ReadModels;

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

        var model = entity.ToModel();

        return OperationResult<LingoModel>.Success(model);
    }
}

public record GetLingoByIdCommand(string UserId, string LingoId) : IOperationCommand<LingoModel>;
