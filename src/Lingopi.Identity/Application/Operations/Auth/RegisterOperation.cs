using Damas.Operations;
using FluentValidation;
using Identity.Application.Helpers;
using Lingopi.Core.Helpers;
using Lingopi.Identity.Application.Helpers;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Types.Entities;
using Lingopi.Identity.Application.Types.Models.Auth;

namespace Lingopi.Identity.Application.Operations.Auth;

public class RegisterOperation(IRepositoryManager repository)
    : IOperation<RegisterCommand, RegisterResult>
{
    public async Task<OperationResult<RegisterResult>> ExecuteAsync(
        RegisterCommand command, CancellationToken? cancellation = null)
    {
        // Validation
        var validation = new RegisterValidator().Validate(command);
        if (!validation.IsValid)
        {
            return OperationResult<RegisterResult>.ValidationFailure([.. validation.GetErrorMessages()]);
        }

        // Check initial ownership
        // NOTE The first registered user becomes the Owner, from the second one onwards, they become regular Users
        var isFirstUser = !await repository.Users.AnyAsync();
        var userRole = isFirstUser ? Role.Owner : Role.User;

        // Check for existing user
        var isExistingUser = await repository.Users.GetByEmailAsync(command.Email);
        if (isExistingUser is not null)
        {
            return OperationResult<RegisterResult>.Failure("A user with this email already exists");
        }

        var user = new UserEntity
        {
            Id = UidHelper.GenerateNewId("user"),
            Email = command.Email.ToLower(),
            PasswordHash = PasswordHelper.Hash(command.Password),
            Status = UserState.Active, // TODO isFirstUser ? UserState.Active : UserState.Inactive,
            Role = userRole,
            SecurityStamp = UserHelper.CreateUserStamp(),
            ConcurrencyStamp = UserHelper.CreateUserStamp(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await repository.Users.InsertAsync(user);

        var result = new RegisterResult
        {
            UserId = user.Id,
            Email = user.Email,
            ActivationToken = "fake-token"
        };

        return OperationResult<RegisterResult>.Success(result);
    }
}

public record RegisterCommand(string Email, string Password) : IOperationCommand;

public class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email)
            .EmailAddress();

        RuleFor(x => x.Password)
            .Must(password => PasswordHelper.CheckStrength(password) >= PasswordScore.Medium)
            .WithMessage("Password is not strong enough");
    }
}
