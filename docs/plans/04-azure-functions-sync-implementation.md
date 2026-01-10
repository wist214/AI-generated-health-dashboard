# Azure Functions Data Sync Implementation Plan (.NET 8 Isolated Worker)

## 1. Overview

This document outlines the implementation plan for the Health Aggregator data synchronization service using **Azure Functions with .NET 8 Isolated Worker Model**, following **SOLID**, **DRY**, and **KISS** principles with Microsoft best practices for 2024-2026.

### 1.1 Technology Stack

- **Runtime**: .NET 8 Isolated Worker Model
- **Trigger**: Timer Trigger (CRON schedule every 30 minutes)
- **Database**: Entity Framework Core 8 with Azure SQL
- **Hosting**: Azure Functions Consumption Plan (Free tier)
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Monitoring**: Application Insights
- **Authentication**: Managed Identity for Azure SQL
- **Configuration**: Azure App Configuration / Key Vault

### 1.2 Architecture Goals

- **Idempotency**: Safe to run multiple times without duplicating data
- **Resilience**: Retry logic for transient failures
- **Observability**: Detailed logging and telemetry
- **Simplicity**: Concrete implementations for Oura, Picooc, and Cronometer (extensibility can be added later if needed)
- **Testability**: Interface-based design for unit testing
- **Efficiency**: Incremental sync to minimize API calls

---

## 2. Project Location and Structure

### 2.1 Important: Functions Project Location

**CRITICAL:** This new Azure Functions implementation should be created in a **separate folder** to avoid modifying the existing working Functions.

**Existing V1 Functions:** `HealthAggregatorApi/Functions/` (current working Azure Functions)

**New V2 Functions location:** `HealthAggregatorV2/src/Functions/` (new .NET 8 isolated worker implementation)

**Rationale:**
- Keep existing V1 sync system operational during migration
- Enable side-by-side testing and comparison
- Allow gradual migration of sync services
- Preserve rollback capability
- Both can write to the same database initially (or separate tables with prefix like `V2_Measurements`)

### 2.2 Functions Project Structure

```
HealthAggregatorV2/src/Functions/
├── Functions.csproj
├── host.json                           # Function runtime configuration
├── local.settings.json                 # Local development settings
├── Program.cs                          # Isolated worker entry point + DI
│
├── Functions/                          # Azure Functions (triggers)
│   ├── SyncTimerFunction.cs            # Timer trigger for sync
│   └── HealthCheckFunction.cs          # HTTP trigger for health check
│
├── Application/                        # Business logic layer
│   ├── Services/
│   │   ├── Interfaces/
│   │   │   ├── ISyncOrchestrator.cs
│   │   │   ├── IDataSourceSyncService.cs
│   │   │   └── IIdempotencyService.cs
│   │   ├── SyncOrchestrator.cs         # Coordinates sync across sources
│   │   ├── IdempotencyService.cs       # Prevents duplicate processing
│   │   └── DataSources/                # Source-specific implementations
│   │       ├── OuraSyncService.cs
│   │       ├── PicoocSyncService.cs
│   │       └── CronometerSyncService.cs
│   │
│   └── DTOs/
│       ├── SyncResult.cs
│       ├── SyncMetadata.cs
│       └── DataSourceMetrics.cs
│
├── Domain/                             # Shared domain models
│   ├── Entities/                       # EF Core entities (shared with API)
│   │   ├── Source.cs
│   │   ├── Measurement.cs
│   │   └── SyncLog.cs
│   │
│   └── Repositories/
│       ├── IMeasurementsRepository.cs
│       ├── ISourcesRepository.cs
│       └── ISyncLogRepository.cs
│
├── Infrastructure/                     # Infrastructure layer
│   ├── Data/
│   │   ├── HealthDbContext.cs          # EF Core DbContext (shared)
│   │   └── Repositories/
│   │       ├── MeasurementsRepository.cs
│   │       ├── SourcesRepository.cs
│   │       └── SyncLogRepository.cs
│   │
│   ├── ExternalApis/                   # External API clients
│   │   ├── Oura/
│   │   │   ├── IOuraApiClient.cs
│   │   │   ├── OuraApiClient.cs
│   │   │   ├── OuraApiModels.cs
│   │   │   └── OuraApiOptions.cs
│   │   ├── Picooc/
│   │   │   ├── IPicoocApiClient.cs
│   │   │   └── PicoocApiClient.cs
│   │   └── Cronometer/
│   │       ├── ICronometerApiClient.cs
│   │       └── CronometerApiClient.cs
│   │
│   └── Configuration/
│       ├── AzureKeyVaultConfiguration.cs
│       └── RetryPolicies.cs
│
├── Extensions/                         # Extension methods
│   ├── ServiceCollectionExtensions.cs  # DI registration
│   └── HostBuilderExtensions.cs        # Host configuration
│
└── Middleware/                         # Function middleware
    └── ExceptionHandlingMiddleware.cs
```

