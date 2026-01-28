using System.Text.Json.Serialization;
using Lingopi.Core.Extensions;
using Lingopi.Core.Helpers;
using Lingopi.Core.Persistence.MongoDB;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Operations;
using Lingopi.Lingo.Core.Bootstrap;
using Lingopi.Lingo.Infrastructure.Database;
using Minimals.Operations;
using Serilog;

var env = BootstrapHelper.GetEnvironmentName("Local");
var configs = BootstrapHelper.GetConfigFromAppSettingsJson(env);

// Logger
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configs)
    .Enrich.WithMachineName()
    .CreateLogger();

// Create app builder
var builder = WebApplication.CreateBuilder();

// Use Serilog as logging provider
builder.Logging.ClearProviders();
builder.Host.UseSerilog(Log.Logger);

// Add configs
builder.Configuration.AddConfiguration(configs);

// Configure JSON options to serialize enums as strings
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Add services to the container
builder.Services.AddCustomConfigurations(configs);
builder.Services.AddOperations();
builder.Services.AddTransient<IOperationService, OperationService>();

// Database
builder.Services.AddConfiguredMongoDB(configs);
builder.Services.AddScoped<IRepositoryManager, RepositoryManager>();

builder.Services.AddHealthChecks();
builder.Services.AddConfiguredOpenApi();

WebApplication app = default!;
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
app.MapHealthChecks("/api/health");

// Add endpoints
app.MapEndpoints();

if (!app.Environment.IsProduction())
{
    app.UseConfiguredSwagger();
}

try
{ await app.RunAsync(); }
catch (Exception ex) { Log.Fatal(ex, "Application failed to start."); }
