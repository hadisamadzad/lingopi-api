using Lingopi.Lingo.Api.Models;
using Lingopi.Lingo.Application.Operations.Languages;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Lingo.Api.Endpoints.Languages;

public sealed class GetActiveLanguagesEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGet("api/lingos/languages", async (
                [FromServices] IOperationMediator operations) =>
            {
                var operationResult = await operations.ExecuteAsync(
                    new GetActiveLanguagesCommand());

                return operationResult.Status == OperationStatus.Completed
                    ? Results.Ok(
                        operationResult.Value!.Select(language => language.ToActiveLocaleResponse()))
                    : Results.Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: operationResult.Error?.Messages?.FirstOrDefault() ??
                            "An unexpected error occurred while retrieving languages.");
            })
            .WithTags("Lingos")
            .WithName("GetActiveLanguages")
            .WithSummary("Get active locales")
            .AllowAnonymous()
            .Produces<IEnumerable<ActiveLocaleResponse>>(StatusCodes.Status200OK);
    }
}
