# Integration and Extensibility Architecture Plan

## 1. Overview

This document outlines the integration architecture and extensibility strategy for the Health Aggregator system, enabling seamless addition of new data sources, custom metrics, and third-party integrations following **SOLID**, **DRY**, and **KISS** principles with Microsoft best practices for 2024-2026.

### 1.1 Architecture Goals

- **Plugin Architecture**: Add new data sources without modifying core code
- **Adapter Pattern**: Normalize heterogeneous external APIs
- **Event-Driven**: Decouple components for scalability
- **API Versioning**: Support backward compatibility
- **Webhook Support**: Enable real-time data updates
- **Data Transformation**: Flexible pipeline for custom mappings
- **Extension Points**: Allow custom business logic injection

### 1.2 Technology Stack

- **Plugin Framework**: .NET 8 with dynamic assembly loading
- **Messaging**: Azure Service Bus (future) / In-memory events (current)
- **API Gateway**: Azure API Management (future) / Direct API (current)
- **Webhook Receiver**: Azure Functions HTTP trigger
- **Data Pipeline**: Fluent validation + AutoMapper
- **Extension System**: Strategy pattern + Factory pattern

---

## 2. Plugin Architecture for Data Sources

### 2.1 Plugin Interface Definition

```csharp
// Application/Integrations/IDataSourcePlugin.cs
public interface IDataSourcePlugin
{
    /// <summary>
    /// Unique identifier for the data source (e.g., "oura", "fitbit", "withings")
    /// </summary>
    string SourceId { get; }

    /// <summary>
    /// Human-readable name for the data source
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Version of the plugin implementation
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// Supported metric types this plugin can provide
    /// </summary>
    IReadOnlyList<string> SupportedMetrics { get; }

    /// <summary>
    /// Initialize the plugin with configuration
    /// </summary>
    Task InitializeAsync(DataSourceConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate connection to the external data source
    /// </summary>
    Task<ValidationResult> ValidateConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sync data from the external source
    /// </summary>
    Task<SyncResult> SyncAsync(SyncOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get real-time data (if supported)
    /// </summary>
    Task<IEnumerable<Measurement>> GetRealtimeDataAsync(CancellationToken cancellationToken = default);
}

// Application/Integrations/DataSourceConfiguration.cs
public class DataSourceConfiguration
{
    public string SourceId { get; set; } = string.Empty;
    public Dictionary<string, string> Settings { get; set; } = new();
    public string? ApiKey { get; set; }
    public string? OAuthToken { get; set; }
    public DateTime? LastSyncedAt { get; set; }
}

// Application/Integrations/SyncOptions.cs
public class SyncOptions
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool FullSync { get; set; }
    public IEnumerable<string>? MetricTypesFilter { get; set; }
}

// Application/Integrations/ValidationResult.cs
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public Dictionary<string, object>? Metadata { get; set; }
}
```

### 2.2 Plugin Discovery and Registration

```csharp
// Application/Integrations/PluginRegistry.cs
public interface IPluginRegistry
{
    void RegisterPlugin(IDataSourcePlugin plugin);
    IDataSourcePlugin? GetPlugin(string sourceId);
    IEnumerable<IDataSourcePlugin> GetAllPlugins();
    IEnumerable<IDataSourcePlugin> GetPluginsByMetric(string metricType);
}

public class PluginRegistry : IPluginRegistry
{
    private readonly Dictionary<string, IDataSourcePlugin> _plugins = new();
    private readonly ILogger<PluginRegistry> _logger;

    public PluginRegistry(ILogger<PluginRegistry> logger)
    {
        _logger = logger;
    }

    public void RegisterPlugin(IDataSourcePlugin plugin)
    {
        if (_plugins.ContainsKey(plugin.SourceId))
        {
            _logger.LogWarning("Plugin {SourceId} is already registered. Overwriting.", plugin.SourceId);
        }

        _plugins[plugin.SourceId] = plugin;
        _logger.LogInformation(
            "Registered plugin: {DisplayName} (v{Version}) with {MetricCount} metrics",
            plugin.DisplayName,
            plugin.Version,
            plugin.SupportedMetrics.Count);
    }

    public IDataSourcePlugin? GetPlugin(string sourceId)
    {
        return _plugins.TryGetValue(sourceId, out var plugin) ? plugin : null;
    }

    public IEnumerable<IDataSourcePlugin> GetAllPlugins()
    {
        return _plugins.Values;
    }

    public IEnumerable<IDataSourcePlugin> GetPluginsByMetric(string metricType)
    {
        return _plugins.Values
            .Where(p => p.SupportedMetrics.Contains(metricType, StringComparer.OrdinalIgnoreCase));
    }
}
```