---

## 3. .NET 8 Isolated Worker Configuration

### 3.1 Program.cs (Entry Point)

```csharp
// Program.cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HealthAggregatorV2.Functions.Extensions;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(builder =>
    {
        // Function middleware
        builder.UseMiddleware<ExceptionHandlingMiddleware>();
    })
    .ConfigureServices((context, services) =>
    {
        // Application Insights
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // EF Core DbContext with connection resiliency
        services.AddDatabaseContext(context.Configuration);

        // Repositories
        services.AddRepositories();

        // Application services
        services.AddSyncServices();

        // External API clients
        services.AddExternalApiClients(context.Configuration);

        // HTTP client factory with retry policies
        services.AddHttpClientWithRetry();

        // Memory cache for idempotency
        services.AddMemoryCache();

        // Logging
        services.AddLogging();
    })
    .Build();

await host.RunAsync();
```

### 3.2 host.json

```json
{
  "version": "2.0",
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "maxTelemetryItemsPerSecond": 20,
        "excludedTypes": "Request"
      }
    },
    "logLevel": {
      "default": "Information",
      "Host.Results": "Error",
      "Function": "Information",
      "Host.Aggregator": "Trace"
    }
  },
  "functionTimeout": "00:10:00",
  "extensions": {
    "http": {
      "routePrefix": "api"
    }
  },
  "retry": {
    "strategy": "exponentialBackoff",
    "maxRetryCount": 3,
    "minimumInterval": "00:00:05",
    "maximumInterval": "00:01:00"
  }
}
```

---

## 4. Timer Trigger Function

### 4.1 SyncTimerFunction

```csharp
// Functions/SyncTimerFunction.cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using HealthAggregatorV2.Functions.Application.Services.Interfaces;

namespace HealthAggregatorV2.Functions.Functions;

public class SyncTimerFunction
{
    private readonly ISyncOrchestrator _syncOrchestrator;
    private readonly ILogger<SyncTimerFunction> _logger;

    public SyncTimerFunction(
        ISyncOrchestrator syncOrchestrator,
        ILogger<SyncTimerFunction> logger)
    {
        _syncOrchestrator = syncOrchestrator;
        _logger = logger;
    }

    [Function("SyncTimerFunction")]
    public async Task Run(
        [TimerTrigger("0 */30 * * * *")] TimerInfo timerInfo,
        FunctionContext context)
    {
        _logger.LogInformation(
            "Sync timer function started at {Time}. Next run: {NextRun}",
            DateTime.UtcNow,
            timerInfo.ScheduleStatus?.Next);

        try
        {
            var result = await _syncOrchestrator.SyncAllSourcesAsync(context.CancellationToken);

            _logger.LogInformation(
                "Sync completed. Success: {SuccessCount}, Failed: {FailedCount}",
                result.SuccessCount,
                result.FailedCount);

            // Track custom metrics in Application Insights
            context.GetLogger<SyncTimerFunction>().LogMetric("SyncSuccessCount", result.SuccessCount);
            context.GetLogger<SyncTimerFunction>().LogMetric("SyncFailedCount", result.FailedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync timer function failed");
            throw; // Re-throw to trigger retry policy
        }
    }
}
```

