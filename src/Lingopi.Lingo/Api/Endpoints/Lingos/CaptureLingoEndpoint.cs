using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Operations.Lingos;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Lingo.Api.Endpoints.Lingos;

public class CaptureLingoEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapPost("api/lingos/", async ([FromServices] IOperationMediator operations,
                [FromBody] CaptureLingoRequest request,
                [FromHeader(Name = "User-Id")] string userId) =>
            {
                var operationResult = await operations.ExecuteAsync(
                    new CaptureLingoCommand(
                        UserId: userId,
                        Expression: request.Expression.Trim(),
                        EncounterContext: request.Context,
                        SourceLanguageCode: request.SourceLanguageCode,
                        SourceLocaleCode: request.SourceLocaleCode));

                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.Created(
                        $"/api/captures/{operationResult.Value}",
                        new CaptureLingoResponse(CaptureId: operationResult.Value!)),
                    OperationStatus.Invalid => Results.BadRequest(operationResult.Error),
                    OperationStatus.Failed => Results.UnprocessableEntity(operationResult.Error),
                    _ => Results.Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: operationResult.Error?.Messages?.FirstOrDefault() ??
                            "An unexpected error occurred while capturing the lingo."),
                };
            })
            .WithTags("Lingos")
            .WithSummary("Capture a new lingo")
            .WithName("CaptureLingo")
            .WithDescription("Capture the user's original lingo text and queue it for later enrichment")
            .Produces<CaptureLingoResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}

public record CaptureLingoRequest(
    string Expression,
    string SourceLanguageCode,
    string SourceLocaleCode,
    LingoContext? Context = null);

public record CaptureLingoResponse(string CaptureId);