### 2.3 Example Plugin Implementation (Fitbit)

```csharp
// Application/Integrations/Plugins/FitbitPlugin.cs
public class FitbitPlugin : IDataSourcePlugin
{
    private readonly IFitbitApiClient _apiClient;
    private readonly IMeasurementsRepository _measurementsRepository;
    private readonly ILogger<FitbitPlugin> _logger;
    private DataSourceConfiguration _configuration = null!;

    public string SourceId => "fitbit";
    public string DisplayName => "Fitbit";
    public Version Version => new Version(1, 0, 0);

    public IReadOnlyList<string> SupportedMetrics => new[]
    {
        "steps",
        "heart_rate",
        "calories_burned",
        "distance",
        "floors_climbed",
        "sleep_duration"
    };

    public FitbitPlugin(
        IFitbitApiClient apiClient,
        IMeasurementsRepository measurementsRepository,
        ILogger<FitbitPlugin> logger)
    {
        _apiClient = apiClient;
        _measurementsRepository = measurementsRepository;
        _logger = logger;
    }

    public Task InitializeAsync(DataSourceConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _configuration = configuration;

        if (string.IsNullOrEmpty(_configuration.OAuthToken))
        {
            throw new InvalidOperationException("Fitbit OAuth token is required");
        }

        _apiClient.SetAccessToken(_configuration.OAuthToken);

        return Task.CompletedTask;
    }

    public async Task<ValidationResult> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await _apiClient.GetUserProfileAsync(cancellationToken);

            return new ValidationResult
            {
                IsValid = true,
                Metadata = new Dictionary<string, object>
                {
                    { "UserId", profile.UserId },
                    { "DisplayName", profile.DisplayName }
                }
            };
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<SyncResult> SyncAsync(SyncOptions options, CancellationToken cancellationToken = default)
    {
        var result = new SyncResult { StartedAt = DateTime.UtcNow };

        try
        {
            var startDate = options.StartDate ?? _configuration.LastSyncedAt ?? DateTime.UtcNow.AddDays(-7);
            var endDate = options.EndDate ?? DateTime.UtcNow;

            // Sync steps
            var stepsData = await _apiClient.GetStepsAsync(startDate, endDate, cancellationToken);
            var stepsMeasurements = stepsData.Select(d => new Measurement
            {
                MetricTypeId = GetMetricTypeId("steps"),
                SourceId = GetSourceId(),
                Value = d.Value,
                Timestamp = d.Date,
                RawDataJson = System.Text.Json.JsonSerializer.Serialize(d)
            });

            await _measurementsRepository.AddRangeAsync(stepsMeasurements, cancellationToken);

            // Sync other metrics...

            result.IsSuccess = true;
            result.RecordsSynced = stepsMeasurements.Count();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fitbit sync failed");
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            result.CompletedAt = DateTime.UtcNow;
        }

        return result;
    }

    public async Task<IEnumerable<Measurement>> GetRealtimeDataAsync(CancellationToken cancellationToken = default)
    {
        // Fitbit doesn't support real-time push, return empty
        return await Task.FromResult(Array.Empty<Measurement>());
    }

    private int GetMetricTypeId(string metricName)
    {
        // Map metric name to MetricTypeId
        return metricName switch
        {
            "steps" => 4,
            "heart_rate" => 7,
            "calories_burned" => 8,
            _ => throw new ArgumentException($"Unknown metric: {metricName}")
        };
    }

    private long GetSourceId()
    {
        // Retrieve from database or configuration
        return 2; // Fitbit source ID
    }
}
```

### 2.4 Dynamic Plugin Loading