### 4.2 CRON Schedule Examples

```csharp
// Every 30 minutes
[TimerTrigger("0 */30 * * * *")]

// Every hour at :00
[TimerTrigger("0 0 * * * *")]

// Every day at 2:00 AM UTC
[TimerTrigger("0 0 2 * * *")]

// Every Monday at 8:00 AM UTC
[TimerTrigger("0 0 8 * * MON")]
```

---

## 5. Sync Orchestration Service

### 5.1 ISyncOrchestrator Interface

```csharp
// Application/Services/Interfaces/ISyncOrchestrator.cs
public interface ISyncOrchestrator
{
    Task<SyncResult> SyncAllSourcesAsync(CancellationToken cancellationToken = default);
    Task<SyncResult> SyncSourceAsync(string sourceName, CancellationToken cancellationToken = default);
}
```

### 5.2 SyncOrchestrator Implementation

```csharp
// Application/Services/SyncOrchestrator.cs
public class SyncOrchestrator : ISyncOrchestrator
{
    private readonly IEnumerable<IDataSourceSyncService> _syncServices;
    private readonly ISourcesRepository _sourcesRepository;
    private readonly ISyncLogRepository _syncLogRepository;
    private readonly ILogger<SyncOrchestrator> _logger;

    public SyncOrchestrator(
        IEnumerable<IDataSourceSyncService> syncServices,
        ISourcesRepository sourcesRepository,
        ISyncLogRepository syncLogRepository,
        ILogger<SyncOrchestrator> logger)
    {
        _syncServices = syncServices;
        _sourcesRepository = sourcesRepository;
        _syncLogRepository = syncLogRepository;
        _logger = logger;
    }

    public async Task<SyncResult> SyncAllSourcesAsync(CancellationToken cancellationToken = default)
    {
        var overallResult = new SyncResult();

        // Get enabled sources from database
        var enabledSources = await _sourcesRepository.GetEnabledSourcesAsync(cancellationToken);

        foreach (var source in enabledSources)
        {
            var syncService = _syncServices.FirstOrDefault(s => s.SourceName == source.ProviderName);
            if (syncService == null)
            {
                _logger.LogWarning("No sync service found for source: {SourceName}", source.ProviderName);
                continue;
            }

            try
            {
                _logger.LogInformation("Starting sync for source: {SourceName}", source.ProviderName);

                var result = await syncService.SyncAsync(cancellationToken);

                overallResult.SuccessCount += result.IsSuccess ? 1 : 0;
                overallResult.FailedCount += result.IsSuccess ? 0 : 1;
                overallResult.TotalRecordsSynced += result.RecordsSynced;

                // Log sync result
                await _syncLogRepository.AddAsync(new SyncLog
                {
                    SourceId = source.Id,
                    StartedAt = result.StartedAt,
                    CompletedAt = result.CompletedAt,
                    RecordsSynced = result.RecordsSynced,
                    IsSuccess = result.IsSuccess,
                    ErrorMessage = result.ErrorMessage,
                    Metadata = System.Text.Json.JsonSerializer.Serialize(result.Metadata)
                }, cancellationToken);

                // Update source LastSyncedAt
                if (result.IsSuccess)
                {
                    source.LastSyncedAt = DateTime.UtcNow;
                    await _sourcesRepository.UpdateAsync(source, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync source: {SourceName}", source.ProviderName);
                overallResult.FailedCount++;
            }
        }

        return overallResult;
    }

    public async Task<SyncResult> SyncSourceAsync(
        string sourceName,
        CancellationToken cancellationToken = default)
    {
        var syncService = _syncServices.FirstOrDefault(s => s.SourceName == sourceName);
        if (syncService == null)
        {
            throw new InvalidOperationException($"No sync service found for source: {sourceName}");
        }

        return await syncService.SyncAsync(cancellationToken);
    }
}
```

