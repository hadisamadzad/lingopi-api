using Lingopi.Lingo.Api.Models;
using Lingopi.Lingo.Application.Operations.Lingos;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Lingo.Api.Endpoints;

public class GetLingosByUserIdEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGroup(Routes.LingoBaseRoute)
            .WithSummary("Get the current user's lingos")
            .MapGet("user", async (
                [FromServices] IOperationMediator operations,
                [FromHeader(Name = "User-Id")] string userId) =>
            {
                var operationResult = await operations.ExecuteAsync(
                    new GetLingosByUserIdCommand(userId));

                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.Ok(operationResult.Value!
                        .Select(lingo => lingo.ToResponse()).ToList()),
                    OperationStatus.NotFound => Results.NotFound(operationResult.Error),
                    _ => Results.Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: operationResult.Error?.Messages?.FirstOrDefault() ??
                            "An unexpected error occurred while retrieving the user's lingos."),
                };
            })
            .WithTags(Routes.LingoEndpointGroupTag)
            .WithName("GetLingosByUserId")
            .WithDescription("Get all lingos for the authenticated user")
            .Produces<List<LingoResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
