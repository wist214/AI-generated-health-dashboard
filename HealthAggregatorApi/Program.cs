using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HealthAggregatorApi.Core.Interfaces;
using HealthAggregatorApi.Core.Services;
using HealthAggregatorApi.Infrastructure.ExternalApis;
using HealthAggregatorApi.Infrastructure.Persistence;
using HealthAggregatorApi.Infrastructure.Configuration;
using HealthAggregatorApi.Core.Models.Oura;
using HealthAggregatorApi.Core.Models.Picooc;
using HealthAggregatorApi.Core.Models.Cronometer;
using HealthAggregatorApi.Core.Models.Settings;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Configure JSON serialization to use camelCase
builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.PropertyNameCaseInsensitive = true;
});

// Add Application Insights telemetry
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Register HttpClient for API calls
builder.Services.AddHttpClient();

// Get configuration
var configuration = builder.Configuration;

// ============================================================================
// DATA PERSISTENCE LAYER - Choose between Blob Storage or Cosmos DB
// ============================================================================

var useCosmosDb = configuration.GetValue<bool>("CosmosDb:Enabled", false);

if (useCosmosDb)
{
    // --- COSMOS DB CONFIGURATION (Recommended for Production) ---
    builder.Logging.AddConsole();
    
    // Register Cosmos DB configuration with validation
    builder.Services.AddOptions<CosmosDbOptions>()
        .Bind(configuration.GetSection(CosmosDbOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    // Register Cosmos DB client factory as Singleton (thread-safe, reusable)
    builder.Services.AddSingleton<ICosmosDbClientFactory, CosmosDbClientFactory>();

    // Register Cosmos DB repositories
    builder.Services.AddSingleton<IDataRepository<OuraData>>(sp =>
    {
        var factory = sp.GetRequiredService<ICosmosDbClientFactory>();
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CosmosDbOptions>>();
        var logger = sp.GetRequiredService<ILogger<CosmosDbRepository<OuraData>>>();
        return new CosmosDbRepository<OuraData>(factory, options, options.Value.OuraContainerName, logger);
    });

    builder.Services.AddSingleton<IDataRepository<PicoocDataCache>>(sp =>
    {
        var factory = sp.GetRequiredService<ICosmosDbClientFactory>();
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CosmosDbOptions>>();
        var logger = sp.GetRequiredService<ILogger<CosmosDbRepository<PicoocDataCache>>>();
        return new CosmosDbRepository<PicoocDataCache>(factory, options, options.Value.PicoocContainerName, logger);
    });

    builder.Services.AddSingleton<IDataRepository<CronometerData>>(sp =>
    {
        var factory = sp.GetRequiredService<ICosmosDbClientFactory>();
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CosmosDbOptions>>();
        var logger = sp.GetRequiredService<ILogger<CosmosDbRepository<CronometerData>>>();
        return new CosmosDbRepository<CronometerData>(factory, options, options.Value.CronometerContainerName, logger);
    });

    builder.Services.AddSingleton<IDataRepository<UserSettings>>(sp =>
    {
        var factory = sp.GetRequiredService<ICosmosDbClientFactory>();
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CosmosDbOptions>>();
        var logger = sp.GetRequiredService<ILogger<CosmosDbRepository<UserSettings>>>();
        return new CosmosDbRepository<UserSettings>(factory, options, options.Value.UserSettingsContainerName, logger);
    });
}
else
{
    // --- BLOB STORAGE CONFIGURATION (Fallback/Legacy) ---
    var blobConnectionString = configuration["AzureWebJobsStorage"] ?? "UseDevelopmentStorage=true";

    builder.Services.AddSingleton<IDataRepository<OuraData>>(sp =>
        new BlobDataRepository<OuraData>(blobConnectionString, "health-data", "oura_data.json"));

    builder.Services.AddSingleton<IDataRepository<PicoocDataCache>>(sp =>
        new BlobDataRepository<PicoocDataCache>(blobConnectionString, "health-data", "picooc_data.json"));

    builder.Services.AddSingleton<IDataRepository<CronometerData>>(sp =>
        new BlobDataRepository<CronometerData>(blobConnectionString, "health-data", "cronometer_data.json"));

    builder.Services.AddSingleton<IDataRepository<UserSettings>>(sp =>
        new BlobDataRepository<UserSettings>(blobConnectionString, "health-data", "user_settings.json"));
}

// ============================================================================
// EXTERNAL API CLIENTS
// ============================================================================

// ============================================================================
// EXTERNAL API CLIENTS
// ============================================================================

// Register Oura services
var ouraToken = configuration["Oura:AccessToken"] ?? string.Empty;
builder.Services.AddSingleton<IOuraApiClient>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("Oura");
    return new OuraApiClient(httpClient, ouraToken);
});

builder.Services.AddSingleton<IOuraDataService, OuraDataService>();

// Register Picooc services
var picoocEmail = configuration["Picooc:Email"] ?? string.Empty;
var picoocPassword = configuration["Picooc:Password"] ?? string.Empty;
builder.Services.AddSingleton<IPicoocSyncClient>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("Picooc");
    var logger = sp.GetRequiredService<ILogger<PicoocApiClient>>();
    return new PicoocApiClient(httpClient, picoocEmail, picoocPassword, logger);
});

builder.Services.AddSingleton<IPicoocDataService, PicoocDataService>();

// Register Cronometer services
var cronometerEmail = configuration["Cronometer:Email"] ?? string.Empty;
var cronometerPassword = configuration["Cronometer:Password"] ?? string.Empty;

builder.Services.AddSingleton<ICronometerApiClient>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<CronometerApiClient>>();
    return new CronometerApiClient(logger);
});

builder.Services.AddSingleton<ICronometerDataService>(sp =>
{
    var apiClient = sp.GetRequiredService<ICronometerApiClient>();
    var repository = sp.GetRequiredService<IDataRepository<CronometerData>>();
    var logger = sp.GetRequiredService<ILogger<CronometerDataService>>();
    return new CronometerDataService(apiClient, repository, cronometerEmail, cronometerPassword, logger);
});

// ============================================================================
// APPLICATION STARTUP
// ============================================================================

var app = builder.Build();

// Ensure Cosmos DB database and containers exist (if using Cosmos DB)
// This runs in the background to not block startup
if (useCosmosDb)
{
    _ = Task.Run(async () =>
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        try
        {
            var factory = scope.ServiceProvider.GetRequiredService<ICosmosDbClientFactory>();
            var options = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CosmosDbOptions>>();
            
            logger.LogInformation("Initializing Cosmos DB database and containers...");
            
            var containerNames = new[]
            {
                options.Value.OuraContainerName,
                options.Value.PicoocContainerName,
                options.Value.CronometerContainerName,
                options.Value.UserSettingsContainerName
            };

            await factory.EnsureDatabaseAndContainersExistAsync(containerNames);
            
            logger.LogInformation("Cosmos DB initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize Cosmos DB. Application will continue but data operations may fail. Error: {ErrorMessage}", ex.Message);
        }
    });
}

app.Run();
