# Database Migration Plan - From Blob Storage to Azure Database

## Executive Summary

**Current State:** JSON files in Azure Blob Storage  
**Target State:** Azure Cosmos DB for NoSQL (serverless mode)  
**Migration Strategy:** Phased rollout with dual-write capability  
**Timeline:** 2-3 weeks  
**Cost Impact:** ~$1-5/month (from $0.01/month)

---

## 1. Database Selection Analysis

### Option A: Azure SQL Database (Free Tier) ❌ **NOT RECOMMENDED**

**Pros:**
- Familiar SQL queries
- Strong consistency
- Free tier available (32GB storage)

**Cons:**
- ❌ **Deal Breaker:** Free tier requires Azure for Students or Visual Studio subscription
- Complex schema with 15+ tables needed
- Rigid schema changes require migrations
- Overkill for single-user application
- Higher query complexity for nested data

### Option B: Azure Cosmos DB for NoSQL ✅ **RECOMMENDED**

**Pros:**
- ✅ **Perfect fit** for JSON document storage
- ✅ Serverless mode: pay only for what you use (~$1-5/month)
- ✅ Minimal code changes (documents look like current JSON)
- ✅ No schema management overhead
- ✅ Flexible schema evolution
- ✅ Built-in partitioning & scaling
- ✅ Multi-region replication available

**Cons:**
- Higher per-operation cost than SQL (mitigated by low volume)
- Eventually consistent by default (not an issue for this use case)

**Cost Estimate:**
- Serverless: $0.25 per million RU (Request Unit)
- ~10,000 operations/month = ~$0.50/month
- Storage: $0.25/GB/month × 0.1GB = $0.025/month
- **Total: ~$1-5/month**

### Option C: Azure Table Storage ⚠️ **ALTERNATIVE**

**Pros:**
- Cheapest option ($0.045/GB/month)
- Good for key-value lookups

**Cons:**
- Limited query capabilities
- 1MB per entity limit
- No document structure support
- Would require significant data restructuring

---

## 2. Recommended Architecture: Azure Cosmos DB for NoSQL

### Database Structure

```
Database: HealthAggregator
├── Container: OuraData
│   ├── Partition Key: /userId (future multi-user support)
│   └── Documents: One per user
├── Container: PicoocData
│   ├── Partition Key: /userId
│   └── Documents: One per user
├── Container: CronometerData
│   ├── Partition Key: /userId
│   └── Documents: One per user
└── Container: UserSettings
    ├── Partition Key: /userId
    └── Documents: One per user
```

### Document Schema (No Changes Needed!)

**OuraData Document:**
```json
{
  "id": "user_default",
  "userId": "default",
  "sleepRecords": [...],
  "dailySleep": [...],
  "readiness": [...],
  "activity": [...],
  "dailyStress": [...],
  "vo2Max": [...],
  "workouts": [...],
  "lastSync": "2026-01-11T10:00:00Z",
  "_etag": "...",
  "_ts": 1736596800
}
```

**PicoocData Document:**
```json
{
  "id": "user_default",
  "userId": "default",
  "measurements": [
    {
      "date": "2026-01-11T08:00:00Z",
      "weight": 75.5,
      "bmi": 24.2,
      "bodyFat": 18.5,
      ...
    }
  ],
  "lastSync": "2026-01-11T10:00:00Z"
}
```

---

## 3. Migration Implementation Plan

### Phase 1: Setup & Infrastructure (Week 1)

#### 1.1 Create Azure Resources
```bash
# Create Cosmos DB account (serverless mode)
az cosmosdb create \
  --name health-aggregator-db \
  --resource-group health-aggregator-rg \
  --locations regionName=westeurope \
  --capabilities EnableServerless \
  --default-consistency-level Session

# Create database
az cosmosdb sql database create \
  --account-name health-aggregator-db \
  --resource-group health-aggregator-rg \
  --name HealthAggregator

# Create containers
az cosmosdb sql container create \
  --account-name health-aggregator-db \
  --resource-group health-aggregator-rg \
  --database-name HealthAggregator \
  --name OuraData \
  --partition-key-path "/userId"

# Repeat for PicoocData, CronometerData, UserSettings
```

