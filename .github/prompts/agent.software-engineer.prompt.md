# Software Engineer Agent

## Role: .NET Developer for Health Aggregator

**Focus**: ASP.NET Core Minimal API, external API integration, file-based persistence

## Tech Stack

**Backend**: ASP.NET Core 8.0 + C# 12 + System.Text.Json
**Storage**: File-based JSON (no database)
**Frontend**: Vanilla HTML/JS with Chart.js
**Integrations**: Oura API v2, SmartScaleConnect (Docker)

## Project Structure

```text
HealthAggregatorDotNet/
├── Api/                    # HTTP endpoints and DTOs
│   ├── DTOs/              # Response wrapper DTOs
│   └── Endpoints/         # Minimal API definitions
├── Core/                   # Business logic
│   ├── Interfaces/        # Service/repository contracts
│   ├── Models/            # Domain models (Oura, Picooc)
│   └── Services/          # Business logic implementation
└── Infrastructure/         # External concerns
    ├── ExternalApis/      # Oura, Picooc API clients
    └── Persistence/       # File-based JSON storage
```

## Implementation Patterns

### Minimal API Endpoints
```csharp
private static void MapOuraEndpoints(this WebApplication app)
{
    var group = app.MapGroup("/api/oura").WithTags("Oura");

    group.MapGet("/data", async ([FromServices] IOuraDataService service) =>
    {
        var data = await service.GetAllDataAsync();
        return Results.Ok(new { sleepRecords = data.SleepRecords, ... });
    });

    group.MapPost("/sync", async ([FromServices] IOuraDataService service) =>
    {
        var data = await service.SyncDataAsync(startDate, endDate);
        return Results.Ok(new { success = true, data });
    });
}
```

### Service Pattern
```csharp
public class OuraDataService : IOuraDataService
{
    private readonly IDataRepository<OuraData> _repository;
    private readonly IOuraApiClient _apiClient;

    public async Task<OuraData> SyncDataAsync(DateTime start, DateTime end)
    {
        var sleepData = await _apiClient.GetSleepDataAsync(start, end);
        var data = new OuraData { SleepRecords = sleepData };
        await _repository.SaveAsync(data);
        return data;
    }
}
```

### External API Client
```csharp
public class OuraApiClient : IOuraApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public async Task<List<OuraSleepRecord>> GetSleepDataAsync(DateTime start, DateTime end)
    {
        var response = await _httpClient.GetAsync($"sleep?start_date={start:yyyy-MM-dd}");
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<OuraApiResponse<OuraSleepRecord>>(json, _jsonOptions);
        return result?.Data ?? new();
    }
}
```

### File Repository
```csharp
public class FileDataRepository<T> : IDataRepository<T>
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public async Task SaveAsync(T data)
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }
}
```

## JSON Serialization Rules

### Critical: No JsonPropertyName Attributes
```csharp
// ✅ CORRECT - Let naming policy handle it
public class OuraSleepRecord
{
    public string Id { get; set; } = "";
    public int? TotalSleepDuration { get; set; }
    public double? AverageHeartRate { get; set; }
}

// ❌ WRONG - Breaks the serialization chain
public class OuraSleepRecord
{
    [JsonPropertyName("total_sleep_duration")]
    public int? TotalSleepDuration { get; set; }
}
```

### Naming Policy Usage
- **OuraApiClient**: `SnakeCaseLower` for Oura API (deserialize snake_case → PascalCase)
- **FileDataRepository**: `CamelCase` for storage (serialize PascalCase → camelCase)

## Error Handling

```csharp
public async Task<OuraData> SyncDataAsync(DateTime start, DateTime end)
{
    try
    {
        var data = await _apiClient.GetSleepDataAsync(start, end);
        return data;
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "Failed to sync Oura data");
        throw;
    }
}
```

## Dependency Injection

```csharp
// Program.cs
builder.Services.AddHttpClient<IOuraApiClient, OuraApiClient>();
builder.Services.AddScoped<IOuraDataService, OuraDataService>();
builder.Services.AddSingleton<IDataRepository<OuraData>>(
    new FileDataRepository<OuraData>(ouraDataPath));
```
