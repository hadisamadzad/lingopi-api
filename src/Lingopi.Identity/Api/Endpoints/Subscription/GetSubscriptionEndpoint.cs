using Lingopi.Core.Interfaces;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Operations.Subscriptions;
using Lingopi.Identity.Application.Types.Models.Subscriptions;
using Microsoft.AspNetCore.Mvc;
using Minimals.Operations;

namespace Lingopi.Identity.Api.Endpoints.Subscription;

public sealed class GetSubscriptionEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        var group = app.MapGroup(Routes.SubscriptionBaseRoute);

        group.MapGet("", async (
                IOperationService operations,
                [FromHeader(Name = "User-Id")] string userId) =>
            {
                var result = await operations.GetSubscription.ExecuteAsync(
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

        group.MapGet("history", async (
                IOperationService operations,
                [FromHeader(Name = "User-Id")] string userId) =>
            {
                var result = await operations.GetSubscriptionHistory.ExecuteAsync(
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
