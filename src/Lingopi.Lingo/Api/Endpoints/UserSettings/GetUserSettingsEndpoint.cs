using Lingopi.Lingo.Api.Models;
using Lingopi.Lingo.Application.Operations.UserSettings;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Lingo.Api.Endpoints.UserSettings;

public sealed class GetUserSettingsEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGet("api/lingos/settings", async (
                [FromServices] IOperationMediator operations,
                [FromHeader(Name = "User-Id")] string userId) =>
            {
                var operationResult = await operations.ExecuteAsync(
                    new GetUserSettingsCommand(UserId: userId));
                var settings = operationResult.Value!;

                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.Ok(
                        new UserSettingsResponse(
                            UserId: settings.UserId,
                            TargetLocaleCode: settings.TargetLocaleCode,
                            SourceLocaleCodes: settings.SourceLocaleCodes,
                            CreatedAt: settings.CreatedAt,
                            UpdatedAt: settings.UpdatedAt)),
                    OperationStatus.Invalid => Results.BadRequest(
                        operationResult.Error?.Messages),
                    OperationStatus.NotFound => Results.UnprocessableEntity(
                        operationResult.Error?.Messages),
                    _ => Results.Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: operationResult.Error?.Messages?.FirstOrDefault() ??
                            "An unexpected error occurred while retrieving user settings.")
                };
            })
            .WithTags("Lingos")
            .WithSummary("Get user settings")
            .WithName("GetUserSettings")
            .WithDescription("Get the default locales and configured locale pairs for a user")
            .Produces<UserSettingsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