---

## 6. Data Source Sync Services

### 6.1 IDataSourceSyncService Interface

```csharp
// Application/Services/Interfaces/IDataSourceSyncService.cs
public interface IDataSourceSyncService
{
    string SourceName { get; }
    Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default);
}

// Application/DTOs/SyncResult.cs
public class SyncResult
{
    public bool IsSuccess { get; set; }
    public int RecordsSynced { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }

    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int TotalRecordsSynced { get; set; }
}
```

### 6.2 OuraSyncService Implementation

```csharp
// Application/Services/DataSources/OuraSyncService.cs
public class OuraSyncService : IDataSourceSyncService
{
    private readonly IOuraApiClient _ouraClient;
    private readonly IMeasurementsRepository _measurementsRepository;
    private readonly ISourcesRepository _sourcesRepository;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<OuraSyncService> _logger;

    public string SourceName => "Oura";

    public OuraSyncService(
        IOuraApiClient ouraClient,
        IMeasurementsRepository measurementsRepository,
        ISourcesRepository sourcesRepository,
        IIdempotencyService idempotencyService,
        ILogger<OuraSyncService> logger)
    {
        _ouraClient = ouraClient;
        _measurementsRepository = measurementsRepository;
        _sourcesRepository = sourcesRepository;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        var result = new SyncResult
        {
            StartedAt = DateTime.UtcNow
        };

        try
        {
            // Get source configuration
            var source = await _sourcesRepository.GetByProviderNameAsync(SourceName, cancellationToken);
            if (source == null)
            {
                throw new InvalidOperationException($"Source '{SourceName}' not found in database");
            }

            // Determine sync start date (incremental sync)
            var startDate = source.LastSyncedAt?.Date ?? DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow.Date;

            _logger.LogInformation(
                "Syncing Oura data from {StartDate} to {EndDate}",
                startDate,
                endDate);

            // Fetch sleep data
            var sleepData = await _ouraClient.GetSleepDataAsync(startDate, endDate, cancellationToken);
            var sleepMeasurements = MapSleepDataToMeasurements(sleepData, source.Id);

            // Fetch activity data
            var activityData = await _ouraClient.GetActivityDataAsync(startDate, endDate, cancellationToken);
            var activityMeasurements = MapActivityDataToMeasurements(activityData, source.Id);

            // Fetch readiness data
            var readinessData = await _ouraClient.GetReadinessDataAsync(startDate, endDate, cancellationToken);
            var readinessMeasurements = MapReadinessDataToMeasurements(readinessData, source.Id);

            // Combine all measurements
            var allMeasurements = sleepMeasurements
                .Concat(activityMeasurements)
                .Concat(readinessMeasurements)
                .ToList();

            // Filter out duplicates using idempotency service
            var newMeasurements = new List<Measurement>();
            foreach (var measurement in allMeasurements)
            {
                var isDuplicate = await _idempotencyService.IsDuplicateAsync(
                    measurement.MetricTypeId,
                    measurement.SourceId,
                    measurement.Timestamp,
                    cancellationToken);

                if (!isDuplicate)
                {
                    newMeasurements.Add(measurement);
                }
            }

            // Bulk insert new measurements
            if (newMeasurements.Any())
            {
                await _measurementsRepository.AddRangeAsync(newMeasurements, cancellationToken);
                _logger.LogInformation("Inserted {Count} new measurements", newMeasurements.Count);
            }

            result.IsSuccess = true;
            result.RecordsSynced = newMeasurements.Count;
            result.Metadata = new Dictionary<string, object>
            {
                { "SleepRecords", sleepData.Count() },
                { "ActivityRecords", activityData.Count() },
                { "ReadinessRecords", readinessData.Count() },
                { "DuplicatesSkipped", allMeasurements.Count - newMeasurements.Count }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync Oura data");
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            result.CompletedAt = DateTime.UtcNow;
        }

        return result;
    }

    private IEnumerable<Measurement> MapSleepDataToMeasurements(
        IEnumerable<OuraSleepData> sleepData,
        long sourceId)
    {
        var measurements = new List<Measurement>();

        foreach (var sleep in sleepData)
        {
            measurements.Add(new Measurement
            {
                MetricTypeId = 1, // sleep_score
                SourceId = sourceId,
                Value = sleep.Score,
                Timestamp = sleep.Date.ToDateTime(TimeOnly.MinValue),
                RawDataJson = System.Text.Json.JsonSerializer.Serialize(sleep)
            });

            // Additional sleep metrics...
        }

        return measurements;
    }

    private IEnumerable<Measurement> MapActivityDataToMeasurements(
        IEnumerable<OuraActivityData> activityData,
        long sourceId)
    {
        // Similar mapping logic
        return Array.Empty<Measurement>();
    }

    private IEnumerable<Measurement> MapReadinessDataToMeasurements(
        IEnumerable<OuraReadinessData> readinessData,
        long sourceId)
    {
        // Similar mapping logic
        return Array.Empty<Measurement>();
    }
}
```

