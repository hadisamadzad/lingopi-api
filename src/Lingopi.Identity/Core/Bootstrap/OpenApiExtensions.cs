namespace Lingopi.Identity.Core.Bootstrap;

public static class OpenApiExtensions
{
    private const string DocumentTitle = "LingopiAPI";

    public static IServiceCollection AddConfiguredOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(DocumentTitle);

        return services;
    }

    public static void UseConfiguredSwagger(this WebApplication app)
    {
        app.MapOpenApi();
        app.UseSwaggerUI(configs =>
        {
            configs.DocumentTitle = "Swagger UI - Lingopi Identity API";
            configs.SwaggerEndpoint($"/openapi/{DocumentTitle}.json", "Lingopi API - Identity");
        });
    }
}