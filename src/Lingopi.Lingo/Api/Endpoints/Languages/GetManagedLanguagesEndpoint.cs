using Lingopi.Lingo.Api.Authorization;
using Lingopi.Lingo.Application.Operations.Languages;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Lingo.Api.Endpoints.Languages;

public sealed class GetManagedLanguagesEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGet("api/admin/lingos/languages", async (
                [FromServices] IOperationMediator operations,
                [FromHeader(Name = "User-Role")] string role) =>
            {
                var isOwnerOrAdmin = AdminRoleAuthorization.IsOwnerOrAdmin(role);
                if (!isOwnerOrAdmin)
                {
                    return Results.Forbid();
                }

                var operationResult = await operations.ExecuteAsync(
                    new GetManagedLanguagesCommand());

                return operationResult.Status == OperationStatus.Completed
                    ? Results.Ok(operationResult.Value!.ConvertAll(language =>
                        new LanguageResponse(
                            Id: language.Id,
                            LocaleCode: language.LocaleCode,
                            LanguageCode: language.LanguageCode,
                            RegionCode: language.RegionCode,
                            Name: language.Name,
                            NativeName: language.NativeName,
                            IsRightToLeft: language.IsRightToLeft,
                            IsActive: language.IsActive,
                            CreatedAt: language.CreatedAt,
                            UpdatedAt: language.UpdatedAt,
                            LastActivatedAt: language.LastActivatedAt)))
                    : Results.Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: operationResult.Error?.Messages?.FirstOrDefault() ??
                            "An unexpected error occurred while retrieving languages.");
            })
            .WithTags("Lingos")
            .WithName("GetManagedLanguages")
            .WithSummary("Get all locales for management")
            .Produces<IEnumerable<LanguageResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden);
    }
}

public record LanguageResponse(
    string Id,
    string LocaleCode,
    string LanguageCode,
    string RegionCode,
    string Name,
    string NativeName,
    bool IsRightToLeft,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastActivatedAt);
