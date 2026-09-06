using Lingopi.Identity.Application.Operations.Users;
using Lingopi.Identity.Application.Types.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Identity.Api.Endpoints.Users;

public class GetUserByIdEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGroup(Routes.UserBaseRoute)
            .WithSummary("Get User by ID")
            .MapGet("{userId}", async (IOperationMediator operations,
                [FromHeader(Name = "User-Id")] string authenticatedUserId,
                [FromRoute] string userId) =>
            {
                // Operation
                var operationResult = await operations.ExecuteAsync(
                    new GetUserByIdCommand(userId));

                if (operationResult.Status == OperationStatus.Completed)
                {
                    var user = operationResult.Value;
                    if (user is null)
                    {
                        return Results.InternalServerError(operationResult.Error);
                    }

                    return Results.Ok(
                        new GetUserByIdResponse(
                            UserId: user.UserId,
                            Email: user.Email,
                            Mobile: user.Mobile,
                            Role: user.Role,
                            FirstName: user.FirstName,
                            LastName: user.LastName,
                            TimeZoneId: user.TimeZoneId,
                            Theme: user.Theme,
                            FullName: user.FullName,
                            CreatedAt: user.CreatedAt,
                            UpdatedAt: user.UpdatedAt
                        ));
                }

                // Result
                return operationResult.Status switch
                {
                    OperationStatus.NotFound => Results.UnprocessableEntity(operationResult.Error),
                    _ => Results.InternalServerError(operationResult.Error),
                };
            })
            .WithTags(Routes.UserEndpointGroupTag)
            .WithDescription("Retrieves a user's details by their unique identifier.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}

public record GetUserByIdResponse(
    string UserId,
    string Email,
    string? Mobile,
    Role Role,
    string? FirstName,
    string? LastName,
    string? TimeZoneId,
    ThemePreference? Theme,
    string FullName,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
