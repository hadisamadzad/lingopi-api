using Lingopi.Identity.Application.Operations.Users;
using Lingopi.Identity.Application.Types.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Identity.Api.Endpoints.Users;

public sealed class UpdateAdminUserStatusEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapPatch("api/admin/users/{userId}/status", async (
                IOperationMediator operations,
                [FromHeader(Name = "User-Id")] string adminUserId,
                [FromRoute] string userId,
                [FromBody] UpdateAdminUserStatusRequest request) =>
            {
                var result = await operations.ExecuteAsync(
                    new UpdateAdminUserStatusCommand(
                        AdminUserId: adminUserId,
                        UserId: userId,
                        Status: request.Status));

                return result.Status switch
                {
                    OperationStatus.Completed => Results.NoContent(),
                    OperationStatus.Invalid => Results.BadRequest(result.Error),
                    OperationStatus.Unauthorized => Results.Forbid(),
                    OperationStatus.NotFound => Results.UnprocessableEntity(result.Error),
                    _ => Results.InternalServerError(result.Error)
                };
            })
            .WithTags("Admin")
            .WithSummary("Update a user's status")
            .WithDescription("Changes a user's account status. This endpoint is intended for administrators.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}

public sealed record UpdateAdminUserStatusRequest(UserState Status);