#### 1.2 Add NuGet Packages
```xml
<PackageReference Include="Microsoft.Azure.Cosmos" Version="3.41.0" />
```

#### 1.3 Update Configuration
```json
// local.settings.json
{
  "CosmosDb": {
    "ConnectionString": "AccountEndpoint=https://...;AccountKey=...",
    "DatabaseName": "HealthAggregator"
  }
}
```

### Phase 2: Code Implementation (Week 1-2)

#### 2.1 Create CosmosDB Repository
**New file:** `Infrastructure/Persistence/CosmosDbRepository.cs`

```csharp
using Microsoft.Azure.Cosmos;
using System.Text.Json;
using HealthAggregatorApi.Core.Interfaces;

public class CosmosDbRepository<T> : IDataRepository<T> where T : class
{
    private readonly Container _container;
    private readonly string _partitionKeyValue;
    private readonly JsonSerializerOptions _jsonOptions;

    public CosmosDbRepository(
        CosmosClient client, 
        string databaseName, 
        string containerName,
        string partitionKeyValue = "default")
    {
        _container = client.GetContainer(databaseName, containerName);
        _partitionKeyValue = partitionKeyValue;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<T?> GetAsync()
    {
        try
        {
            var response = await _container.ReadItemAsync<T>(
                id: $"user_{_partitionKeyValue}",
                partitionKey: new PartitionKey(_partitionKeyValue)
            );
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Return empty instance if document doesn't exist
            return Activator.CreateInstance<T>();
        }
    }

    public async Task SaveAsync(T data)
    {
        // Ensure document has id field
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var document = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        document!["id"] = $"user_{_partitionKeyValue}";
        document["userId"] = _partitionKeyValue;

        await _container.UpsertItemAsync(
            item: document,
            partitionKey: new PartitionKey(_partitionKeyValue)
        );
    }
}
```

#### 2.2 Update DI Configuration in Program.cs

```csharp
// Register Cosmos DB Client
var cosmosConnectionString = configuration["CosmosDb:ConnectionString"];
var cosmosDatabaseName = configuration["CosmosDb:DatabaseName"] ?? "HealthAggregator";

builder.Services.AddSingleton<CosmosClient>(sp =>
{
    var options = new CosmosClientOptions
    {
        SerializerOptions = new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        }
    };
    return new CosmosClient(cosmosConnectionString, options);
});

// Register repositories with Cosmos DB
builder.Services.AddSingleton<IDataRepository<OuraData>>(sp =>
{
    var client = sp.GetRequiredService<CosmosClient>();
    return new CosmosDbRepository<OuraData>(client, cosmosDatabaseName, "OuraData");
});

builder.Services.AddSingleton<IDataRepository<PicoocDataCache>>(sp =>
{
    var client = sp.GetRequiredService<CosmosClient>();
    return new CosmosDbRepository<PicoocDataCache>(client, cosmosDatabaseName, "PicoocData");
});

builder.Services.AddSingleton<IDataRepository<CronometerData>>(sp =>
{
    var client = sp.GetRequiredService<CosmosClient>();
    return new CosmosDbRepository<CronometerData>(client, cosmosDatabaseName, "CronometerData");
});

builder.Services.AddSingleton<IDataRepository<UserSettings>>(sp =>
{
    var client = sp.GetRequiredService<CosmosClient>();
    return new CosmosDbRepository<UserSettings>(client, cosmosDatabaseName, "UserSettings");
});
```

### Phase 3: Data Migration (Week 2)

#### 3.1 Create Migration Script
**New file:** `Scripts/MigrateFromBlobToCosmosDb.ps1`

```powershell
# Migration script to copy data from Blob Storage to Cosmos DB

param(
    [string]$BlobConnectionString,
    [string]$CosmosConnectionString,
    [string]$DatabaseName = "HealthAggregator"
)

# Download JSON files from blob storage
Write-Host "Downloading data from Blob Storage..."
$tempDir = "$env:TEMP\health-data-migration"
New-Item -ItemType Directory -Force -Path $tempDir

# Use Azure CLI or Azure Storage SDK to download
az storage blob download --connection-string $BlobConnectionString `
    --container-name health-data `
    --name oura_data.json `
    --file "$tempDir\oura_data.json"

