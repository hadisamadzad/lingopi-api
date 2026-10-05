using Lingopi.Identity.Application.Operations.Subscriptions;
using Lingopi.Identity.Application.Types.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Identity.Api.Endpoints.Internals;

public sealed class GetEffectiveEntitlementEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGet("api/internal/subscriptions/{userId}/entitlement", async (
                IOperationMediator operations,
                [FromRoute] string userId,
                [FromHeader(Name = "Lingopi-Internal-Auth")] string internalAuthSecret) =>
            {
                var result = await operations.ExecuteAsync(
                    new GetEffectiveEntitlementCommand(
                        InternalAuthSecret: internalAuthSecret,
                        UserId: userId));
                var entitlement = result.Value!;

                return result.Status switch
                {
                    OperationStatus.Completed => Results.Ok(
                        new EffectiveEntitlementResponse(
                            UserId: entitlement.UserId,
                            Plan: entitlement.Plan,
                            SubscriptionStatus: entitlement.SubscriptionStatus,
                            SubscriptionStartedAt: entitlement.SubscriptionStartedAt,
                            SubscriptionExpiresAt: entitlement.SubscriptionExpiresAt)),
                    OperationStatus.Invalid => Results.BadRequest(result.Error),
                    OperationStatus.Unauthorized => Results.Unauthorized(),
                    OperationStatus.NotFound => Results.UnprocessableEntity(result.Error),
                    _ => Results.InternalServerError(result.Error)
                };
            });
    }
}

public sealed record EffectiveEntitlementResponse(
    string UserId,
    SubscriptionPlan Plan,
    SubscriptionStatus? SubscriptionStatus,
    DateTime? SubscriptionStartedAt,
    DateTime? SubscriptionExpiresAt);
