using Lingopi.Identity.Application.Operations.Auth;
using Lingopi.Identity.Application.Operations.PasswordReset;
using Lingopi.Identity.Application.Operations.Users;

namespace Lingopi.Identity.Application.Interfaces;

public interface IOperationService
{
    // Auth
    CheckUsernameOperation CheckUsername { get; }
    GetUserProfileOperation GetUserProfile { get; }
    GetOwnershipStatusOperation GetOwnershipStatus { get; }
    LoginOperation Login { get; }
    RegisterOperation Register { get; }
    RefreshAccessTokenOperation GetNewAccessToken { get; }
    RevokeRefreshTokenOperation RevokeRefreshToken { get; }
    AuthenticateGoogleUserOperation AuthenticateGoogleUser { get; }

    // Users
    GetUserByIdOperation GetUserById { get; }
    UpdateUserOperation UpdateUser { get; }
    UpdateUserPasswordOperation UpdateUserPassword { get; }

    // Password Reset
    SendPasswordResetEmailOperation SendPasswordResetEmail { get; }
    GetPasswordResetEmailOperation GetPasswordResetEmail { get; }
    ResetPasswordOperation ResetPassword { get; }
}