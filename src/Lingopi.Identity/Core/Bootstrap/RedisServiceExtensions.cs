using Lingopi.Core.Persistence.Redis;

namespace Lingopi.Identity.Core.Bootstrap;

public static class RedisServiceExtensions
{
    public static IServiceCollection AddConfiguredRedisCache(this IServiceCollection services,
        IConfiguration configuration)
    {
        var config = configuration.GetSection(RedisConfig.Key).Get<RedisConfig>()
            ?? throw new InvalidOperationException("Redis configuration is missing or invalid.");

        services.AddRedisCache("identity", config);

        return services;
    }
}
