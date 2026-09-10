using Lingopi.Identity.Application.Operations.Auth;

namespace Lingopi.Identity.Api.Endpoints.Auth;

public class GetOwnershipStatusEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        // Endpoint for checking if ownership is done
        app.MapGet("api/auth/ownership-check", async (IOperationMediator operations
                ) =>
            {
                // Operation
                var operationResult = await operations.ExecuteAsync(new GetOwnershipStatusCommand());

                // Result
                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.Ok(
                        new GetOwnershipStatusResponse(IsAlreadyOwned: operationResult.Value)),
                    _ => Results.InternalServerError(operationResult.Error),
                };
            })
            .WithTags("Auth")
            .WithSummary("Checks whether the service ownership stage is completed")
            .WithDescription("Returns a boolean indicating whether the one-off " +
                "ownership process is completed or not.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}

public record GetOwnershipStatusResponse(bool IsAlreadyOwned);
