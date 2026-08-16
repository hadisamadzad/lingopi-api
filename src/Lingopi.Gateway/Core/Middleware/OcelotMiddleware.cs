using Ocelot.Middleware;

namespace Lingopi.Gateway.Core.Middleware;

public static class OcelotMiddleware
{
    public static IApplicationBuilder UseConfiguredOcelot(this IApplicationBuilder app)
    {
        app.UseWhen(
            context =>
                !context.Request.Path.StartsWithSegments(
                    "/api/auth/google",
                    StringComparison.OrdinalIgnoreCase)
                && !context.Request.Path.StartsWithSegments(
                    "/signin-google",
                    StringComparison.OrdinalIgnoreCase),
            branch => branch.UseOcelot().Wait());

        return app;
    }
}
