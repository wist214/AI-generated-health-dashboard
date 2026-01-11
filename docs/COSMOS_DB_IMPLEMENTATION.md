# Cosmos DB Migration - Implementation Guide

## Overview

This document describes the Cosmos DB implementation for Health Aggregator, following enterprise-grade patterns and best practices.

## Architecture Quality Features

### ✅ SOLID Principles

1. **Single Responsibility Principle (SRP)**
   - `CosmosDbOptions`: Configuration only
   - `ICosmosDbClientFactory`: Client creation and management
   - `CosmosDbRepository<T>`: Data access only
   - Each class has one reason to change

2. **Open/Closed Principle (OCP)**
   - `IDataRepository<T>` interface allows different implementations
   - Can swap Blob Storage ↔ Cosmos DB without changing services
   - New storage providers can be added without modifying existing code

3. **Liskov Substitution Principle (LSP)**
   - `CosmosDbRepository<T>` fully implements `IDataRepository<T>`
   - Can replace `BlobDataRepository<T>` seamlessly
   - All services work with any `IDataRepository<T>` implementation

4. **Interface Segregation Principle (ISP)**
   - `IDataRepository<T>` has minimal, focused contract
   - `ICosmosDbClientFactory` separates factory concerns
   - No client is forced to depend on methods it doesn't use

5. **Dependency Inversion Principle (DIP)**
   - Services depend on `IDataRepository<T>`, not concrete implementations
   - Factory pattern with `ICosmosDbClientFactory` interface
   - High-level modules independent of low-level modules

### ✅ Design Patterns

1. **Factory Pattern**
   - `CosmosDbClientFactory` encapsulates complex client creation
   - Thread-safe singleton with lazy initialization
   - Proper resource management with IDisposable

2. **Repository Pattern**
   - `CosmosDbRepository<T>` abstracts data access
   - Consistent interface for all data entities
   - Testable and mockable

3. **Options Pattern**
   - `CosmosDbOptions` with data annotations validation
   - Strongly-typed configuration binding
   - Validated on startup for fail-fast behavior

4. **Dependency Injection**
   - All dependencies injected via constructor
   - Proper lifetime management (Singleton for repositories)
   - Loose coupling between components

### ✅ Best Practices

1. **Logging & Telemetry**
   - Comprehensive structured logging with `ILogger<T>`
   - Request charges (RU) tracked for cost monitoring
   - Latency measurements with stopwatch
   - Activity IDs for distributed tracing

2. **Error Handling**
   - Specific catch blocks for different exception types
   - Rate limiting (429) handling with retry after
   - Document size validation
   - Not found scenarios handled gracefully

3. **Performance**
   - Direct connection mode for best latency
   - CamelCase serialization for frontend compatibility
   - Session consistency level (optimal for single-user)
   - Lazy initialization to avoid startup delays

4. **Security**
   - Connection strings in configuration, not code
   - Emulator SSL validation bypass only when needed
   - No credentials in logs

5. **Testability**
   - Interfaces for all external dependencies
   - Configuration via Options pattern
   - Mockable factory and repository

## File Structure

```
HealthAggregatorApi/
├── Infrastructure/
│   ├── Configuration/
│   │   └── CosmosDbOptions.cs              # Validated configuration
│   └── Persistence/
│       ├── ICosmosDbClientFactory.cs       # Factory interface
│       ├── CosmosDbClientFactory.cs        # Thread-safe factory
│       ├── CosmosDbRepository.cs           # Generic repository
│       └── BlobDataRepository.cs           # Fallback implementation
├── Program.cs                               # DI registration & startup
└── local.settings.json                      # Development configuration
```

## Configuration

### Development (Emulator)

```json
{
  "CosmosDb:Enabled": "true",
  "CosmosDb:UseEmulator": "true",
  "CosmosDb:DatabaseName": "HealthAggregator"
}
```

### Production (Azure)

```json
{
  "CosmosDb:Enabled": "true",
  "CosmosDb:UseEmulator": "false",
  "CosmosDb:ConnectionString": "AccountEndpoint=https://...;AccountKey=...",
  "CosmosDb:DatabaseName": "HealthAggregator"
}
```

## Usage

### Switching Between Blob and Cosmos DB

Simply toggle `CosmosDb:Enabled`:

```json
{
  "CosmosDb:Enabled": "false"  // Use Blob Storage
}
```

```json
{
  "CosmosDb:Enabled": "true"   // Use Cosmos DB
}
```

No code changes required! The DI container handles the switch.

### Adding New Data Entities

1. Create model class: `MyNewData.cs`
2. Add container name to `CosmosDbOptions`:
   ```csharp
   public string MyNewDataContainerName { get; set; } = "MyNewData";
   ```
3. Register in `Program.cs`:
   ```csharp
   builder.Services.AddSingleton<IDataRepository<MyNewData>>(sp => {
       var factory = sp.GetRequiredService<ICosmosDbClientFactory>();
       var options = sp.GetRequiredService<IOptions<CosmosDbOptions>>();
       var logger = sp.GetRequiredService<ILogger<CosmosDbRepository<MyNewData>>>();
       return new CosmosDbRepository<MyNewData>(factory, options, 
           options.Value.MyNewDataContainerName, logger);
   });
   ```

## Testing Locally

### 1. Start Cosmos DB Emulator

**Option A: Docker**
```powershell
docker run -d --name azure-cosmosdb-emulator `
    -p 8081:8081 -p 10250-10255:10250-10255 `
    -e AZURE_COSMOS_EMULATOR_PARTITION_COUNT=10 `
    -e AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=true `
    mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
```

**Option B: Windows Installer**
Download from: https://aka.ms/cosmosdb-emulator

### 2. Configure Application

