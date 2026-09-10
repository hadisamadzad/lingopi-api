using Lingopi.Lingo.Api.Authorization;
using Lingopi.Lingo.Api.Models;
using Lingopi.Lingo.Application.Operations.Languages;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Lingo.Api.Endpoints.Languages;

public sealed class CreateLanguageEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapPost("api/admin/lingos/languages", async (
                [FromServices] IOperationMediator operations,
                [FromHeader(Name = "User-Role")] string role,
                [FromBody] UpsertLanguageRequest request) =>
            {
                var isOwnerOrAdmin = AdminRoleAuthorization.IsOwnerOrAdmin(role);
                if (!isOwnerOrAdmin)
                {
                    return Results.Forbid();
                }

                var operationResult = await operations.ExecuteAsync(
                    new UpsertLanguageCommand(
                        null,
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
                    OperationStatus.Completed => Results.Created(
                        $"/api/admin/lingos/languages/{operationResult.Value!.Id}",
                        operationResult.Value.ToResponse()),
                    OperationStatus.Invalid => Results.BadRequest(operationResult.Error?.Messages),
                    _ => Results.UnprocessableEntity(operationResult.Error?.Messages)
                };
            })
            .WithTags("Lingos")
            .WithName("CreateLanguage")
            .WithSummary("Create a language with locales")
            .Produces<LanguageResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status422UnprocessableEntity);
    }
}
