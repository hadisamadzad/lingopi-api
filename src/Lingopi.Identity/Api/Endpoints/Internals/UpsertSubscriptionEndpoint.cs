using Lingopi.Identity.Api.Models;
using Lingopi.Identity.Application.Operations.Subscriptions;
using Lingopi.Identity.Application.Types.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Identity.Api.Endpoints.Internals;

public sealed class UpsertSubscriptionEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapPut("api/internal/subscriptions/{userId}", async (
                IOperationMediator operations,
                [FromRoute] string userId,
                [FromHeader(Name = "Lingopi-Internal-Auth")] string internalAuthSecret,
                [FromBody] UpsertSubscriptionRequest request) =>
            {
                var result = await operations.ExecuteAsync(
                    new UpsertSubscriptionCommand(
                        InternalAuthSecret: internalAuthSecret,
                        UserId: userId,
                        Plan: request.Plan,
                        Status: request.Status,
                        StartedAt: request.StartedAt,
                        ExpiresAt: request.ExpiresAt));
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
                    OperationStatus.Unauthorized => Results.Unauthorized(),
                    OperationStatus.NotFound => Results.UnprocessableEntity(result.Error),
                    _ => Results.InternalServerError(result.Error)
                };
            })
            .WithTags("Internal")
            .WithSummary("Manage a user's subscription internally")
            .WithDescription("Internal service-to-service subscription management endpoint.")
            .ExcludeFromDescription();
    }
}

public sealed record UpsertSubscriptionRequest(
    SubscriptionPlan Plan,
    SubscriptionStatus Status,
    DateTime StartedAt,
    DateTime? ExpiresAt);