```csharp
// Application/Integrations/PluginLoader.cs
public class PluginLoader
{
    private readonly IPluginRegistry _registry;
    private readonly ILogger<PluginLoader> _logger;

    public PluginLoader(IPluginRegistry registry, ILogger<PluginLoader> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public void LoadPluginsFromAssembly(Assembly assembly)
    {
        var pluginTypes = assembly.GetTypes()
            .Where(t => typeof(IDataSourcePlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in pluginTypes)
        {
            try
            {
                var plugin = (IDataSourcePlugin)Activator.CreateInstance(type)!;
                _registry.RegisterPlugin(plugin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin: {TypeName}", type.Name);
            }
        }
    }

    public void LoadPluginsFromDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            _logger.LogWarning("Plugin directory not found: {Directory}", directory);
            return;
        }

        var dllFiles = Directory.GetFiles(directory, "*.dll");

        foreach (var dllFile in dllFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllFile);
                LoadPluginsFromAssembly(assembly);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load assembly: {DllFile}", dllFile);
            }
        }
    }
}
```

---

## 3. Adapter Pattern for External APIs

### 3.1 Data Source Adapter Interface

```csharp
// Application/Integrations/Adapters/IDataSourceAdapter.cs
public interface IDataSourceAdapter
{
    string SourceName { get; }

    Task<IEnumerable<NormalizedMeasurement>> FetchDataAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}

// Application/Integrations/Adapters/NormalizedMeasurement.cs
public class NormalizedMeasurement
{
    public string MetricType { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
```

### 3.2 Adapter Implementation Example

```csharp
// Application/Integrations/Adapters/OuraAdapter.cs
public class OuraAdapter : IDataSourceAdapter
{
    private readonly IOuraApiClient _apiClient;
    private readonly ILogger<OuraAdapter> _logger;

    public string SourceName => "Oura";

    public OuraAdapter(IOuraApiClient apiClient, ILogger<OuraAdapter> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<IEnumerable<NormalizedMeasurement>> FetchDataAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var measurements = new List<NormalizedMeasurement>();

        // Fetch sleep data
        var sleepData = await _apiClient.GetSleepDataAsync(startDate, endDate, cancellationToken);
        measurements.AddRange(sleepData.Select(d => new NormalizedMeasurement
        {
            MetricType = "sleep_score",
            Value = d.Score,
            Unit = "score",
            Timestamp = d.Date.ToDateTime(TimeOnly.MinValue),
            Metadata = new Dictionary<string, object>
            {
                { "total_duration", d.TotalSleepDuration },
                { "efficiency", d.Efficiency }
            }
        }));

        // Fetch activity data
        var activityData = await _apiClient.GetActivityDataAsync(startDate, endDate, cancellationToken);
        measurements.AddRange(activityData.Select(d => new NormalizedMeasurement
        {
            MetricType = "steps",
            Value = d.Steps,
            Unit = "steps",
            Timestamp = d.Date.ToDateTime(TimeOnly.MinValue)
        }));

        return measurements;
    }
}
```

---

## 4. Event-Driven Communication

### 4.1 Domain Events

```csharp
// Domain/Events/IDomainEvent.cs
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}

// Domain/Events/MeasurementSyncedEvent.cs
public class MeasurementSyncedEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    public string SourceName { get; init; } = string.Empty;
    public int RecordCount { get; init; }
    public IEnumerable<string> MetricTypes { get; init; } = Array.Empty<string>();
}

// Domain/Events/SyncFailedEvent.cs
public class SyncFailedEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    public string SourceName { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public int RetryCount { get; init; }
}
```

### 4.2 Event Bus (In-Memory)

```csharp
// Application/Events/IEventBus.cs
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;

    void Subscribe<TEvent>(IEventHandler<TEvent> handler)
        where TEvent : IDomainEvent;
}

// Application/Events/IEventHandler.cs
public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

// Application/Events/InMemoryEventBus.cs
public class InMemoryEventBus : IEventBus
{
    private readonly Dictionary<Type, List<object>> _handlers = new();
    private readonly ILogger<InMemoryEventBus> _logger;

    public InMemoryEventBus(ILogger<InMemoryEventBus> logger)
    {
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        var eventType = typeof(TEvent);

        if (!_handlers.TryGetValue(eventType, out var handlers))
        {
            _logger.LogDebug("No handlers registered for event: {EventType}", eventType.Name);
            return;
        }

        foreach (var handler in handlers.Cast<IEventHandler<TEvent>>())
        {
            try
            {
                await handler.HandleAsync(@event, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling event: {EventType}", eventType.Name);
            }
        }
    }

    public void Subscribe<TEvent>(IEventHandler<TEvent> handler)
        where TEvent : IDomainEvent
    {
        var eventType = typeof(TEvent);

        if (!_handlers.ContainsKey(eventType))
        {
            _handlers[eventType] = new List<object>();
        }

        _handlers[eventType].Add(handler);
    }
}
```

