using Lingopi.Core.Interfaces;
using Lingopi.Lingo.Api.Models;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Operations.Lingos;
using Microsoft.AspNetCore.Mvc;
using Minimals.Operations;

namespace Lingopi.Lingo.Api.Endpoints;

public class CreateLingoEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGroup(Routes.LingoBaseRoute)
            .WithSummary("Capture a new lingo")
            .MapPost("", async (
                [FromServices] IOperationService operations,
                [FromBody] CreateLingoRequest request) =>
            {
                var operationResult = await operations.CreateLingo.ExecuteAsync(
                    new CreateLingoCommand(
                        UserId: request.UserId,
                        OriginalText: request.OriginalText,
                        SourceLocaleCode: request.SourceLocaleCode));

                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.Created(
                        $"/api/lingos/{operationResult.Value}",
                        new CreateLingoResponse(operationResult.Value!)),
                    OperationStatus.Invalid => Results.BadRequest(operationResult.Error),
                    OperationStatus.Failed => Results.UnprocessableEntity(operationResult.Error),
                    _ => Results.InternalServerError(operationResult.Error),
                };
            })
            .WithTags(Routes.LingoEndpointGroupTag)
            .WithName("CreateLingo")
            .WithDescription("Capture the user's original lingo text and queue it for later enrichment")
            .Produces<CreateLingoResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
