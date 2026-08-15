using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Operations.Auth;
using Lingopi.Identity.Application.Operations.PasswordReset;
using Lingopi.Identity.Application.Operations.Users;
using Lingopi.Identity.Application.Types.Models.Auth;
using Lingopi.Identity.Application.Types.Models.Users;
using Minimals.Operations;

#pragma warning disable S107 // Avoid excessive complexity

namespace Lingopi.Identity.Application.Operations;

public class OperationService(
    IOperation<CheckUsernameCommand, bool> checkUsername,
    IOperation<GetUserProfileCommand, UserModel> getUserProfile,
    IOperation<GetOwnershipStatusCommand, bool> getOwnershipStatus,
    IOperation<LoginCommand, LoginResult> login,
    IOperation<RegisterCommand, RegisterResult> register,
    IOperation<RefreshAccessTokenCommand, RefreshAccessTokenResult> getNewAccessToken,
    IOperation<RevokeRefreshTokenCommand, NoResult> revokeRefreshToken,
    IOperation<AuthenticateGoogleUserCommand, AuthenticateGoogleUserResult> authenticateGoogleUser,
    IOperation<CreateUserCommand, string> createUser,
    IOperation<GetUserByIdCommand, UserModel> getUserById,
    IOperation<UpdateUserCommand, NoResult> updateUser,
    IOperation<UpdateUserPasswordCommand, NoResult> updateUserPassword,
    IOperation<UpdateUserStatusCommand, NoResult> updateUserState,
    IOperation<SendPasswordResetEmailCommand, NoResult> sendPasswordResetEmail,
    IOperation<GetPasswordResetEmailCommand, string> getPasswordResetInfo,
    IOperation<ResetPasswordCommand, NoResult> resetPassword
) : IOperationService
{
    // Auth
    public CheckUsernameOperation CheckUsername { get; } =
        (checkUsername as CheckUsernameOperation)!;
    public GetUserProfileOperation GetUserProfile { get; } =
        (getUserProfile as GetUserProfileOperation)!;
    public GetOwnershipStatusOperation GetOwnershipStatus { get; } =
        (getOwnershipStatus as GetOwnershipStatusOperation)!;
    public LoginOperation Login { get; } =
        (login as LoginOperation)!;
    public RegisterOperation Register { get; } =
        (register as RegisterOperation)!;
    public RefreshAccessTokenOperation GetNewAccessToken { get; } =
        (getNewAccessToken as RefreshAccessTokenOperation)!;
    public RevokeRefreshTokenOperation RevokeRefreshToken { get; } =
        (revokeRefreshToken as RevokeRefreshTokenOperation)!;
    public AuthenticateGoogleUserOperation AuthenticateGoogleUser { get; } =
        (authenticateGoogleUser as AuthenticateGoogleUserOperation)!;

    // Users
    public CreateUserOperation CreateUser { get; } =
        (createUser as CreateUserOperation)!;
    public GetUserByIdOperation GetUserById { get; } =
        (getUserById as GetUserByIdOperation)!;
    public UpdateUserOperation UpdateUser { get; } =
        (updateUser as UpdateUserOperation)!;
    public UpdateUserPasswordOperation UpdateUserPassword { get; } =
        (updateUserPassword as UpdateUserPasswordOperation)!;
    public UpdateUserStatusOperation UpdateUserState { get; } =
        (updateUserState as UpdateUserStatusOperation)!;

    // Password Reset
    public SendPasswordResetEmailOperation SendPasswordResetEmail { get; } =
        (sendPasswordResetEmail as SendPasswordResetEmailOperation)!;
    public GetPasswordResetEmailOperation GetPasswordResetEmail { get; } =
        (getPasswordResetInfo as GetPasswordResetEmailOperation)!;
    public ResetPasswordOperation ResetPassword { get; } =
        (resetPassword as ResetPasswordOperation)!;
}
