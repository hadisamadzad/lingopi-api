using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Identity.Api.Endpoints;

public sealed class DevLoggerTestEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGroup(Routes.DevBaseRoute)
            .MapGet("logger/test", (
            ILogger<object> logger,
            [FromQuery] string message) =>
            {
                logger.LogInformation("Hey, we have got a log: {message}", message);
                return Results.Ok(true);
            })
            .WithTags(Routes.DevEndpointGroupTag);
    }
}
