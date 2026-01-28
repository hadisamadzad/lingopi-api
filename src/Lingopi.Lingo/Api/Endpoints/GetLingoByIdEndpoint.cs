using Lingopi.Core.Interfaces;
using Lingopi.Lingo.Api.Models;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Operations.Lingos;
using Microsoft.AspNetCore.Mvc;
using Minimals.Operations;

namespace Lingopi.Lingo.Api.Endpoints;

public class GetLingoByIdEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGroup(Routes.LingoBaseRoute)
            .WithSummary("Get a lingo by ID")
            .MapGet("{lingoId}", async (
                [FromServices] IOperationService operations,
                [FromRoute] string lingoId) =>
            {
                var operationResult = await operations.GetLingoById.ExecuteAsync(
                    new GetLingoByIdCommand(lingoId));

                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.Ok(new LingoResponse(
                        LingoId: operationResult.Value!.Id,
                        UserId: operationResult.Value!.UserId,
                        Lingo: operationResult.Value!.Lingo,
                        LingoType: operationResult.Value!.LingoType,
                        Definition: operationResult.Value!.Definition,
                        Translation: operationResult.Value!.Translation,
                        SourceLanguage: operationResult.Value!.SourceLanguage,
                        TargetLanguage: operationResult.Value!.TargetLanguage,
                        Style: operationResult.Value!.Style,
                        Examples: operationResult.Value!.Examples,
                        Context: operationResult.Value!.Context,
                        Tags: operationResult.Value!.Tags,
                        LearningGoal: operationResult.Value!.LearningGoal,
                        UserNote: operationResult.Value!.UserNote,
                        SourceMethod: operationResult.Value!.SourceMethod,
                        SourceModel: operationResult.Value!.SourceModel,
                        SourceVersion: operationResult.Value!.SourceVersion,
                        ReviewLastTime: operationResult.Value!.ReviewLastTime,
                        ReviewNextTime: operationResult.Value!.ReviewNextTime,
                        ReviewRepetitions: operationResult.Value!.ReviewRepetitions,
                        ReviewSrsLevel: operationResult.Value!.ReviewSrsLevel,
                        CreatedAt: operationResult.Value!.CreatedAt,
                        UpdatedAt: operationResult.Value!.UpdatedAt
                    )),
                    OperationStatus.Invalid => Results.BadRequest(operationResult.Error?.Messages),
                    OperationStatus.NotFound => Results.NotFound(operationResult.Error?.Messages),
                    _ => Results.Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "An error occurred while processing your request")
                };
            })
            .WithTags(Routes.LingoEndpointGroupTag)
            .WithName("GetLingoById")
            .WithDescription("Get a specific lingo item by its unique ID")
            .Produces<LingoResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
