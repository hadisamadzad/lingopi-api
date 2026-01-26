using Lingopi.Core.Persistence.Redis;

namespace Lingopi.Lingo.Core.Bootstrap;

public static class RedisServiceExtensions
{
    public static IServiceCollection AddConfiguredRedisCache(this IServiceCollection services,
        IConfiguration configuration)
    {
        var config = configuration.GetSection(RedisConfig.Key).Get<RedisConfig>();

        // Distributed caching
        services.AddRedisCache("lingo", config!);

        return services;
    }
}