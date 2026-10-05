using Lingopi.Identity.Application.Operations.Users;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Identity.Api.Endpoints.Users;

public sealed class UpdateUserTimezoneEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapPatch("api/users/me/timezone", async (
                IOperationMediator operations,
                [FromHeader(Name = "User-Id")] string userId,
                [FromBody] UpdateUserTimezoneRequest request) =>
            {
                var result = await operations.ExecuteAsync(
                    new UpdateUserTimezoneCommand(
                        UserId: userId,
                        TimeZoneId: request.TimeZoneId));

                return result.Status switch
                {
                    OperationStatus.Completed => Results.NoContent(),
                    OperationStatus.Invalid => Results.BadRequest(result.Error),
                    OperationStatus.NotFound => Results.UnprocessableEntity(result.Error),
                    _ => Results.InternalServerError(result.Error)
                };
            })
            .WithTags("Users")
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
