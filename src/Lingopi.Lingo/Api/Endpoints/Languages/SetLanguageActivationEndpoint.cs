using Lingopi.Lingo.Api.Models;
using Lingopi.Lingo.Api.Authorization;
using Lingopi.Lingo.Application.Operations.Languages;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Lingo.Api.Endpoints.Languages;

public sealed class SetLanguageActivationEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapPatch($"{Routes.LingoBaseRoute}admin/languages/{{id}}/activation", async (
                [FromServices] IOperationMediator operations,
                [FromHeader(Name = "User-Role")] string role,
                string id,
                [FromBody] SetLanguageActivationRequest request) =>
            {
                var isOwnerOrAdmin = AdminRoleAuthorization.IsOwnerOrAdmin(role);
                if (!isOwnerOrAdmin)
                {
                    return Results.Forbid();
                }

                var operationResult = await operations.ExecuteAsync(
                    new SetLanguageActivationCommand(id, request.IsActive));

                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.Ok(operationResult.Value!.ToResponse()),
                    OperationStatus.NotFound => Results.NotFound(operationResult.Error?.Messages),
                    _ => Results.UnprocessableEntity(operationResult.Error?.Messages)
                };
            })
            .WithTags(Routes.LingoEndpointGroupTag)
            .WithName("SetLanguageActivation")
            .WithSummary("Activate or deactivate a language")
            .Produces<LanguageResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status422UnprocessableEntity);
    }
}
