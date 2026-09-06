using Lingopi.Identity.Application.Operations.Users;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Identity.Api.Endpoints.Users;

public sealed class UpdateUserTimezoneEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGroup(Routes.UserBaseRoute)
            .MapPatch("me/timezone", async (
                IOperationMediator operations,
                [FromHeader(Name = "User-Id")] string userId,
                [FromBody] UpdateUserTimezoneRequest request) =>
            {
                var result = await operations.ExecuteAsync(
                    new UpdateUserTimezoneCommand(userId, request.TimeZoneId));

                return result.Status switch
                {
                    OperationStatus.Completed => Results.NoContent(),
                    OperationStatus.Invalid => Results.BadRequest(result.Error),
                    OperationStatus.NotFound => Results.NotFound(result.Error),
                    _ => Results.InternalServerError(result.Error)
                };
            })
            .WithTags(Routes.UserEndpointGroupTag)
            .WithSummary("Update the current user's timezone")
            .WithDescription("Stores a valid IANA timezone identifier for local scheduling and notifications.")
            .WithName("UpdateUserTimezone")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}

public sealed record UpdateUserTimezoneRequest(string? TimeZoneId);
