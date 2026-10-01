using System.Text.Json;
using System.Text.Json.Serialization;
using Lingopi.Core.Extensions;
using Lingopi.Core.Helpers;
using Lingopi.Gateway.Core;
using Lingopi.Gateway.Core.DependencyInjection;
using Lingopi.Gateway.Core.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Serilog;

var env = BootstrapHelper.GetEnvironmentName("Local");
var configs = BootstrapHelper.GetConfigFromAppSettingsJson(env);

// Logger
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configs)
    .Enrich.WithMachineName()
    .CreateLogger();

var builder = WebApplication.CreateBuilder();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Use Serilog as logging provider
builder.Logging.ClearProviders();
builder.Host.UseSerilog(Log.Logger);

builder.Configuration.AddConfiguration(configs);
if (builder.Environment.IsProduction())
{
    builder.Configuration.AddOcelot(Constants.RouteConfigPath, builder.Environment);
}
else
{
    var swaggerRoutesPath = Path.Combine(Constants.RouteConfigPath, "swagger-routes.json");
    var swaggerRoutes = JsonSerializer.Deserialize<FileConfiguration>(
            await File.ReadAllTextAsync(swaggerRoutesPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? throw new InvalidOperationException($"Unable to load Ocelot routes from '{swaggerRoutesPath}'.");

    var mergedRoutes = builder.Configuration.GetMergedOcelotJson(
        Constants.RouteConfigPath,
        builder.Environment,
        swaggerRoutes);
    builder.Configuration.AddOcelotJsonFile(
        mergedRoutes,
        Path.Combine(Constants.RouteConfigPath, "ocelot.json"));
}

// Add services to the container
builder.Services
    .AddControllers()
    .AddJsonOptions(x =>
    {
        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddConfiguredCors(configs);
builder.Services.AddConfiguredAuthentication(configs);
builder.Services.AddHttpClient("identity", client =>
    client.BaseAddress = new Uri(
        configs["IdentityService:BaseUrl"]
        ?? throw new InvalidOperationException("IdentityService:BaseUrl is not configured.")));
builder.Services.AddConfiguredOcelot();

builder.Services.AddConfiguredHealthChecks();

WebApplication app = default;
try
{
    app = builder.Build();
    Log.Information("Application started on: {0} ({1})", configs["Urls"], env);
}
catch (Exception ex)
{
    Log.Fatal(ex, $"Application failed to build.");
}
if (app is null)
{
    return;
}

// Add middleware

app.UseForwardedHeaders();

if (builder.Environment.IsProduction())
{
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["Gateway-Version"] = DeploymentInfo.Version;
    await next(context);
});

app.UseCors(Constants.CorsPolicyName);
app.UseHealthChecks("/api/health");
app.UseAuthentication();
app.Use(async (context, next) =>
{
    var isAdminRoute =
        context.Request.Path.StartsWithSegments("/api/lingo/admin", StringComparison.OrdinalIgnoreCase) ||
        context.Request.Path.StartsWithSegments("/api/identity/admin", StringComparison.OrdinalIgnoreCase);
    if (isAdminRoute)
    {
        var authenticationResult = await context.AuthenticateAsync(Constants.JwtBearerScheme);
        if (!authenticationResult.Succeeded || authenticationResult.Principal is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        context.User = authenticationResult.Principal;
    }

    var isOwnerOrAdmin = context.User.IsInRole("Owner") || context.User.IsInRole("Admin");
    if (isAdminRoute && !isOwnerOrAdmin)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return;
    }

    await next(context);
});
app.MapEndpoints();

app.UseConfiguredOcelot();

try
{ await app.RunAsync(); }
catch (Exception ex) { Log.Fatal(ex, "Application failed to start."); }
