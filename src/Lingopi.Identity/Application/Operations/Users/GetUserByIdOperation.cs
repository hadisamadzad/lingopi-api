using Damas.Operations;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Types.Models.Users;

namespace Lingopi.Identity.Application.Operations.Users;

public class GetUserByIdOperation(IRepositoryManager repository) :
    IOperation<GetUserByIdCommand, UserModel>
{
    public async Task<OperationResult<UserModel>> ExecuteAsync(
        GetUserByIdCommand command, CancellationToken? cancellation = null)
    {
        // Get
        var entity = await repository.Users.GetByIdAsync(command.UserId);
        if (entity is null)
            return OperationResult<UserModel>.NotFoundFailure("User not found");

        // Mapping
        var model = entity.MapToUserModel();

        return OperationResult<UserModel>.Success(model);
    }
}

public record GetUserByIdCommand(string UserId) : IOperationCommand;
