using Lingopi.Lingo.Api.Authorization;
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
                [FromBody] CreateLanguageRequest request) =>
            {
                var isOwnerOrAdmin = AdminRoleAuthorization.IsOwnerOrAdmin(role);
                if (!isOwnerOrAdmin)
                {
                    return Results.Forbid();
                }

                var operationResult = await operations.ExecuteAsync(
                    new CreateLanguageCommand(
                        LanguageCode: request.LanguageCode,
                        RegionCode: request.RegionCode,
                        Name: request.Name,
                        NativeName: request.NativeName,
                        IsRightToLeft: request.IsRightToLeft,
                        IsActive: request.IsActive));

                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.Created(
                        $"/api/admin/lingos/languages/{operationResult.Value!.Id}",
                        new CreateLanguageResponse(Id: operationResult.Value!.Id)),
                    OperationStatus.Invalid => Results.BadRequest(operationResult.Error?.Messages),
                    _ => Results.UnprocessableEntity(operationResult.Error?.Messages)
                };
            })
            .WithTags("Lingos")
            .WithName("CreateLanguage")
            .WithSummary("Create a locale")
            .Produces<CreateLanguageResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status422UnprocessableEntity);
    }
}

public record CreateLanguageRequest(
    string LanguageCode,
    string RegionCode,
    string Name,
    string NativeName,
    bool IsRightToLeft,
    bool IsActive);

public record CreateLanguageResponse(string Id);
