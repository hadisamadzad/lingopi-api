namespace Lingopi.Gateway.Core.DependencyInjection;

public static class CorsInjection
{
    public static IServiceCollection AddConfiguredCors(this IServiceCollection services,
        IConfiguration configs)
    {
        var configuredOrigins = configs["AllowedOrigins"]
            ?? throw new InvalidOperationException("AllowedOrigins is not configured.");
        var origins = configuredOrigins
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        services.AddCors(options => options
            .AddPolicy(Constants.CorsPolicyName, policy => policy
            .WithOrigins(origins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials())
        );

        return services;
    }
}