---

## 7. Idempotency Service

### 7.1 IIdempotencyService Interface

```csharp
// Application/Services/Interfaces/IIdempotencyService.cs
public interface IIdempotencyService
{
    Task<bool> IsDuplicateAsync(
        int metricTypeId,
        long sourceId,
        DateTime timestamp,
        CancellationToken cancellationToken = default);
}
```

### 7.2 IdempotencyService Implementation

```csharp
// Application/Services/IdempotencyService.cs
public class IdempotencyService : IIdempotencyService
{
    private readonly IMeasurementsRepository _measurementsRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<IdempotencyService> _logger;

    public IdempotencyService(
        IMeasurementsRepository measurementsRepository,
        IMemoryCache cache,
        ILogger<IdempotencyService> logger)
    {
        _measurementsRepository = measurementsRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsDuplicateAsync(
        int metricTypeId,
        long sourceId,
        DateTime timestamp,
        CancellationToken cancellationToken = default)
    {
        // Cache key for faster lookups
        var cacheKey = $"measurement:{metricTypeId}:{sourceId}:{timestamp:yyyy-MM-dd-HH-mm-ss}";

        if (_cache.TryGetValue(cacheKey, out bool _))
        {
            return true; // Already processed in this run
        }

        // Check database
        var exists = await _measurementsRepository.ExistsAsync(
            metricTypeId,
            sourceId,
            timestamp,
            cancellationToken);

        if (exists)
        {
            // Cache the result for 10 minutes
            _cache.Set(cacheKey, true, TimeSpan.FromMinutes(10));
        }

        return exists;
    }
}
```

---

## 8. External API Client Example (Oura)

### 8.1 IOuraApiClient Interface

```csharp
// Infrastructure/ExternalApis/Oura/IOuraApiClient.cs
public interface IOuraApiClient
{
    Task<IEnumerable<OuraSleepData>> GetSleepDataAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<OuraActivityData>> GetActivityDataAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<OuraReadinessData>> GetReadinessDataAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}
```

### 8.2 OuraApiClient Implementation

```csharp
// Infrastructure/ExternalApis/Oura/OuraApiClient.cs
public class OuraApiClient : IOuraApiClient
{
    private readonly HttpClient _httpClient;
    private readonly OuraApiOptions _options;
    private readonly ILogger<OuraApiClient> _logger;

    public OuraApiClient(
        HttpClient httpClient,
        IOptions<OuraApiOptions> options,
        ILogger<OuraApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri("https://api.ouraring.com/v2/");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.AccessToken}");
    }

    public async Task<IEnumerable<OuraSleepData>> GetSleepDataAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var url = $"usercollection/daily_sleep?start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = System.Text.Json.JsonSerializer.Deserialize<OuraSleepResponse>(content);

            return result?.Data ?? Array.Empty<OuraSleepData>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch Oura sleep data");
            throw;
        }
    }

    public async Task<IEnumerable<OuraActivityData>> GetActivityDataAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        // Similar implementation
        return Array.Empty<OuraActivityData>();
    }

    public async Task<IEnumerable<OuraReadinessData>> GetReadinessDataAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        // Similar implementation
        return Array.Empty<OuraReadinessData>();
    }
}
```