### 4.3 Event Handler Example

```csharp
// Application/Events/Handlers/MeasurementSyncedEventHandler.cs
public class MeasurementSyncedEventHandler : IEventHandler<MeasurementSyncedEvent>
{
    private readonly ILogger<MeasurementSyncedEventHandler> _logger;
    private readonly IDailySummaryService _summaryService;

    public MeasurementSyncedEventHandler(
        ILogger<MeasurementSyncedEventHandler> logger,
        IDailySummaryService summaryService)
    {
        _logger = logger;
        _summaryService = summaryService;
    }

    public async Task HandleAsync(MeasurementSyncedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing MeasurementSyncedEvent: {SourceName}, {RecordCount} records",
            @event.SourceName,
            @event.RecordCount);

        // Trigger daily summary recalculation
        await _summaryService.RecalculateSummariesAsync(cancellationToken);

        // Additional side effects...
    }
}
```

### 4.4 Azure Service Bus Integration (Future)

```csharp
// Infrastructure/Messaging/AzureServiceBusEventBus.cs
public class AzureServiceBusEventBus : IEventBus
{
    private readonly ServiceBusSender _sender;
    private readonly ILogger<AzureServiceBusEventBus> _logger;

    public AzureServiceBusEventBus(ServiceBusClient client, ILogger<AzureServiceBusEventBus> logger)
    {
        _sender = client.CreateSender("health-events");
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        var messageBody = System.Text.Json.JsonSerializer.Serialize(@event);
        var message = new ServiceBusMessage(messageBody)
        {
            MessageId = @event.EventId.ToString(),
            Subject = typeof(TEvent).Name
        };

        await _sender.SendMessageAsync(message, cancellationToken);
        _logger.LogInformation("Published event: {EventType}", typeof(TEvent).Name);
    }

    public void Subscribe<TEvent>(IEventHandler<TEvent> handler)
        where TEvent : IDomainEvent
    {
        // Handled by separate Azure Function with Service Bus trigger
        throw new NotImplementedException("Use Azure Functions for subscription");
    }
}
```

---

## 5. API Versioning Strategy

### 5.1 URL-Based Versioning

```csharp
// Endpoints/V1/MetricsEndpoints.cs
public static class MetricsEndpointsV1
{
    public static void MapMetricsEndpointsV1(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/metrics")
            .WithTags("Metrics V1")
            .WithOpenApi();

        group.MapGet("/latest", GetLatestMetrics);
        group.MapGet("/range", GetMetricsInRange);
    }

    private static async Task<IResult> GetLatestMetrics(IMetricsService service)
    {
        var result = await service.GetLatestMetricsAsync();
        return Results.Ok(result);
    }

    private static async Task<IResult> GetMetricsInRange(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        IMetricsService service)
    {
        var result = await service.GetMetricsInRangeAsync(from, to);
        return Results.Ok(result);
    }
}

// Endpoints/V2/MetricsEndpoints.cs (Future Version)
public static class MetricsEndpointsV2
{
    public static void MapMetricsEndpointsV2(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v2/metrics")
            .WithTags("Metrics V2")
            .WithOpenApi();

        // V2 might include pagination, filtering, etc.
        group.MapGet("/latest", GetLatestMetricsWithPagination);
    }

    private static async Task<IResult> GetLatestMetricsWithPagination(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        IMetricsService service)
    {
        // New implementation with pagination
        return Results.Ok(new { page, pageSize });
    }
}
```

### 5.2 API Versioning Configuration

```csharp
// Program.cs
var app = builder.Build();

// Register versioned endpoints
app.MapMetricsEndpointsV1();
app.MapMetricsEndpointsV2();

// Default route redirects to latest version
app.MapGet("/api/metrics/latest", () => Results.Redirect("/api/v2/metrics/latest"));
```

