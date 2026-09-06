using Lingopi.Lingo.Api.Models;
using Lingopi.Lingo.Application.Operations.UserSettings;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Lingo.Api.Endpoints;

public sealed class GetUserSettingsEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGroup($"{Routes.LingoBaseRoute}settings")
            .WithTags(Routes.LingoEndpointGroupTag)
            .MapGet("", async (
                [FromServices] IOperationMediator operations,
                [FromHeader(Name = "User-Id")] string userId) =>
            {
                var operationResult = await operations.ExecuteAsync(
                    new GetUserSettingsCommand(userId));

                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.Ok(
                        operationResult.Value!.ToResponse()),
                    OperationStatus.Invalid => Results.BadRequest(
                        operationResult.Error?.Messages),
                    OperationStatus.NotFound => Results.NotFound(
                        operationResult.Error?.Messages),
                    _ => Results.Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: operationResult.Error?.Messages?.FirstOrDefault() ??
                            "An unexpected error occurred while retrieving user settings.")
                };
            })
            .WithSummary("Get user settings")
            .WithName("GetUserSettings")
            .WithDescription("Get the default locales and configured locale pairs for a user")
            .Produces<UserSettingsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
