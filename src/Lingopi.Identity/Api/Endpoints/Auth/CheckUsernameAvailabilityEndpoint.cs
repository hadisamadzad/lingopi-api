using Lingopi.Identity.Application.Operations.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Identity.Api.Endpoints.Auth;

public class CheckUsernameAvailabilityEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        // Endpoint for checking username availability
        app.MapGet("api/auth/username-check", async (IOperationMediator operations,
                [FromQuery] string email) =>
            {
                // Operation
                var operationResult = await operations.ExecuteAsync(
                    new CheckUsernameCommand(Email: email));

                // Result
                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.Ok(
                        new CheckUsernameAvailabilityResponse(
                            IsAvailable: operationResult.Value)),
                    OperationStatus.Invalid => Results.BadRequest(operationResult.Error),
                    _ => Results.InternalServerError(operationResult.Error),
                };
            })
            .WithTags("Auth")
            .WithSummary("Check Username Availability")
            .WithDescription("Checks if a username (email) is available.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}

public sealed record CheckUsernameAvailabilityResponse(bool IsAvailable);
