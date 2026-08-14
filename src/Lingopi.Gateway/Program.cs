using System.Text.Json;
using System.Text.Json.Serialization;
using Lingopi.Core.Helpers;
using Lingopi.Gateway.Core;
using Lingopi.Gateway.Core.DependencyInjection;
using Lingopi.Gateway.Core.Middleware;
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
if (app is null) return;

// Add middleware

if (builder.Environment.IsProduction())
    app.UseHsts();

app.UseCors(Constants.CorsPolicyName);
app.UseHealthChecks("/api/health");

app.UseConfiguredOcelot();

try
{ await app.RunAsync(); }
catch (Exception ex) { Log.Fatal(ex, "Application failed to start."); }