### 8.3 Oura API Models

```csharp
// Infrastructure/ExternalApis/Oura/OuraApiModels.cs
public class OuraSleepResponse
{
    public IEnumerable<OuraSleepData> Data { get; set; } = Array.Empty<OuraSleepData>();
}

public class OuraSleepData
{
    public string Id { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public int Score { get; set; }
    public int TotalSleepDuration { get; set; }
    public int RemSleepDuration { get; set; }
    public int DeepSleepDuration { get; set; }
    public int Efficiency { get; set; }
    public int Restfulness { get; set; }
}

public class OuraActivityData
{
    public string Id { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public int Score { get; set; }
    public int Steps { get; set; }
    public int CaloriesActive { get; set; }
    public int CaloriesTotal { get; set; }
}

public class OuraReadinessData
{
    public string Id { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public int Score { get; set; }
    public int TemperatureDeviation { get; set; }
    public int RestingHeartRate { get; set; }
}
```

---

## 9. Dependency Injection Configuration

### 9.1 ServiceCollectionExtensions

```csharp
// Extensions/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabaseContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<HealthDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING");
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
                sqlOptions.CommandTimeout(60);
            });
        });

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IMeasurementsRepository, MeasurementsRepository>();
        services.AddScoped<ISourcesRepository, SourcesRepository>();
        services.AddScoped<ISyncLogRepository, SyncLogRepository>();

        return services;
    }

    public static IServiceCollection AddSyncServices(this IServiceCollection services)
    {
        services.AddScoped<ISyncOrchestrator, SyncOrchestrator>();
        services.AddScoped<IIdempotencyService, IdempotencyService>();

        // Register all sync services
        services.AddScoped<IDataSourceSyncService, OuraSyncService>();
        services.AddScoped<IDataSourceSyncService, PicoocSyncService>();
        services.AddScoped<IDataSourceSyncService, CronometerSyncService>();

        return services;
    }

    public static IServiceCollection AddExternalApiClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Oura API client
        services.AddHttpClient<IOuraApiClient, OuraApiClient>()
            .AddPolicyHandler(GetRetryPolicy());

        services.Configure<OuraApiOptions>(configuration.GetSection("Oura"));

        // Picooc API client
        services.AddHttpClient<IPicoocApiClient, PicoocApiClient>()
            .AddPolicyHandler(GetRetryPolicy());

        // Cronometer API client
        services.AddHttpClient<ICronometerApiClient, CronometerApiClient>()
            .AddPolicyHandler(GetRetryPolicy());

        return services;
    }

    public static IServiceCollection AddHttpClientWithRetry(this IServiceCollection services)
    {
        services.AddHttpClient()
            .AddTransientHttpErrorPolicy(policy =>
                policy.WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds}s");
                });
    }
}
```

---

## 10. Error Handling and Retry Policies

### 10.1 Exception Handling Middleware

```csharp
// Middleware/ExceptionHandlingMiddleware.cs
public class ExceptionHandlingMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in function: {FunctionName}", context.FunctionDefinition.Name);

            // Track exception in Application Insights
            var telemetryClient = context.InstanceServices.GetService<TelemetryClient>();
            telemetryClient?.TrackException(ex);

            throw; // Re-throw to trigger retry policy
        }
    }
}
```

### 10.2 Retry Policy with Polly

```csharp
// Infrastructure/Configuration/RetryPolicies.cs
public static class RetryPolicies
{
    public static IAsyncPolicy<HttpResponseMessage> GetHttpRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    // Log retry attempt
                });
    }

    public static IAsyncPolicy GetDatabaseRetryPolicy()
    {
        return Policy
            .Handle<SqlException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
}
```

