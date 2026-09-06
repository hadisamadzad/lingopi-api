using Lingopi.Identity.Application.Operations.Subscriptions;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Identity.Api.Endpoints.Internals;

public sealed class GetEffectiveEntitlementEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGroup("api/internal/subscriptions/")
            .MapGet("{userId}/entitlement", async (
                IOperationMediator operations,
                [FromRoute] string userId,
                [FromHeader(Name = "Lingopi-Internal-Auth")] string internalAuthSecret) =>
            {
                var result = await operations.ExecuteAsync(
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
    }
}