Ensure `local.settings.json` has:
```json
{
  "CosmosDb:Enabled": "true",
  "CosmosDb:UseEmulator": "true"
}
```

### 3. Run Application

```powershell
cd HealthAggregatorApi
func start
```

### 4. Verify Database Creation

The application automatically creates:
- Database: `HealthAggregator`
- Containers: `OuraData`, `PicoocData`, `CronometerData`, `UserSettings`

Check logs for:
```
Initializing Cosmos DB database and containers...
Created database HealthAggregator
Created container OuraData with partition key /userId
...
Cosmos DB initialization completed successfully
```

### 5. Test API Endpoints

```powershell
# Sync data
Invoke-RestMethod -Method POST http://localhost:7071/api/oura/sync
Invoke-RestMethod -Method POST http://localhost:7071/api/picooc/sync

# Verify data stored
Invoke-RestMethod http://localhost:7071/api/oura/data
Invoke-RestMethod http://localhost:7071/api/dashboard/metrics
```

### 6. Monitor Performance

Check logs for request charges:
```
Successfully saved OuraData. Request charge: 10.52 RU, Latency: 23ms
Successfully retrieved PicoocDataCache. Request charge: 1.0 RU, Latency: 5ms
```

## Performance Metrics

### Expected Latencies (Local Emulator)

| Operation | Latency | RU Cost |
|-----------|---------|---------|
| Point Read | 5-15ms | 1 RU |
| Upsert (small) | 10-25ms | 5-10 RU |
| Upsert (large) | 20-50ms | 10-20 RU |

### Expected Latencies (Azure - Same Region)

| Operation | Latency | RU Cost |
|-----------|---------|---------|
| Point Read | 5-10ms | 1 RU |
| Upsert (small) | 10-20ms | 5-10 RU |
| Upsert (large) | 15-30ms | 10-20 RU |

## Cost Monitoring

### Log Analysis

Search for "Request charge" in logs:
```csharp
_logger.LogInformation(
    "Successfully saved {EntityType}. Request charge: {RequestCharge} RU",
    typeof(T).Name,
    response.RequestCharge);
```

### Monthly Cost Estimation

Based on current usage patterns:

| Data Type | Operations/Month | RU/Operation | Total RU | Cost |
|-----------|------------------|--------------|----------|------|
| Oura Sync | 730 writes | 10 RU | 7,300 | $0.002 |
| Picooc Sync | 730 writes | 5 RU | 3,650 | $0.001 |
| Dashboard Reads | 10,000 reads | 1 RU | 10,000 | $0.003 |
| **Total** | | | **21,000 RU** | **~$0.005/month** |

**Storage**: 100MB × $0.25/GB = $0.025/month

**Grand Total**: ~$0.03/month (with overhead: **~$1-2/month**)

## Troubleshooting

### Issue: Connection Refused

```
CosmosException: Unable to connect to https://localhost:8081
```

**Solution**: Ensure emulator is running:
```powershell
docker ps | Select-String "azure-cosmosdb-emulator"
```

### Issue: SSL Certificate Error

```
The SSL connection could not be established
```

**Solution**: Set `UseEmulator = true` to bypass SSL validation:
```json
{
  "CosmosDb:UseEmulator": "true"
}
```

### Issue: Rate Limiting (429 errors)

```
CosmosException: Rate limit exceeded. StatusCode: 429
```

**Solution**: SDK automatically retries. Configure in `CosmosDbOptions`:
```json
{
  "CosmosDb:MaxRetryAttempts": "5",
  "CosmosDb:MaxRetryWaitTimeSeconds": "60"
}
```

### Issue: Document Too Large

```
CosmosException: Request entity too large. StatusCode: 413
```

**Solution**: Cosmos DB has 2MB document limit. Archive old data or split into multiple documents.

## Migration from Blob Storage

See [DATABASE_MIGRATION_PLAN.md](../docs/DATABASE_MIGRATION_PLAN.md) for complete migration strategy.

### Quick Migration

1. **Backup existing data**:
   ```powershell
   $date = Get-Date -Format "yyyyMMdd"
   az storage blob download-batch `
       --source health-data `
       --destination ./backup-$date `
       --connection-string "UseDevelopmentStorage=true"
   ```

2. **Enable Cosmos DB**:
   ```json
   { "CosmosDb:Enabled": "true" }
   ```

3. **Sync fresh data**:
   ```powershell
   Invoke-RestMethod -Method POST http://localhost:7071/api/oura/sync
   Invoke-RestMethod -Method POST http://localhost:7071/api/picooc/sync
   Invoke-RestMethod -Method POST http://localhost:7071/api/cronometer/sync
   ```

4. **Verify**: Check dashboard shows data

## Code Quality Checklist

✅ **SOLID Principles**: All 5 principles applied  
✅ **Design Patterns**: Factory, Repository, Options  
✅ **Error Handling**: Comprehensive exception handling  
✅ **Logging**: Structured logging with metrics  
✅ **Performance**: Optimized connection settings  
✅ **Security**: No hardcoded credentials  
✅ **Testability**: All dependencies injected  
✅ **Documentation**: Comprehensive inline docs  
✅ **Configuration**: Validated options pattern  
✅ **Resource Management**: Proper disposal  

## Next Steps

1. ✅ Implementation complete
2. ⏳ Local testing with emulator
3. ⏳ Integration tests
4. ⏳ Production deployment
5. ⏳ Monitor performance and costs

## Support

For issues or questions:
1. Check logs for detailed error messages
2. Verify configuration in `local.settings.json`
3. Ensure emulator is running (if local)
4. Review [DATABASE_MIGRATION_PLAN.md](../docs/DATABASE_MIGRATION_PLAN.md)
