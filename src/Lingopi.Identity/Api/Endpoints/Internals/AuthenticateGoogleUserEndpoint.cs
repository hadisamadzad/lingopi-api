using Lingopi.Identity.Application.Operations.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Identity.Api.Endpoints.Internals;

/// <summary>Internal endpoint called by the Gateway after Google has verified the user.</summary>
public class AuthenticateGoogleUserEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapPost("api/internal/auth/google", async (IOperationMediator operations,
            HttpContext context, [FromBody] AuthenticateGoogleUserRequest request) =>
        {
            var operationResult = await operations.ExecuteAsync(new AuthenticateGoogleUserCommand
            (
                InternalAuthSecret: context.Request.Headers["Lingopi-Internal-Auth"].ToString(),
                Email: request.Email,
                FirstName: request.FirstName,
                LastName: request.LastName
            ));

            return operationResult.Status switch
            {
                OperationStatus.Completed => Results.Ok(operationResult.Value),
                OperationStatus.Invalid => Results.BadRequest(operationResult.Error),
                OperationStatus.Unauthorized => Results.Unauthorized(),
                _ => Results.InternalServerError(operationResult.Error)
            };
        });
    }
}

public record AuthenticateGoogleUserRequest(string Email, string? FirstName, string? LastName);
