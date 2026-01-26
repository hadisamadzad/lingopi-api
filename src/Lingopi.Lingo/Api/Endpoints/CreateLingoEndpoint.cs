using Lingopi.Core.Interfaces;
using Lingopi.Core.Utilities.OperationResult;
using Lingopi.Lingo.Api.Models;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Operations.Lingos;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Lingo.Api.Endpoints;

public class CreateLingoEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGroup(Routes.LingoBaseRoute)
            .WithSummary("Create a new lingo")
            .MapPost("", async (
                [FromServices] IOperationService operations,
                [FromBody] CreateLingoRequest request) =>
            {
                var operationResult = await operations.CreateLingo.ExecuteAsync(
                    new CreateLingoCommand(
                        UserId: request.UserId,
                        Lingo: request.Lingo,
                        LingoType: request.LingoType,
                        Definition: request.Definition,
                        Translation: request.Translation,
                        SourceLanguage: request.SourceLanguage,
                        TargetLanguage: request.TargetLanguage)
                    {
                        Style = request.Style,
                        Examples = request.Examples,
                        Context = request.Context,
                        Tags = request.Tags,
                        LearningGoal = request.LearningGoal,
                        UserNote = request.UserNote,
                        SourceMethod = request.SourceMethod,
                        SourceModel = request.SourceAIModel,
                        SourceVersion = request.SourceAIModelVersion
                    });

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
            .WithDescription("Create a new lingo (word, phrase, or expression) for language learning")
            .Produces<CreateLingoResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
