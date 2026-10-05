using Lingopi.Identity.Application.Extensions.Mappers;
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
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            return OperationResult<UserModel>.ValidationFailure("Invalid userId");
        }

        // Get
        var entity = await repository.Users.GetByIdAsync(command.UserId);
        if (entity is null)
        {
            return OperationResult<UserModel>.NotFoundFailure("User not found");
        }

        // Mapping
        var model = entity.ToModel();

        return OperationResult<UserModel>.Success(model);
    }
}

public record GetUserProfileCommand(string UserId) : IOperationCommand<UserModel>;
