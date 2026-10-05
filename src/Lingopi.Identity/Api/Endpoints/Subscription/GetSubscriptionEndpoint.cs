using Lingopi.Identity.Api.Models;
using Lingopi.Identity.Application.Operations.Subscriptions;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Identity.Api.Endpoints.Subscription;

public sealed class GetSubscriptionEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGet("api/subscription/", async (IOperationMediator operations,
                [FromHeader(Name = "User-Id")] string userId) =>
            {
                var result = await operations.ExecuteAsync(
                    new GetSubscriptionCommand(UserId: userId));
                var subscription = result.Value!;

                return result.Status switch
                {
                    OperationStatus.Completed => Results.Ok(
                        new SubscriptionResponse(
                            UserId: subscription.UserId,
                            Plan: subscription.Plan,
                            Source: subscription.Source,
                            Status: subscription.Status,
                            StartedAt: subscription.StartedAt,
                            ExpiresAt: subscription.ExpiresAt,
                            CreatedAt: subscription.CreatedAt,
                            UpdatedAt: subscription.UpdatedAt)),
                    OperationStatus.Invalid => Results.BadRequest(result.Error),
                    OperationStatus.NotFound => Results.UnprocessableEntity(result.Error),
                    _ => Results.InternalServerError(result.Error)
                };
            })
            .WithTags("Subscription")
            .WithSummary("Get the current user's subscription")
            .WithDescription("Returns the current subscription and plan for the authenticated user.")
            .WithName("GetSubscription")
            .Produces<SubscriptionResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
