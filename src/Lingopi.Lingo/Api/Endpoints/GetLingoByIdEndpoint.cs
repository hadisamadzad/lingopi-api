using Lingopi.Lingo.Api.Models;
using Lingopi.Lingo.Application.Operations.Lingos;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Lingo.Api.Endpoints;

public class GetLingoByIdEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGroup(Routes.LingoBaseRoute)
            .WithSummary("Get a lingo by ID")
            .MapGet("{lingoId}", async (
                [FromServices] IOperationMediator operations,
                [FromHeader(Name = "User-Id")] string userId,
                [FromRoute] string lingoId) =>
            {
                var operationResult = await operations.ExecuteAsync(
                    new GetLingoByIdCommand(userId, lingoId));

                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.Ok(operationResult.Value!.ToResponse()),
                    OperationStatus.Invalid => Results.BadRequest(operationResult.Error?.Messages),
                    OperationStatus.NotFound => Results.NotFound(operationResult.Error?.Messages),
                    _ => Results.Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: operationResult.Error?.Messages?.FirstOrDefault() ??
                            "An unexpected error occurred while retrieving the lingo.")
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
