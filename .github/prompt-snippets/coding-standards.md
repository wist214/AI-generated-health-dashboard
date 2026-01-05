# Coding Standards

## C# Standards

- **Zero Warnings**: Enable nullable reference types, treat warnings as errors
- **Nullable Reference Types**: Properly handle null with `?` and null-conditional operators
- **Implicit Usings**: Enabled for cleaner code
- **Target Framework**: .NET 8.0

## JSON Serialization (Critical)

### Naming Policy Strategy
```csharp
// For external APIs (Oura) - deserialize snake_case
JsonNamingPolicy.SnakeCaseLower

// For file storage and frontend - serialize/deserialize camelCase
JsonNamingPolicy.CamelCase
```

### Rules
- **NEVER** use `[JsonPropertyName]` attributes on domain models
- Let naming policies handle all conversions automatically
- Use `PropertyNameCaseInsensitive = true` for flexible deserialization

### Why This Matters
```csharp
// ❌ BAD - This breaks the naming policy chain
public class SleepRecord
{
    [JsonPropertyName("total_sleep_duration")]
    public int? TotalSleepDuration { get; set; }
}

// ✅ GOOD - Let naming policy handle it
public class SleepRecord
{
    public int? TotalSleepDuration { get; set; }
}
```

## Clean Architecture

### Layer Dependencies
```
Api → Core ← Infrastructure
```

- **Api/**: Endpoints, response DTOs, HTTP concerns
- **Core/**: Business logic, domain models, interfaces
- **Infrastructure/**: External APIs, file persistence

### Interface Segregation
- Define interfaces in `Core/Interfaces/`
- Implement in `Infrastructure/` or `Core/Services/`
- Register in `Program.cs` via dependency injection

## ASP.NET Core Patterns

### Minimal API Endpoints
```csharp
group.MapGet("/data", async ([FromServices] IDataService service) =>
{
    var data = await service.GetDataAsync();
    return Results.Ok(data);
});
```

### Error Handling
- Return appropriate HTTP status codes
- Wrap external API calls in try-catch
- Log errors with `ILogger<T>`

## File Repository Pattern

```csharp
public class FileDataRepository<T> : IDataRepository<T>
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
}
```

## Frontend Compatibility

JavaScript expects camelCase:
```javascript
// ✅ Works
data.totalSleepDuration
data.averageHeartRate

// ❌ Breaks
data.total_sleep_duration
data.average_heart_rate
```

## Performance Guidelines

- Use `async/await` for all I/O operations
- Implement `SemaphoreSlim` for file access thread safety
- Cache expensive computations when appropriate

## Logging

```csharp
private readonly ILogger<MyService> _logger;

_logger.LogInformation("Syncing data from {Source}", "Oura");
_logger.LogError(ex, "Failed to sync data");
```
