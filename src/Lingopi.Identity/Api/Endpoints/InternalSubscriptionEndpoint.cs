using Lingopi.Core.Interfaces;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Operations.Subscriptions;
using Lingopi.Identity.Application.Types.Entities;
using Microsoft.AspNetCore.Mvc;
using Minimals.Operations;

namespace Lingopi.Identity.Api.Endpoints;

public sealed class InternalSubscriptionEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        var group = app.MapGroup("api/internal/subscriptions/");
        group
            .MapGet("{userId}/entitlement", async (
                IOperationService operations,
                [FromRoute] string userId,
                [FromHeader(Name = "Lingopi-Internal-Auth")] string internalAuthSecret) =>
            {
                var result = await operations.GetEffectiveEntitlement.ExecuteAsync(
                    new GetEffectiveEntitlementCommand(internalAuthSecret, userId));

                return result.Status switch
                {
                    OperationStatus.Completed => Results.Ok(result.Value),
                    OperationStatus.Invalid => Results.BadRequest(result.Error),
                    OperationStatus.Unauthorized => Results.Unauthorized(),
                    OperationStatus.NotFound => Results.NotFound(result.Error),
                    _ => Results.InternalServerError(result.Error)
                };
            });

        group
            .MapPut("{userId}", async (
                IOperationService operations,
                [FromRoute] string userId,
                [FromHeader(Name = "Lingopi-Internal-Auth")] string internalAuthSecret,
                [FromBody] UpsertSubscriptionRequest request) =>
            {
                var result = await operations.UpsertSubscription.ExecuteAsync(
                    new UpsertSubscriptionCommand(
                        internalAuthSecret,
                        userId,
                        request.Plan,
                        request.Status,
                        request.StartedAt,
                        request.ExpiresAt));

                return result.Status switch
                {
                    OperationStatus.Completed => Results.Ok(result.Value),
                    OperationStatus.Invalid => Results.BadRequest(result.Error),
                    OperationStatus.Unauthorized => Results.Unauthorized(),
                    OperationStatus.NotFound => Results.NotFound(result.Error),
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
