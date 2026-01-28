using Lingopi.Core.Interfaces;
using Lingopi.Lingo.Api.Models;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Operations.Lingos;
using Microsoft.AspNetCore.Mvc;
using Minimals.Operations;

namespace Lingopi.Lingo.Api.Endpoints;

public class GetLingosByUserIdEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGroup(Routes.LingoBaseRoute)
            .WithSummary("Get lingos by user ID")
            .MapGet("user/{userId}", async (
                [FromServices] IOperationService operations,
                [FromRoute] string userId) =>
            {
                var operationResult = await operations.GetLingosByUserId.ExecuteAsync(
                    new GetLingosByUserIdCommand(userId));

                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.Ok(
                        operationResult.Value!.Select(lingo => new LingoResponse(
                            LingoId: lingo.Id,
                            UserId: lingo.UserId,
                            Lingo: lingo.Lingo,
                            LingoType: lingo.LingoType,
                            Definition: lingo.Definition,
                            Translation: lingo.Translation,
                            SourceLanguage: lingo.SourceLanguage,
                            TargetLanguage: lingo.TargetLanguage,
                            Style: lingo.Style,
                            Examples: lingo.Examples,
                            Context: lingo.Context,
                            Tags: lingo.Tags,
                            LearningGoal: lingo.LearningGoal,
                            UserNote: lingo.UserNote,
                            SourceMethod: lingo.SourceMethod,
                            SourceModel: lingo.SourceModel,
                            SourceVersion: lingo.SourceVersion,
                            ReviewLastTime: lingo.ReviewLastTime,
                            ReviewNextTime: lingo.ReviewNextTime,
                            ReviewRepetitions: lingo.ReviewRepetitions,
                            ReviewSrsLevel: lingo.ReviewSrsLevel,
                            CreatedAt: lingo.CreatedAt,
                            UpdatedAt: lingo.UpdatedAt
                        )).ToList()),
                    OperationStatus.NotFound => Results.NotFound(operationResult.Error),
                    _ => Results.InternalServerError(operationResult.Error),
                };
            })
            .WithTags(Routes.LingoEndpointGroupTag)
            .WithName("GetLingosByUserId")
            .WithDescription("Get all lingos for a specific user")
            .Produces<List<LingoResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
