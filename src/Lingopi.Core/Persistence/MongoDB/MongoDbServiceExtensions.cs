using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lingopi.Core.Persistence.MongoDB;

public static class MongoDbServiceExtensions
{
    public static IServiceCollection AddConfiguredMongoDB(this IServiceCollection services,
        IConfiguration configuration)
    {
        var config = configuration.GetSection(MongoDBConfig.Key).Get<MongoDBConfig>()
            ?? throw new InvalidOperationException("MongoDB configuration is missing.");

        services.AddSingleton(MongoDBContext.Connect(config));

        return services;
    }
}
