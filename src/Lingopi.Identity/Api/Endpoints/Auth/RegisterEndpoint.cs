using Lingopi.Identity.Application.Operations.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Identity.Api.Endpoints.Auth;

public class RegisterEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        // Endpoint for registration
        app.MapPost("api/auth/register", async (IOperationMediator operations,
                [FromBody] RegisterRequest request) =>
            {
                // Operation
                var operationResult = await operations.ExecuteAsync(new RegisterCommand
                (
                    Email: request.Email.Trim(),
                    Password: request.Password.Trim()
                ));
                var registration = operationResult.Value!;

                // Result
                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.Ok(
                        new RegisterResponse(UserId: registration.UserId)),
                    OperationStatus.Invalid => Results.BadRequest(operationResult.Error),
                    OperationStatus.Failed => Results.UnprocessableEntity(operationResult.Error),
                    _ => Results.InternalServerError(operationResult.Error),
                };
            })
            .WithTags("Auth")
            .WithSummary("Registers the owner")
            .WithDescription("Registers the first user as the owner.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}

public record RegisterRequest(string Email, string Password);

public sealed record RegisterResponse(string UserId);
