using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HealthAggregatorApi.Core.Interfaces;
using HealthAggregatorApi.Core.Services;
using HealthAggregatorApi.Infrastructure.ExternalApis;
using HealthAggregatorApi.Infrastructure.Persistence;
using HealthAggregatorApi.Core.Models.Oura;
using HealthAggregatorApi.Core.Models.Picooc;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Add Application Insights telemetry
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Register HttpClient for API calls
builder.Services.AddHttpClient();

// Get configuration
var configuration = builder.Configuration;

// Register Blob Storage repositories
var blobConnectionString = configuration["AzureWebJobsStorage"] ?? "UseDevelopmentStorage=true";

builder.Services.AddSingleton<IDataRepository<OuraData>>(sp =>
    new BlobDataRepository<OuraData>(blobConnectionString, "health-data", "oura_data.json"));

builder.Services.AddSingleton<IDataRepository<PicoocDataCache>>(sp =>
    new BlobDataRepository<PicoocDataCache>(blobConnectionString, "health-data", "picooc_data.json"));

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

var app = builder.Build();

app.Run();
