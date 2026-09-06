using Lingopi.Lingo.Api.Models;
using Lingopi.Lingo.Application.Operations.UserUsage;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Lingo.Api.Endpoints.Usage;

public sealed class GetUserUsageSummaryEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGroup(Routes.LingoBaseRoute)
            .WithSummary("Get the current user's usage summary")
            .MapGet("usage", async (
                [FromServices] IOperationMediator operations,
                [FromHeader(Name = "User-Id")] string userId) =>
            {
                var operationResult = await operations.ExecuteAsync(
                    new GetUserUsageSummaryCommand(userId));

                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.Ok(operationResult.Value!.ToResponse()),
                    OperationStatus.Invalid => Results.BadRequest(operationResult.Error),
                    _ => Results.Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: operationResult.Error?.Messages?.FirstOrDefault() ??
                            "An unexpected error occurred while retrieving the user's usage summary.")
                };
            })
            .WithTags(Routes.LingoEndpointGroupTag)
            .WithName("GetUserUsageSummary")
            .WithDescription(
                "Returns account information and total plus previous-calendar-month usage metrics for the user.")
            .Produces<UserUsageSummaryResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
