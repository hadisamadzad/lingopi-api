using Lingopi.Lingo.Api.Authorization;
using Lingopi.Lingo.Api.Models;
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
                [FromBody] UpsertLanguageRequest request) =>
            {
                var isOwnerOrAdmin = AdminRoleAuthorization.IsOwnerOrAdmin(role);
                if (!isOwnerOrAdmin)
                {
                    return Results.Forbid();
                }

                var operationResult = await operations.ExecuteAsync(
                    new UpsertLanguageCommand(
                        id,
                        request.Code,
                        request.Name,
                        request.NativeName,
                        request.IsActive,
                        request.Locales.Select(locale => new LocaleRequest(
                            locale.Code,
                            locale.Region,
                            locale.Name,
                            locale.NativeName,
                            locale.IsRightToLeft,
                            locale.IsActive)).ToList()));

                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.Ok(operationResult.Value!.ToResponse()),
                    OperationStatus.Invalid => Results.BadRequest(operationResult.Error?.Messages),
                    OperationStatus.NotFound => Results.NotFound(operationResult.Error?.Messages),
                    _ => Results.UnprocessableEntity(operationResult.Error?.Messages)
                };
            })
            .WithTags("Lingos")
            .WithName("UpdateLanguage")
            .WithSummary("Update a language and its locales")
            .Produces<LanguageResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status422UnprocessableEntity);
    }
}
