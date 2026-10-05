using Lingopi.Lingo.Api.Authorization;
using Lingopi.Lingo.Application.Operations.Languages;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Lingo.Api.Endpoints.Languages;

public sealed class UpdateLanguageActivationEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapPatch("api/admin/lingos/languages/{id}/activation", async (
                [FromServices] IOperationMediator operations,
                [FromHeader(Name = "User-Role")] string role,
                string id,
                [FromBody] UpdateLanguageActivationRequest request) =>
            {
                var isOwnerOrAdmin = AdminRoleAuthorization.IsOwnerOrAdmin(role);
                if (!isOwnerOrAdmin)
                {
                    return Results.Forbid();
                }

                var operationResult = await operations.ExecuteAsync(
                    new UpdateLanguageActivationCommand(
                        Id: id,
                        IsActive: request.IsActive));

                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.NoContent(),
                    OperationStatus.NotFound => Results.UnprocessableEntity(operationResult.Error?.Messages),
                    _ => Results.UnprocessableEntity(operationResult.Error?.Messages)
                };
            })
            .WithTags("Lingos")
            .WithName("UpdateLanguageActivation")
            .WithSummary("Activate or deactivate a language")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status422UnprocessableEntity);
    }
}

public record UpdateLanguageActivationRequest(bool IsActive);
