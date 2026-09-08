using Lingopi.Lingo.Api.Models;
using Lingopi.Lingo.Api.Authorization;
using Lingopi.Lingo.Application.Operations.Languages;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Lingo.Api.Endpoints.Languages;

public sealed class GetManagedLanguagesEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGet($"{Routes.LingoBaseRoute}admin/languages", async (
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
                    ? Results.Ok(operationResult.Value!.Select(language => language.ToResponse()))
                    : Results.Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: operationResult.Error?.Messages?.FirstOrDefault() ??
                            "An unexpected error occurred while retrieving languages.");
            })
            .WithTags(Routes.LingoEndpointGroupTag)
            .WithName("GetManagedLanguages")
            .WithSummary("Get all languages and locales for management")
            .Produces<IEnumerable<LanguageResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden);
    }
}
