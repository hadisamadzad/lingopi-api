using Lingopi.Core.Interfaces;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Operations.Auth;
using Minimals.Operations;

namespace Lingopi.Identity.Api.AuthEndpoints;

public class GetOwnershipStatusEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        // Endpoint for checking if ownership is done
        app.MapGroup(Routes.AuthBaseRoute)
            .WithSummary("Checks whether the service ownership stage is completed")
            .MapGet("ownership-check", async (IOperationService operations
                ) =>
            {
                // Operation
                var operationResult = await operations.GetOwnershipStatus
                    .ExecuteAsync(new GetOwnershipStatusCommand());

                // Result
                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.Ok(
                        new GetOwnershipStatusResponse(IsAlreadyOwned: operationResult.Value)),
                    _ => Results.InternalServerError(operationResult.Error),
                };
            })
            .WithTags(Routes.AuthEndpointGroupTag)
            .WithDescription("Returns a boolean indicating whether the one-off " +
                "ownership process is completed or not.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}

public record GetOwnershipStatusResponse(bool IsAlreadyOwned);
