using Lingopi.Core.Persistence.Redis;

namespace Lingopi.Identity.Core.Bootstrap;

public static class RedisServiceExtensions
{
    public static IServiceCollection AddConfiguredRedisCache(this IServiceCollection services,
        IConfiguration configuration)
    {
        var config = configuration.GetSection(RedisConfig.Key).Get<RedisConfig>();

        services.AddRedisCache("identity", config);

        return services;
    }
}