---

## 6. Webhook Support for Real-Time Updates

### 6.1 Webhook Registration Endpoint

```csharp
// Endpoints/WebhooksEndpoints.cs
public static class WebhooksEndpoints
{
    public static void MapWebhooksEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/webhooks")
            .WithTags("Webhooks")
            .WithOpenApi();

        group.MapPost("/oura", HandleOuraWebhook);
        group.MapPost("/fitbit", HandleFitbitWebhook);
    }

    private static async Task<IResult> HandleOuraWebhook(
        [FromBody] OuraWebhookPayload payload,
        IWebhookProcessor processor)
    {
        await processor.ProcessOuraWebhookAsync(payload);
        return Results.Ok();
    }

    private static async Task<IResult> HandleFitbitWebhook(
        [FromBody] FitbitWebhookPayload payload,
        IWebhookProcessor processor)
    {
        await processor.ProcessFitbitWebhookAsync(payload);
        return Results.Ok();
    }
}
```

### 6.2 Webhook Processor

```csharp
// Application/Services/WebhookProcessor.cs
public interface IWebhookProcessor
{
    Task ProcessOuraWebhookAsync(OuraWebhookPayload payload, CancellationToken cancellationToken = default);
    Task ProcessFitbitWebhookAsync(FitbitWebhookPayload payload, CancellationToken cancellationToken = default);
}

public class WebhookProcessor : IWebhookProcessor
{
    private readonly IPluginRegistry _pluginRegistry;
    private readonly IEventBus _eventBus;
    private readonly ILogger<WebhookProcessor> _logger;

    public WebhookProcessor(
        IPluginRegistry pluginRegistry,
        IEventBus eventBus,
        ILogger<WebhookProcessor> logger)
    {
        _pluginRegistry = pluginRegistry;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task ProcessOuraWebhookAsync(OuraWebhookPayload payload, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing Oura webhook: {EventType}", payload.EventType);

        var plugin = _pluginRegistry.GetPlugin("oura");
        if (plugin == null)
        {
            _logger.LogWarning("Oura plugin not found");
            return;
        }

        // Trigger sync for the affected date range
        var syncResult = await plugin.SyncAsync(new SyncOptions
        {
            StartDate = payload.StartDate,
            EndDate = payload.EndDate
        }, cancellationToken);

        // Publish event
        await _eventBus.PublishAsync(new MeasurementSyncedEvent
        {
            SourceName = "Oura",
            RecordCount = syncResult.RecordsSynced
        }, cancellationToken);
    }

    public Task ProcessFitbitWebhookAsync(FitbitWebhookPayload payload, CancellationToken cancellationToken = default)
    {
        // Similar implementation
        return Task.CompletedTask;
    }
}
```

---

## 7. Data Transformation Pipeline

### 7.1 Transformation Pipeline Interface

```csharp
// Application/Pipelines/IDataTransformationPipeline.cs
public interface IDataTransformationPipeline
{
    IDataTransformationPipeline AddStep(ITransformationStep step);
    Task<IEnumerable<Measurement>> ExecuteAsync(
        IEnumerable<NormalizedMeasurement> input,
        CancellationToken cancellationToken = default);
}

// Application/Pipelines/ITransformationStep.cs
public interface ITransformationStep
{
    Task<IEnumerable<NormalizedMeasurement>> TransformAsync(
        IEnumerable<NormalizedMeasurement> measurements,
        CancellationToken cancellationToken = default);
}
```

### 7.2 Pipeline Implementation

