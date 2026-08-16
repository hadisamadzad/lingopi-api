using Lingopi.Core.Interfaces;

namespace Lingopi.Gateway.Core.Endpoints;

public sealed class VersionEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGet("/api/version", () => Results.Ok(new
        {
            service = "lingopi-gateway",
            version = DeploymentInfo.Version,
            commit = DeploymentInfo.Commit,
            environment = DeploymentInfo.EnvironmentName
        }));
    }
}
