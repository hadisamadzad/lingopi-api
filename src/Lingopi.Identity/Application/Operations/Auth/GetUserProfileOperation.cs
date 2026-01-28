using Damas.Operations;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Types.Models.Users;

namespace Lingopi.Identity.Application.Operations.Auth;

public class GetUserProfileOperation(
    IRepositoryManager repository) :
    IOperation<GetUserProfileCommand, UserModel>
{
    public async Task<OperationResult<UserModel>> ExecuteAsync(
        GetUserProfileCommand command, CancellationToken? cancellation = null)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(command.RequestedById))
            return OperationResult<UserModel>.ValidationFailure(["Invalid userId"]);

        // Get
        var user = await repository.Users.GetByIdAsync(command.RequestedById);
        if (user is null)
            return OperationResult<UserModel>.NotFoundFailure("User not found");

        // Mapping
        var response = user.MapToUserModel();

        return OperationResult<UserModel>.Success(response);
    }
}

public record GetUserProfileCommand(string RequestedById) : IOperationCommand;
