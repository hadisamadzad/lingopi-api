using Lingopi.Lingo.Api.Authorization;
using Lingopi.Lingo.Application.Operations.Languages;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Lingo.Api.Endpoints.Languages;

public sealed class UpdateLanguageEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapPut("api/admin/lingos/languages/{id}", async (
                [FromServices] IOperationMediator operations,
                [FromHeader(Name = "User-Role")] string role,
                string id,
                [FromBody] UpdateLanguageRequest request) =>
            {
                var isOwnerOrAdmin = AdminRoleAuthorization.IsOwnerOrAdmin(role);
                if (!isOwnerOrAdmin)
                {
                    return Results.Forbid();
                }

                var operationResult = await operations.ExecuteAsync(
                    new UpdateLanguageCommand(
                        Id: id,
                        Name: request.Name,
                        NativeName: request.NativeName,
                        IsRightToLeft: request.IsRightToLeft,
                        IsActive: request.IsActive));

                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.NoContent(),
                    OperationStatus.Invalid => Results.BadRequest(operationResult.Error?.Messages),
                    OperationStatus.NotFound => Results.UnprocessableEntity(operationResult.Error?.Messages),
                    _ => Results.UnprocessableEntity(operationResult.Error?.Messages)
                };
            })
            .WithTags("Lingos")
            .WithName("UpdateLanguage")
            .WithSummary("Update a locale")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status422UnprocessableEntity);
    }
}

public record UpdateLanguageRequest(
    string Name,
    string NativeName,
    bool IsRightToLeft,
    bool IsActive);
