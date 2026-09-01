using Lingopi.Core.Interfaces;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Operations.Users;
using Lingopi.Identity.Application.Types.Entities;
using Microsoft.AspNetCore.Mvc;
using Minimals.Operations;

namespace Lingopi.Identity.Api.Endpoints.Users;

public sealed class UpdateUserThemeEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGroup(Routes.UserBaseRoute)
            .MapPatch("me/theme", async (
                IOperationService operations,
                [FromHeader(Name = "User-Id")] string userId,
                [FromBody] UpdateUserThemeRequest request) =>
            {
                var result = await operations.UpdateUserTheme.ExecuteAsync(
                    new UpdateUserThemeCommand(userId, request.Theme));

                return result.Status switch
                {
                    OperationStatus.Completed => Results.NoContent(),
                    OperationStatus.Invalid => Results.BadRequest(result.Error),
                    OperationStatus.NotFound => Results.NotFound(result.Error),
                    _ => Results.InternalServerError(result.Error)
                };
            })
            .WithTags(Routes.UserEndpointGroupTag)
            .WithSummary("Update the current user's theme")
            .WithDescription("Stores the user's theme preference. Set the value to null to use the client's system theme.")
            .WithName("UpdateUserTheme")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}

public sealed record UpdateUserThemeRequest(ThemePreference? Theme);
