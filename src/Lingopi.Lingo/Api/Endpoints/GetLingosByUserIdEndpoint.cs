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
                    OperationStatus.Completed => Results.Ok(operationResult.Value!
                        .Select(lingo => lingo.ToResponse()).ToList()),
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