---

## 11. Application Insights Integration

### 11.1 Custom Telemetry

```csharp
// Application/Services/SyncOrchestrator.cs (enhanced with telemetry)
public async Task<SyncResult> SyncAllSourcesAsync(CancellationToken cancellationToken = default)
{
    using var operation = _telemetryClient.StartOperation<DependencyTelemetry>("SyncAllSources");

    try
    {
        var result = new SyncResult();

        // ... sync logic ...

        _telemetryClient.TrackMetric("SyncSuccessCount", result.SuccessCount);
        _telemetryClient.TrackMetric("SyncFailedCount", result.FailedCount);
        _telemetryClient.TrackMetric("TotalRecordsSynced", result.TotalRecordsSynced);

        operation.Telemetry.Success = true;
        return result;
    }
    catch (Exception ex)
    {
        operation.Telemetry.Success = false;
        _telemetryClient.TrackException(ex);
        throw;
    }
}
```

### 11.2 Custom Metrics

```csharp
// Track custom metrics
_logger.LogMetric("OuraSyncDuration", duration.TotalSeconds);
_logger.LogMetric("OuraRecordsSynced", recordCount);
```

---

## 12. Managed Identity for Azure SQL

### 12.1 Connection String Configuration

```csharp
// Extensions/ServiceCollectionExtensions.cs
public static IServiceCollection AddDatabaseContext(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.AddDbContext<HealthDbContext>(options =>
    {
        var connectionString = configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING");

        // Use Managed Identity in production
        if (string.IsNullOrEmpty(connectionString))
        {
            var sqlServer = configuration["AzureSql:ServerName"];
            var database = configuration["AzureSql:DatabaseName"];

            var credential = new DefaultAzureCredential();
            var token = credential.GetToken(
                new Azure.Core.TokenRequestContext(new[] { "https://database.windows.net/.default" }));

            connectionString = $"Server={sqlServer};Database={database};";
        }

        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
        });
    });

    return services;
}
```

---

## 13. Testing Strategy

### 13.1 Unit Test Example

```csharp
// Tests/Services/OuraSyncServiceTests.cs
[TestClass]
public class OuraSyncServiceTests
{
    private Mock<IOuraApiClient> _mockOuraClient = null!;
    private Mock<IMeasurementsRepository> _mockMeasurementsRepo = null!;
    private Mock<ISourcesRepository> _mockSourcesRepo = null!;
    private Mock<IIdempotencyService> _mockIdempotencyService = null!;
    private OuraSyncService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockOuraClient = new Mock<IOuraApiClient>();
        _mockMeasurementsRepo = new Mock<IMeasurementsRepository>();
        _mockSourcesRepo = new Mock<ISourcesRepository>();
        _mockIdempotencyService = new Mock<IIdempotencyService>();

        _service = new OuraSyncService(
            _mockOuraClient.Object,
            _mockMeasurementsRepo.Object,
            _mockSourcesRepo.Object,
            _mockIdempotencyService.Object,
            Mock.Of<ILogger<OuraSyncService>>());
    }

    [TestMethod]
    public async Task SyncAsync_WhenSuccessful_ReturnsSuccessResult()
    {
        // Arrange
        var source = new Source { Id = 1, ProviderName = "Oura", IsEnabled = true };
        _mockSourcesRepo.Setup(r => r.GetByProviderNameAsync("Oura", default))
            .ReturnsAsync(source);

        var sleepData = new[]
        {
            new OuraSleepData { Date = DateOnly.FromDateTime(DateTime.UtcNow), Score = 85 }
        };
        _mockOuraClient.Setup(c => c.GetSleepDataAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync(sleepData);

        _mockIdempotencyService.Setup(s => s.IsDuplicateAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync(false);

        // Act
        var result = await _service.SyncAsync();

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.RecordsSynced > 0);
        _mockMeasurementsRepo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<Measurement>>(), default), Times.Once);
    }
}
```

