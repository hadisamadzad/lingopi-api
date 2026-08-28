using System.ClientModel;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Interfaces.Services;
using Lingopi.Lingo.Application.Models.Configs;
using Microsoft.Extensions.Options;
using OpenAI;

namespace Lingopi.Lingo.Infrastructure.OpenAI;

public static class OpenAIServiceExtensions
{
    public static IServiceCollection AddConfiguredOpenAI(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<OpenAIConfig>()
            .Bind(configuration.GetSection(OpenAIConfig.Key))
            .Validate(config => Uri.TryCreate(config.BaseAddress, UriKind.Absolute, out _),
                "OpenAI BaseAddress must be an absolute URI.")
            .Validate(config => !string.IsNullOrWhiteSpace(config.ApiKey),
                "OpenAI ApiKey is required.")
            .Validate(config => config.TimeoutSeconds > 0,
                "OpenAI TimeoutSeconds must be greater than zero.")
            .ValidateOnStart();

        services.AddSingleton<IOpenAIModelSettingsProvider, OpenAIModelSettingsProvider>();

        services.AddSingleton<OpenAIClient>(serviceProvider =>
        {
            var config = serviceProvider.GetRequiredService<IOptions<OpenAIConfig>>().Value;

            var clientOptions = new OpenAIClientOptions
            {
                Endpoint = NormalizeEndpoint(config.BaseAddress),
                NetworkTimeout = TimeSpan.FromSeconds(config.TimeoutSeconds)
            };

            return new OpenAIClient(new ApiKeyCredential(config.ApiKey), clientOptions);
        });

        services.AddSingleton<ITranslationService, OpenAITranslationService>();
        services.AddSingleton<ICaptureAnalysisService, OpenAICaptureAnalysisService>();
        services.AddSingleton<IEmbeddingService, OpenAIEmbeddingService>();

        return services;
    }

    private static Uri NormalizeEndpoint(string baseAddress)
    {
        var endpoint = new Uri(baseAddress, UriKind.Absolute);

        if (string.Equals(endpoint.Host, "api.openai.com", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrEmpty(endpoint.AbsolutePath.Trim('/')))
        {
            return new Uri($"{endpoint.Scheme}://{endpoint.Authority}/v1/", UriKind.Absolute);
        }

        return endpoint;
    }
}
