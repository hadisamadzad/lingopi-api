using Lingopi.Core.Interfaces;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Operations.Auth;
using Lingopi.Identity.Core.Configuration;

namespace Lingopi.Identity.Api.Endpoints.Auth;

public class LogoutEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        // Endpoint for logging out
        app.MapGroup(Routes.AuthBaseRoute)
            .WithSummary("Logout endpoint")
            .MapPost("logout", async (IOperationService operations, HttpContext context) =>
            {
                var refreshToken = context.Request.Cookies["refreshToken"];
                await operations.RevokeRefreshToken.ExecuteAsync(
                    new RevokeRefreshTokenCommand(refreshToken));

                context.Response.Cookies.Delete(
                    "refreshToken",
                    CookieConfiguration.GetRefreshTokenDeletionOptions(
                        isProduction: app.Environment.IsProduction()));

                // Return 204 No Content - logout successful, no response body needed
                return Results.NoContent();
            })
            .WithTags(Routes.AuthEndpointGroupTag)
            .WithDescription("Logs out the user by clearing the refresh token cookie.")
            .Produces(StatusCodes.Status204NoContent);
    }
}
