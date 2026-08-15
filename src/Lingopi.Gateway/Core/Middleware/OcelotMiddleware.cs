using Ocelot.Middleware;

namespace Lingopi.Gateway.Core.Middleware;

public static class OcelotMiddleware
{
    //private const string RoleKey = "role";

    public static IApplicationBuilder UseConfiguredOcelot(this IApplicationBuilder app)
    {
        /*
        var configuration = new OcelotPipelineConfiguration
        {
            AuthorizationMiddleware = async (context, next) =>
            {
                var downstreamRoute = context.Items[nameof(DownstreamRoute)] as DownstreamRoute;

                var authorized = false;
                if (!downstreamRoute.RouteClaimsRequirement.ContainsKey(RoleKey))
                {
                    authorized = true;
                }
                else
                {
                    var allowedRoles = downstreamRoute.RouteClaimsRequirement[RoleKey].Split(',').Select(x => x.Trim());
                    authorized = allowedRoles.Any(x => context.User.IsInRole(x));
                }

                if (!authorized)
                    context.Items.SetError(new UnauthorizedError("Failed to authorize"));
                else
                    await next.Invoke();

            }
        };
        */
        app.UseWhen(
            context => !context.Request.Path.StartsWithSegments(
                "/api/auth/google",
                StringComparison.OrdinalIgnoreCase),
            branch => branch.UseOcelot().Wait());

        return app;
    }
}