using Lingopi.Identity.Api.Extensions;
using Lingopi.Identity.Application.Operations.Auth;
using Lingopi.Identity.Core.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Identity.Api.Endpoints.Auth;

public class LoginEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        // Endpoint for logging in
        app.MapPost("api/auth/login", async (IOperationMediator operations,
                [FromBody] LoginRequest request) =>
            {
                // Operation
                var operationResult = await operations.ExecuteAsync(new LoginCommand
                (
                    Email: request.Email.Trim(),
                    Password: request.Password.Trim()
                ));
                var login = operationResult.Value!;

                // Result
                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.Ok(
                        new LoginResponse(
                            Email: login.Email,
                            FullName: login.FullName,
                            AccessToken: login.AccessToken))
                        .WithCookie("refreshToken", login.RefreshToken,
                            CookieConfiguration.GetRefreshTokenOptions(
                                login.RefreshTokenLifetime,
                                isProduction: app.Environment.IsProduction())),

                    OperationStatus.Invalid => Results.BadRequest(operationResult.Error),
                    OperationStatus.NotFound => Results.UnprocessableEntity(operationResult.Error),
                    OperationStatus.Unauthorized => Results.Unauthorized(),
                    OperationStatus.Failed => Results.UnprocessableEntity(operationResult.Error),
                    _ => Results.InternalServerError(operationResult.Error),
                };
            })
            .WithTags("Auth")
            .WithSummary("Login endpoint")
            .WithDescription("Authenticates a user and returns access and refresh tokens.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}

public record LoginRequest(string Email, string Password);

public sealed record LoginResponse(
    string Email,
    string FullName,
    string AccessToken);
