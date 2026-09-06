using Lingopi.Lingo.Api.Models;
using Lingopi.Lingo.Application.Operations.UserSettings;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Lingo.Api.Endpoints;

public sealed class SaveUserSettingsEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGroup($"{Routes.LingoBaseRoute}settings")
            .WithTags(Routes.LingoEndpointGroupTag)
            .MapPut("", async (
                [FromServices] IOperationMediator operations,
                [FromHeader(Name = "User-Id")] string userId,
                [FromBody] SaveUserSettingsRequest request) =>
            {
                var operationResult = await operations.ExecuteAsync(
                    new SaveUserSettingsCommand(
                        userId,
                        request.TargetLocaleCode,
                        request.SourceLocaleCodes));

                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.Ok(
                        operationResult.Value!.ToResponse()),
                    OperationStatus.Invalid => Results.BadRequest(
                        operationResult.Error?.Messages),
                    OperationStatus.Failed => Results.UnprocessableEntity(
                        operationResult.Error?.Messages),
                    _ => Results.Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: operationResult.Error?.Messages?.FirstOrDefault() ??
                            "An unexpected error occurred while saving user settings.")
                };
            })
            .WithSummary("Save user settings")
            .WithName("SaveUserSettings")
            .WithDescription("Save the target locale and source locales for a user")
            .Produces<UserSettingsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
