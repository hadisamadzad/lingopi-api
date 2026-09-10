namespace Lingopi.Identity.Api.Endpoints;

public sealed class DevRedisEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGet("api/dev/redis", async (
            ICacheService cache) =>
            {
                _ = await cache.SetAsync("test", "test", TimeSpan.FromMinutes(1));
                _ = await cache.GetAsync<string>("test");

                return Results.Ok("Redis works as expected!");
            })
            .WithTags("Dev");
    }
}
