using Lingopi.Lingo.Api.Models;
using Lingopi.Lingo.Application.Operations.Languages;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Lingo.Api.Endpoints.Languages;

public sealed class GetActiveLanguagesEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGet($"{Routes.LingoBaseRoute}languages", async (
                [FromServices] IOperationMediator operations) =>
            {
                var operationResult = await operations.ExecuteAsync(
                    new GetActiveLanguagesCommand());

                return operationResult.Status == OperationStatus.Completed
                    ? Results.Ok(operationResult.Value!.Select(language => language.ToResponse()))
                    : Results.Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: operationResult.Error?.Messages?.FirstOrDefault() ??
                            "An unexpected error occurred while retrieving languages.");
            })
            .WithTags(Routes.LingoEndpointGroupTag)
            .WithName("GetActiveLanguages")
            .WithSummary("Get active languages and locales")
            .AllowAnonymous()
            .Produces<IEnumerable<LanguageResponse>>(StatusCodes.Status200OK);
    }
}
