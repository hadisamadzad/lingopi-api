using Lingopi.Identity.Api.Models;
using Lingopi.Identity.Application.Operations.Users;
using Lingopi.Identity.Application.Types.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Identity.Api.Endpoints.Users;

public sealed class UpdateAdminUserPlanEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapPatch("api/admin/users/{userId}/plan", async (
                IOperationMediator operations,
                [FromHeader(Name = "User-Id")] string adminUserId,
                [FromRoute] string userId,
                [FromBody] UpdateAdminUserPlanRequest request) =>
            {
                var result = await operations.ExecuteAsync(
                    new UpdateAdminUserPlanCommand(
                        AdminUserId: adminUserId,
                        UserId: userId,
                        Plan: request.Plan,
                        ExpiresOn: request.ExpiresOn));
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
                    OperationStatus.Unauthorized => Results.Forbid(),
                    OperationStatus.NotFound => Results.UnprocessableEntity(result.Error),
                    _ => Results.InternalServerError(result.Error)
                };
            })
            .WithTags("Admin")
            .WithSummary("Update a user's subscription plan as an administrator")
            .WithDescription(
                "Activates an admin-assigned plan immediately with an optional expiration date.")
            .Produces<SubscriptionResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}

public sealed record UpdateAdminUserPlanRequest(
    SubscriptionPlan Plan,
    DateOnly? ExpiresOn);
