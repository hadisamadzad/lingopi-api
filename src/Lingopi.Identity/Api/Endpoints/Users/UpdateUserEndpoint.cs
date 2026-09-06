using Lingopi.Identity.Application.Operations.Users;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Identity.Api.Endpoints.Users;

public class UpdateUserEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGroup(Routes.UserBaseRoute)
            .WithSummary("Update user details by admins")
            .MapPatch("{userId}", async (IOperationMediator operations,
                [FromRoute] string userId,
                [FromHeader(Name = "User-Id")] string authenticatedUserId,
                [FromBody] UpdateUserRequest request) =>
            {
                // Operation
                var operationResult = await operations.ExecuteAsync(
                    new UpdateUserCommand(
                        authenticatedUserId,
                        userId,
                        request.FirstName,
                        request.LastName));

                // Result
                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.NoContent(),
                    OperationStatus.Invalid => Results.BadRequest(operationResult.Error),
                    OperationStatus.Unauthorized => Results.Forbid(),
                    OperationStatus.NotFound => Results.UnprocessableEntity(operationResult.Error),
                    _ => Results.InternalServerError(operationResult.Error),
                };
            })
            .WithTags(Routes.UserEndpointGroupTag)
            .WithDescription("Updates a user's details such as first name and last name. This endpoint is intended for use by administrators.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}

public record UpdateUserRequest(string FirstName, string LastName);