---

## 14. Gray Areas / Questions

### 14.1 Sync Conflict Resolution

**Question:** How should conflicts be handled when multiple sources provide the same metric?

**Options:**
- **Option 1:** Last-write-wins (overwrite existing)
- **Option 2:** Keep both with source precedence (e.g., Oura > Picooc)
- **Option 3:** Average conflicting values

**Note:** User will clarify this later. For now, implement Option 1 (last-write-wins) as the simplest approach.

### 14.2 Incremental vs Full Sync

**Question:** Should sync always be incremental or support full re-sync?

**Options:**
- **Option 1:** Always incremental (based on LastSyncedAt)
- **Option 2:** Full sync on demand (manual trigger)
- **Option 3:** Full sync if LastSyncedAt > 30 days

**Recommendation:** Option 1 by default, with Option 2 for troubleshooting.

### 14.3 Rate Limiting for External APIs

**Question:** How should API rate limits be handled?

**Options:**
- **Option 1:** Exponential backoff with 429 status code detection
- **Option 2:** Pre-emptive throttling (e.g., max 10 requests/minute)
- **Option 3:** Queue-based approach with Azure Storage Queue

**Recommendation:** Option 1 (reactive) + Option 2 (preventive). No complex token management needed since we're not using OAuth for now.

### 14.4 Sync Failure Notifications

**Question:** Should sync failures trigger alerts?

**Options:**
- **Option 1:** No alerts (silent failure)
- **Option 2:** Azure Monitor alerts on repeated failures
- **Option 3:** Email notifications via Logic Apps

**Recommendation:** Option 2 (Azure Monitor) for operational visibility.

### 14.5 Historical Data Backfill

**Question:** Should the system support backfilling historical data?

**Options:**
- **Option 1:** No backfill (only sync forward from today)
- **Option 2:** One-time backfill on first sync (e.g., last 90 days)
- **Option 3:** Manual backfill trigger with date range

**Recommendation:** Option 2 (automatic backfill on first sync).

---

## 15. Implementation Checklist

- [ ] Create Azure Functions project with .NET 8 Isolated Worker
- [ ] Configure host.json with retry policies
- [ ] Set up dependency injection in Program.cs
- [ ] Create timer trigger function (30-minute CRON)
- [ ] Implement ISyncOrchestrator interface and service
- [ ] Implement IDataSourceSyncService for each data source
- [ ] Create external API clients (Oura, Picooc, Cronometer)
- [ ] Implement idempotency service with caching
- [ ] Configure EF Core with connection resiliency
- [ ] Add Application Insights telemetry
- [ ] Implement exception handling middleware
- [ ] Configure Managed Identity for Azure SQL
- [ ] Add retry policies with Polly for HTTP clients
- [ ] Create SyncLog entity for audit trail
- [ ] Write unit tests for sync services
- [ ] Write integration tests with mocked APIs
- [ ] Test locally with Azurite and LocalDB
- [ ] Deploy to Azure Functions Consumption Plan
- [ ] Configure app settings in Azure
- [ ] Monitor sync execution in Application Insights
- [ ] Set up Azure Monitor alerts for failures

---

## 16. References

### Microsoft Documentation

- [Azure Functions .NET 8 Isolated Worker](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide)
- [Timer Trigger for Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer)
- [Dependency Injection in Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection)
- [Application Insights for Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-monitoring)
- [Managed Identity with Azure SQL](https://learn.microsoft.com/en-us/azure/azure-sql/database/authentication-aad-configure)
- [HTTP Client Factory Best Practices](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests)

### Best Practices

- Use .NET 8 Isolated Worker Model for better performance
- Implement idempotency to prevent duplicate data
- Use incremental sync to minimize API calls
- Add retry policies for transient failures
- Track custom metrics in Application Insights
- Use Managed Identity for secure database access
- Batch database operations for efficiency
- Cache duplicate checks for performance
- Log detailed sync results for troubleshooting
- Use CRON expressions for flexible scheduling
