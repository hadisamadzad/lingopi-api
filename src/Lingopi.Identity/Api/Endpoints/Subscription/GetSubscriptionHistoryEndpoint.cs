using Lingopi.Identity.Application.Operations.Subscriptions;
using Lingopi.Identity.Application.Types.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Identity.Api.Endpoints.Subscription;

public sealed class GetSubscriptionHistoryEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGet("api/subscription/history", async (
                IOperationMediator operations,
                [FromHeader(Name = "User-Id")] string userId) =>
            {
                var result = await operations.ExecuteAsync(
                    new GetSubscriptionHistoryCommand(UserId: userId));
                var history = result.Value!;

                return result.Status switch
                {
                    OperationStatus.Completed => Results.Ok(
                        history.ConvertAll(item => new SubscriptionHistoryResponse(
                            Id: item.Id,
                            SubscriptionId: item.SubscriptionId,
                            UserId: item.UserId,
                            EventType: item.EventType,
                            Plan: item.Plan,
                            Source: item.Source,
                            Status: item.Status,
                            StartedAt: item.StartedAt,
                            ExpiresAt: item.ExpiresAt,
                            SubscriptionCreatedAt: item.SubscriptionCreatedAt,
                            SubscriptionUpdatedAt: item.SubscriptionUpdatedAt,
                            RecordedAt: item.RecordedAt))),
                    OperationStatus.Invalid => Results.BadRequest(result.Error),
                    OperationStatus.NotFound => Results.UnprocessableEntity(result.Error),
                    _ => Results.InternalServerError(result.Error)
                };
            })
            .WithTags("Subscription")
            .WithSummary("Get the current user's subscription history")
            .WithDescription("Returns immutable subscription state snapshots for the authenticated user.")
            .WithName("GetSubscriptionHistory")
            .Produces<List<SubscriptionHistoryResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}

public sealed record SubscriptionHistoryResponse(
    string Id,
    string SubscriptionId,
    string UserId,
    SubscriptionHistoryEventType EventType,
    SubscriptionPlan Plan,
    SubscriptionSource Source,
    SubscriptionStatus Status,
    DateTime StartedAt,
    DateTime? ExpiresAt,
    DateTime SubscriptionCreatedAt,
    DateTime SubscriptionUpdatedAt,
    DateTime RecordedAt);
