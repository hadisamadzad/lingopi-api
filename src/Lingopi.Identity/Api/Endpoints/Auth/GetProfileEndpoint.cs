using Lingopi.Identity.Application.Operations.Auth;
using Lingopi.Identity.Application.Types.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Identity.Api.Endpoints.Auth;

public class GetProfileEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        // Endpoint for getting user profile
        app.MapGet("api/auth/profile", async (IOperationMediator operations,
                [FromHeader(Name = "User-Id")] string userId) =>
            {
                // Operation
                var operationResult = await operations.ExecuteAsync(
                    new GetUserProfileCommand(UserId: userId));
                var user = operationResult.Value!;

                // Result
                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.Ok(
                        new GetUserProfileResponse(
                            UserId: user.UserId,
                            Email: user.Email,
                            IsEmailConfirmed: user.IsEmailConfirmed,
                            FirstName: user.FirstName,
                            LastName: user.LastName,
                            TimeZoneId: user.TimeZoneId,
                            Theme: user.Theme,
                            FullName: user.FullName,
                            Role: user.Role,
                            Status: user.Status,
                            LastLoginDate: user.LastLoginDate,
                            CreatedAt: user.CreatedAt,
                            UpdatedAt: user.UpdatedAt)),
                    OperationStatus.Invalid => Results.BadRequest(operationResult.Error),
                    OperationStatus.NotFound => Results.UnprocessableEntity(operationResult.Error),
                    _ => Results.InternalServerError(operationResult.Error),
                };
            })
            .WithTags("Auth")
            .WithSummary("Gets the current user's profile")
            .WithDescription("Returns the profile information for the authenticated user.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}

public sealed record GetUserProfileResponse(
    string UserId,
    string Email,
    bool IsEmailConfirmed,
    string? FirstName,
    string? LastName,
    string? TimeZoneId,
    ThemePreference? Theme,
    string FullName,
    Role Role,
    UserState Status,
    DateTime? LastLoginDate,
    DateTime CreatedAt,
    DateTime UpdatedAt);
