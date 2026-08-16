using Lingopi.Core.Interfaces;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Operations.Users;
using Lingopi.Identity.Application.Types.Entities;
using Microsoft.AspNetCore.Mvc;
using Minimals.Operations;

namespace Lingopi.Identity.Api.Endpoints.Users;

public class UpdateUserStatusEndpoint : IEndpoint
{
    public record UpdateUserStatusRequest(UserState State);

    public void MapEndpoints(WebApplication app)
    {
        app.MapGroup(Routes.UserBaseRoute)
            .WithSummary("Update User Status")
            .MapPatch("{userId}/status", async (IOperationService operations,
                [FromRoute] string userId,
                [FromHeader] string requestedBy,
                [FromBody] UpdateUserStatusRequest request) =>
            {
                // Operation
                var operationResult = await operations.UpdateUserState.ExecuteAsync(
                    new UpdateUserStatusCommand(
                        requestedBy,
                        userId,
                        request.State
                    ));

                // Result
                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.NoContent(),
                    OperationStatus.Invalid => Results.BadRequest(operationResult.Error),
                    OperationStatus.NotFound => Results.Forbid(),
                    _ => Results.InternalServerError(operationResult.Error),
                };
            })
            .WithTags(Routes.UserEndpointGroupTag)
            .WithDescription("Updates the status of a user (e.g., Active, Inactive, Suspended).")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