az storage blob download --connection-string $BlobConnectionString `
    --container-name health-data `
    --name picooc_data.json `
    --file "$tempDir\picooc_data.json"

az storage blob download --connection-string $BlobConnectionString `
    --container-name health-data `
    --name cronometer_data.json `
    --file "$tempDir\cronometer_data.json"

# Upload to Cosmos DB using Data Migration Tool or custom script
Write-Host "Uploading data to Cosmos DB..."

# Option 1: Use Azure Data Factory
# Option 2: Use custom .NET migration tool
# Option 3: Manual import via Azure Portal
```

#### 3.2 Create .NET Migration Tool
**New project:** `HealthAggregatorApi.Migration/Program.cs`

```csharp
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using System.Text.Json;

var blobConnectionString = args[0];
var cosmosConnectionString = args[1];

// Download from Blob
var blobClient = new BlobServiceClient(blobConnectionString);
var container = blobClient.GetBlobContainerClient("health-data");

var ouraBlob = container.GetBlobClient("oura_data.json");
var ouraJson = await ouraBlob.DownloadContentAsync();
var ouraData = JsonSerializer.Deserialize<OuraData>(ouraJson.Value.Content.ToString());

// Upload to Cosmos DB
var cosmosClient = new CosmosClient(cosmosConnectionString);
var database = cosmosClient.GetDatabase("HealthAggregator");
var ouraContainer = database.GetContainer("OuraData");

var document = new
{
    id = "user_default",
    userId = "default",
    sleepRecords = ouraData.SleepRecords,
    dailySleep = ouraData.DailySleep,
    readiness = ouraData.Readiness,
    activity = ouraData.Activity,
    // ... map all fields
    lastSync = ouraData.LastSync
};

await ouraContainer.UpsertItemAsync(document, new PartitionKey("default"));

Console.WriteLine("Migration complete!");
```

### Phase 4: Testing & Validation (Week 2-3)

#### 4.1 Local Testing Checklist
- [ ] Run Cosmos DB Emulator locally
- [ ] Test sync operations (Oura, Picooc, Cronometer)
- [ ] Verify data retrieval endpoints
- [ ] Test dashboard aggregation
- [ ] Validate timer function
- [ ] Check error handling

#### 4.2 Azure Testing Checklist
- [ ] Deploy with Cosmos DB settings
- [ ] Run manual sync from production
- [ ] Compare data integrity with blob backup
- [ ] Monitor query performance
- [ ] Check RU consumption
- [ ] Validate auto-sync timer

### Phase 5: Deployment & Cutover (Week 3)

#### 5.1 Pre-Deployment
1. **Backup existing blob data**
   ```bash
   az storage blob download-batch \
     --source health-data \
     --destination ./backup-$(date +%Y%m%d) \
     --connection-string $BLOB_CONNECTION
   ```

2. **Run migration script** to populate Cosmos DB with existing data

3. **Test in staging environment** with production data copy

#### 5.2 Deployment Steps
1. Update GitHub Secrets with Cosmos DB connection string
2. Deploy updated Functions code
3. Verify all endpoints work
4. Monitor for 24 hours

#### 5.3 Post-Deployment
1. Keep blob storage for 30 days as backup
2. Monitor Cosmos DB costs
3. Optimize queries if needed
4. Delete blob storage after validation period

---

## 4. Code Changes Summary

### Files to Create
- `Infrastructure/Persistence/CosmosDbRepository.cs`
- `Scripts/MigrateFromBlobToCosmosDb.ps1`
- `Migration/Program.cs` (optional dedicated migration tool)

### Files to Modify
- `HealthAggregatorApi.csproj` - Add Cosmos DB NuGet package
- `Program.cs` - Replace BlobDataRepository with CosmosDbRepository
- `local.settings.json` - Add Cosmos DB connection string
- `.github/workflows/azure-functions.yml` - No changes needed (uses app settings)

### Files to Keep (No Changes)
- All models in `Core/Models/` - Same structure
- All services in `Core/Services/` - Use same IDataRepository interface
- All API clients - No changes
- Frontend code - No changes (same JSON responses)

---

## 5. Rollback Plan

### If Migration Fails
1. Revert `Program.cs` to use `BlobDataRepository`
2. Redeploy previous version
3. Blob data remains untouched as fallback
4. Investigate Cosmos DB issues offline

### Keep Blob Storage Active During Transition
Implement dual-write for first 2 weeks:

```csharp
public class DualWriteRepository<T> : IDataRepository<T> where T : class
{
    private readonly IDataRepository<T> _cosmosRepo;
    private readonly IDataRepository<T> _blobRepo;
    private readonly ILogger _logger;

