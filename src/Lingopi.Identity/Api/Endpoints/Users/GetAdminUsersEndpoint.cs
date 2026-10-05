using Lingopi.Core.Utilities.Pagination;
using Lingopi.Identity.Application.Operations.Users;
using Lingopi.Identity.Application.Types.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Identity.Api.Endpoints.Users;

public sealed class GetAdminUsersEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGet("api/admin/users", async (
                IOperationMediator operations,
                [FromHeader(Name = "User-Id")] string adminUserId,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20) =>
            {
                var result = await operations.ExecuteAsync(
                    new GetAdminUsersCommand(
                        AdminUserId: adminUserId,
                        Page: page,
                        PageSize: pageSize));
                var users = result.Value!;

                return result.Status switch
                {
                    OperationStatus.Completed => Results.Ok(
                        new PaginatedList<AdminUserResponse>
                        {
                            Page = users.Page,
                            PageSize = users.PageSize,
                            TotalCount = users.TotalCount,
                            Results = users.Results.Select(user => new AdminUserResponse(
                                UserId: user.UserId,
                                Email: user.Email,
                                FirstName: user.FirstName,
                                LastName: user.LastName,
                                FullName: user.FullName,
                                Role: user.Role,
                                Status: user.Status,
                                LastLoginDate: user.LastLoginDate,
                                CreatedAt: user.CreatedAt,
                                Plan: user.Plan,
                                PlanSource: user.PlanSource,
                                PlanStatus: user.PlanStatus,
                                PlanExpiresAt: user.PlanExpiresAt)).ToList()
                        }),
                    OperationStatus.Invalid => Results.BadRequest(result.Error),
                    OperationStatus.Unauthorized => Results.Forbid(),
                    _ => Results.InternalServerError(result.Error)
                };
            })
            .WithTags("Admin")
            .WithSummary("List users for administration")
            .WithDescription("Returns a paginated list of users for administrators.")
            .Produces<PaginatedList<AdminUserResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}

public sealed record AdminUserResponse(
    string UserId,
    string Email,
    string? FirstName,
    string? LastName,
    string FullName,
    Role Role,
    UserState Status,
    DateTime? LastLoginDate,
    DateTime CreatedAt,
    SubscriptionPlan Plan,
    SubscriptionSource PlanSource,
    SubscriptionStatus? PlanStatus,
    DateTime? PlanExpiresAt);
