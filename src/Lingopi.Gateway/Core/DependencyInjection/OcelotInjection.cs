using Lingopi.Gateway.Core.DelegatingHandlers;
using Ocelot.DependencyInjection;

namespace Lingopi.Gateway.Core.DependencyInjection;

public static class OcelotInjection
{
    public static IServiceCollection AddConfiguredOcelot(this IServiceCollection services)
    {
        services
            .AddOcelot()
            .AddDelegatingHandler<GlobalDelegatingHandler>(global: true);

        return services;
    }
}