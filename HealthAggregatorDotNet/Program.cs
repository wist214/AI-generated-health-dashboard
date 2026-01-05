using System.Text.Json;
using HealthAggregator.Api.Endpoints;
using HealthAggregator.Core.Interfaces;
using HealthAggregator.Core.Models.Oura;
using HealthAggregator.Core.Services;
using HealthAggregator.Infrastructure.ExternalApis;
using HealthAggregator.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
var configuration = builder.Configuration;
var dataPath = Path.Combine(AppContext.BaseDirectory, "data");
Directory.CreateDirectory(dataPath);

// Configure JSON serialization
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// Register repositories (Singleton for file-based storage)
builder.Services.AddSingleton<IDataRepository<OuraData>>(sp =>
    new FileDataRepository<OuraData>(Path.Combine(dataPath, "oura_data.json")));

builder.Services.AddSingleton<IDataRepository<PicoocDataCache>>(sp =>
    new FileDataRepository<PicoocDataCache>(Path.Combine(dataPath, "picooc_data.json")));

// Register API clients
var ouraToken = configuration["Oura:AccessToken"] ?? Environment.GetEnvironmentVariable("OURA_ACCESS_TOKEN") ?? "";
var picoocEmail = configuration["Picooc:Username"] ?? Environment.GetEnvironmentVariable("PICOOC_EMAIL") ?? "";
var picoocPassword = configuration["Picooc:Password"] ?? Environment.GetEnvironmentVariable("PICOOC_PASSWORD") ?? "";

builder.Services.AddSingleton<IOuraApiClient>(sp => new OuraApiClient(ouraToken));

// Native Picooc API client (no Docker required)
builder.Services.AddHttpClient<IPicoocSyncClient, PicoocApiClient>((sp, client) =>
{
    client.Timeout = TimeSpan.FromMinutes(1);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
});

builder.Services.AddSingleton<IPicoocSyncClient>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();
    var logger = sp.GetRequiredService<ILogger<PicoocApiClient>>();
    return new PicoocApiClient(httpClient, picoocEmail, picoocPassword, logger);
});

// Register services
builder.Services.AddSingleton<IOuraDataService, OuraDataService>();
builder.Services.AddSingleton<IPicoocDataService, PicoocDataService>();
builder.Services.AddSingleton<IDashboardService, DashboardService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Global exception handler
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
});

app.UseCors();
app.UseStaticFiles();

// Map all API endpoints using clean architecture
app.MapHealthAggregatorEndpoints();

// Legacy endpoints for backward compatibility with existing frontend
app.MapGet("/api/data", async (IPicoocDataService service) =>
{
    var data = await service.GetAllMeasurementsAsync();
    return Results.Json(data);
});

app.MapPost("/api/sync", async (IPicoocDataService service) =>
{
    try
    {
        var data = await service.SyncDataAsync();
        return Results.Json(new { success = true, count = data.Count, data });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
});

app.MapGet("/api/latest", async (IPicoocDataService service) =>
{
    var latest = await service.GetLatestMeasurementAsync();
    return latest != null ? Results.Json(latest) : Results.Json<object?>(null);
});

app.MapGet("/api/stats", async (IPicoocDataService service) =>
{
    var stats = await service.GetStatsAsync();
    return Results.Json(new
    {
        totalMeasurements = stats.Count,
        weight = stats.Weight != null ? new
        {
            current = stats.Weight.Latest,
            min = stats.Weight.Min,
            max = stats.Weight.Max,
            avg = stats.Weight.Average
        } : null,
        bodyFat = stats.BodyFat != null ? new
        {
            current = stats.BodyFat.Latest,
            min = stats.BodyFat.Min,
            max = stats.BodyFat.Max,
            avg = stats.BodyFat.Average
        } : null,
        bmi = stats.BMI != null ? new
        {
            current = stats.BMI.Latest,
            min = stats.BMI.Min,
            max = stats.BMI.Max,
            avg = stats.BMI.Average
        } : null,
        muscle = stats.Muscle != null ? new
        {
            current = stats.Muscle.Latest,
            min = stats.Muscle.Min,
            max = stats.Muscle.Max,
            avg = stats.Muscle.Average
        } : null,
        firstMeasurement = stats.FirstDate,
        lastMeasurement = stats.LastDate
    });
});

app.MapGet("/api/status", async (IPicoocDataService service, IPicoocSyncClient syncClient) =>
{
    var available = await service.IsServiceAvailableAsync();
    var lastSync = await service.GetLastSyncTimeAsync();
    var measurements = await service.GetAllMeasurementsAsync();
    var isConfigured = syncClient.IsConfigured();
    
    return Results.Json(new
    {
        configured = isConfigured,
        hasCredentials = !string.IsNullOrEmpty(picoocEmail),
        hasDocker = false, // No longer using Docker
        dataCount = measurements.Count,
        syncMethod = isConfigured ? "native" : "none",
        lastSync
    });
});

// Serve the main page
app.MapFallbackToFile("index.html");

Console.WriteLine("===========================================");
Console.WriteLine("  Health Aggregator (.NET) v2.0");
Console.WriteLine("  Refactored with Clean Architecture");
Console.WriteLine("===========================================");
Console.WriteLine($"  Data path: {dataPath}");
Console.WriteLine($"  Oura configured: {!string.IsNullOrEmpty(ouraToken)}");
Console.WriteLine($"  Picooc configured: {!string.IsNullOrEmpty(picoocEmail)}");
Console.WriteLine("===========================================");

app.Run("http://localhost:3001");