```csharp
// Application/Pipelines/DataTransformationPipeline.cs
public class DataTransformationPipeline : IDataTransformationPipeline
{
    private readonly List<ITransformationStep> _steps = new();
    private readonly ILogger<DataTransformationPipeline> _logger;

    public DataTransformationPipeline(ILogger<DataTransformationPipeline> logger)
    {
        _logger = logger;
    }

    public IDataTransformationPipeline AddStep(ITransformationStep step)
    {
        _steps.Add(step);
        return this;
    }

    public async Task<IEnumerable<Measurement>> ExecuteAsync(
        IEnumerable<NormalizedMeasurement> input,
        CancellationToken cancellationToken = default)
    {
        var current = input;

        foreach (var step in _steps)
        {
            current = await step.TransformAsync(current, cancellationToken);
        }

        // Convert to domain entities
        return current.Select(m => new Measurement
        {
            MetricTypeId = GetMetricTypeId(m.MetricType),
            Value = m.Value,
            Timestamp = m.Timestamp,
            RawDataJson = System.Text.Json.JsonSerializer.Serialize(m.Metadata)
        });
    }

    private int GetMetricTypeId(string metricType)
    {
        // Map metric type name to ID
        return 1;
    }
}
```

### 7.3 Transformation Step Examples

```csharp
// Application/Pipelines/Steps/UnitConversionStep.cs
public class UnitConversionStep : ITransformationStep
{
    public Task<IEnumerable<NormalizedMeasurement>> TransformAsync(
        IEnumerable<NormalizedMeasurement> measurements,
        CancellationToken cancellationToken = default)
    {
        var transformed = measurements.Select(m => m.Unit switch
        {
            "lbs" => m with { Value = m.Value * 0.453592m, Unit = "kg" }, // Convert lbs to kg
            "miles" => m with { Value = m.Value * 1.60934m, Unit = "km" }, // Convert miles to km
            _ => m
        });

        return Task.FromResult(transformed);
    }
}

// Application/Pipelines/Steps/ValidationStep.cs
public class ValidationStep : ITransformationStep
{
    private readonly ILogger<ValidationStep> _logger;

    public ValidationStep(ILogger<ValidationStep> logger)
    {
        _logger = logger;
    }

    public Task<IEnumerable<NormalizedMeasurement>> TransformAsync(
        IEnumerable<NormalizedMeasurement> measurements,
        CancellationToken cancellationToken = default)
    {
        var valid = measurements.Where(m =>
        {
            if (m.Value < 0)
            {
                _logger.LogWarning("Negative value detected for {MetricType}", m.MetricType);
                return false;
            }

            if (m.Timestamp > DateTime.UtcNow)
            {
                _logger.LogWarning("Future timestamp detected for {MetricType}", m.MetricType);
                return false;
            }

            return true;
        });

        return Task.FromResult(valid);
    }
}

// Application/Pipelines/Steps/DeduplicationStep.cs
public class DeduplicationStep : ITransformationStep
{
    public Task<IEnumerable<NormalizedMeasurement>> TransformAsync(
        IEnumerable<NormalizedMeasurement> measurements,
        CancellationToken cancellationToken = default)
    {
        var deduplicated = measurements
            .GroupBy(m => new { m.MetricType, m.Timestamp })
            .Select(g => g.First()); // Take first occurrence

        return Task.FromResult(deduplicated);
    }
}
```

---

## 8. Extension Points for Custom Metrics

### 8.1 Custom Metric Calculator Interface

```csharp
// Application/Extensions/ICustomMetricCalculator.cs
public interface ICustomMetricCalculator
{
    string MetricName { get; }
    string Unit { get; }
    string Category { get; }

    Task<decimal?> CalculateAsync(
        IEnumerable<Measurement> sourceMeasurements,
        DateTime date,
        CancellationToken cancellationToken = default);
}
```

### 8.2 Example: BMI Calculator

```csharp
// Application/Extensions/Calculators/BmiCalculator.cs
public class BmiCalculator : ICustomMetricCalculator
{
    public string MetricName => "bmi";
    public string Unit => "kg/m²";
    public string Category => "Body";

    public Task<decimal?> CalculateAsync(
        IEnumerable<Measurement> sourceMeasurements,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var weight = sourceMeasurements
            .Where(m => m.MetricType.Name == "weight" && m.Timestamp.Date == date.Date)
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefault();

        var height = sourceMeasurements
            .Where(m => m.MetricType.Name == "height" && m.Timestamp.Date == date.Date)
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefault();

        if (weight == null || height == null || height.Value == 0)
        {
            return Task.FromResult<decimal?>(null);
        }

        var bmi = weight.Value / (height.Value * height.Value);
        return Task.FromResult<decimal?>(Math.Round(bmi, 2));
    }
}
```

### 8.3 Custom Metric Registry

