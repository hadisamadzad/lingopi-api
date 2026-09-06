using Lingopi.Identity.Application.Operations.Subscriptions;
using Lingopi.Identity.Application.Types.Models.Subscriptions;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Identity.Api.Endpoints.Subscription;

public sealed class GetSubscriptionHistoryEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGroup(Routes.SubscriptionBaseRoute)
            .MapGet("history", async (
                IOperationMediator operations,
                [FromHeader(Name = "User-Id")] string userId) =>
            {
                var result = await operations.ExecuteAsync(
                    new GetSubscriptionHistoryCommand(userId));

                return result.Status switch
                {
                    OperationStatus.Completed => Results.Ok(result.Value),
                    OperationStatus.Invalid => Results.BadRequest(result.Error),
                    OperationStatus.NotFound => Results.NotFound(result.Error),
                    _ => Results.InternalServerError(result.Error)
                };
            })
            .WithTags(Routes.SubscriptionEndpointGroupTag)
            .WithSummary("Get the current user's subscription history")
            .WithDescription("Returns immutable subscription state snapshots for the authenticated user.")
            .WithName("GetSubscriptionHistory")
            .Produces<List<SubscriptionHistoryModel>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
