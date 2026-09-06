using Lingopi.Identity.Application.Operations.Subscriptions;
using Lingopi.Identity.Application.Types.Models.Subscriptions;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Identity.Api.Endpoints.Subscription;

public sealed class GetSubscriptionEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGroup(Routes.SubscriptionBaseRoute)
            .MapGet("", async (IOperationMediator operations,
                [FromHeader(Name = "User-Id")] string userId) =>
            {
                var result = await operations.ExecuteAsync(
                    new GetSubscriptionCommand(userId));

                return result.Status switch
                {
                    OperationStatus.Completed => Results.Ok(result.Value),
                    OperationStatus.Invalid => Results.BadRequest(result.Error),
                    OperationStatus.NotFound => Results.NotFound(result.Error),
                    _ => Results.InternalServerError(result.Error)
                };
            })
            .WithTags(Routes.SubscriptionEndpointGroupTag)
            .WithSummary("Get the current user's subscription")
            .WithDescription("Returns the current subscription and plan for the authenticated user.")
            .WithName("GetSubscription")
            .Produces<SubscriptionModel>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