```csharp
// Application/Extensions/CustomMetricRegistry.cs
public interface ICustomMetricRegistry
{
    void RegisterCalculator(ICustomMetricCalculator calculator);
    ICustomMetricCalculator? GetCalculator(string metricName);
    IEnumerable<ICustomMetricCalculator> GetAllCalculators();
}

public class CustomMetricRegistry : ICustomMetricRegistry
{
    private readonly Dictionary<string, ICustomMetricCalculator> _calculators = new();

    public void RegisterCalculator(ICustomMetricCalculator calculator)
    {
        _calculators[calculator.MetricName] = calculator;
    }

    public ICustomMetricCalculator? GetCalculator(string metricName)
    {
        return _calculators.TryGetValue(metricName, out var calculator) ? calculator : null;
    }

    public IEnumerable<ICustomMetricCalculator> GetAllCalculators()
    {
        return _calculators.Values;
    }
}
```

---

---

## 9. Gray Areas / Questions

### 9.1 External API Authentication (Future Consideration)

**Note:** OAuth 2.0 and external API authentication will be implemented later when needed. For initial implementation, focus on simple API key-based authentication stored securely in Azure Key Vault.

### 9.2 API Quotas and Rate Limiting

**Question:** How should API quotas be managed across multiple data sources?

**Options:**
- **Option 1:** Hard-coded limits per provider
- **Option 2:** Dynamic quota tracking with database
- **Option 3:** Third-party rate limiting service

**Recommendation:** Option 2 (database tracking) with alerts.

### 9.3 Data Retention Policies

**Question:** How long should raw measurement data be retained?

**Options:**
- **Option 1:** 90 days (free tier constraint)
- **Option 2:** 1 year with archival to Blob Storage
- **Option 3:** Indefinite retention

**Recommendation:** Option 2 (1 year with archival).

### 9.4 Plugin Security

**Question:** Should third-party plugins be sandboxed?

**Options:**
- **Option 1:** No sandboxing (trust all plugins)
- **Option 2:** AppDomain isolation (limited in .NET Core)
- **Option 3:** Separate process execution

**Recommendation:** Option 1 initially, Option 3 for production with untrusted plugins.

### 9.5 Event Delivery Guarantees

**Question:** What delivery guarantees are needed for events?

**Options:**
- **Option 1:** At-most-once (fire and forget)
- **Option 2:** At-least-once (with retries)
- **Option 3:** Exactly-once (with deduplication)

**Recommendation:** Option 2 (at-least-once) with idempotent handlers.

---

## 10. Implementation Checklist

- [ ] Define IDataSourcePlugin interface
- [ ] Create PluginRegistry for plugin management
- [ ] Implement plugin loader (assembly + directory)
- [ ] Create adapter pattern for external APIs
- [ ] Implement data transformation pipeline
- [ ] Define domain events (MeasurementSynced, SyncFailed)
- [ ] Create in-memory event bus
- [ ] Implement event handlers
- [ ] Set up API versioning (v1, v2)
- [ ] Create webhook endpoints
- [ ] Implement webhook processor
- [ ] Create custom metric calculator interface
- [ ] Implement example calculators (BMI, etc.)
- [ ] Store API keys securely in Key Vault
- [ ] Create API quota tracking
- [ ] Implement rate limiting middleware
- [ ] Document plugin development guide
- [ ] Test plugin loading and execution
- [ ] Monitor event processing with Application Insights

---

## 11. References

### Microsoft Documentation

- [Plugin Architecture in .NET](https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support)
- [Azure Service Bus](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-overview)
- [API Versioning Best Practices](https://learn.microsoft.com/en-us/azure/architecture/best-practices/api-design#versioning-a-restful-web-api)
- [OAuth 2.0 in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/)
- [Azure Key Vault](https://learn.microsoft.com/en-us/azure/key-vault/general/overview)

### Best Practices

- Use plugin architecture for extensibility
- Implement adapter pattern for heterogeneous APIs
- Use event-driven architecture for decoupling
- Version APIs for backward compatibility
- Support webhooks for real-time updates
- Create transformation pipelines for data normalization
- Provide extension points for custom logic
- Secure OAuth tokens with Key Vault
- Track API quotas to prevent overages
- Use idempotent event handlers