    public async Task SaveAsync(T data)
    {
        // Write to Cosmos DB first
        await _cosmosRepo.SaveAsync(data);
        
        // Write to Blob as backup
        try
        {
            await _blobRepo.SaveAsync(data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Blob backup write failed");
        }
    }

    public Task<T?> GetAsync() => _cosmosRepo.GetAsync();
}
```

---

## 6. Performance & Optimization

### Expected Performance
- **Read latency:** 5-10ms (vs 20-50ms blob)
- **Write latency:** 10-15ms (vs 50-100ms blob)
- **Throughput:** Serverless auto-scales

### Query Optimization
1. Use point reads (by id + partition key) - Most efficient
2. Create indexes for frequently queried fields
3. Use continuation tokens for large result sets
4. Implement caching for dashboard metrics

### Cost Optimization
1. Use serverless mode (no minimum charge)
2. Batch operations when possible
3. Set TTL on old data if needed
4. Monitor RU consumption with Application Insights

---

## 7. Future Enhancements (Post-Migration)

### Multi-User Support
- Change partition key strategy to real userId
- Add authentication (Azure AD B2C)
- Isolate data per user automatically

### Advanced Querying
- Cross-partition queries for analytics
- Date range queries for trends
- Aggregation pipeline for statistics

### Real-time Updates
- Cosmos DB Change Feed for live dashboard updates
- Azure SignalR integration
- No more polling needed

---

## 8. Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Data loss during migration | Low | Critical | Keep blob storage, backup before migration |
| Higher costs than expected | Medium | Low | Serverless mode, monitor usage, set alerts |
| Performance regression | Low | Medium | Test locally with emulator first |
| API compatibility issues | Low | Medium | Same IDataRepository interface |
| Cosmos DB outage | Low | Medium | Implement retry logic, cache responses |

---

## 9. Success Metrics

### Technical Metrics
- ✅ Zero data loss (100% migration accuracy)
- ✅ Improved read latency (<10ms avg)
- ✅ No frontend code changes required
- ✅ All tests passing

### Business Metrics
- ✅ Cost remains under $5/month
- ✅ Deployment completed within 3 weeks
- ✅ Zero downtime during migration
- ✅ Scalable for future multi-user expansion

---

## 10. Decision: Proceed with Cosmos DB

**Recommendation:** Migrate to Azure Cosmos DB for NoSQL in serverless mode

**Rationale:**
1. Perfect match for current JSON document structure
2. Minimal code changes (same models, same interface)
3. Better performance than blob storage
4. Future-proof for multi-user and advanced features
5. Cost-effective for low-volume personal use
6. No complex schema migrations needed

**Next Steps:**
1. Create Cosmos DB account in Azure Portal
2. Implement CosmosDbRepository class
3. Test locally with Cosmos DB Emulator
4. Run migration script with production backup
5. Deploy and monitor

---

## Appendix A: Local Development Setup

### Install Cosmos DB Emulator
```powershell
# Download from Microsoft
# Or use Docker
docker pull mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator

docker run -p 8081:8081 -p 10251:10251 -p 10252:10252 -p 10253:10253 -p 10254:10254 `
  --name azure-cosmosdb-emulator `
  -e AZURE_COSMOS_EMULATOR_PARTITION_COUNT=10 `
  -e AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=true `
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator
```

### Local Connection String
```
AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==
```

---

## Appendix B: Cost Comparison

| Scenario | Blob Storage | Cosmos DB (Serverless) |
|----------|--------------|------------------------|
| Storage (100MB) | $0.0018/month | $0.025/month |
| 10,000 reads/month | Included | ~$0.10 |
| 1,000 writes/month | Included | ~$0.40 |
| Data transfer | Free (same region) | Free (same region) |
| **Total** | **~$0.01/month** | **~$1-2/month** |

**Verdict:** 100x cost increase, but still minimal (<$2/month) for significantly better capabilities.
