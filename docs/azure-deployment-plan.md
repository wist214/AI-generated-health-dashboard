# Azure Deployment Plan: Health Aggregator

**Status:** ✅ Implementation Complete  
**Goal:** Deploy dashboard with automatic hourly sync  
**Azure Account:** wist214azure@gmail.com  
**Estimated Cost:** ~$0.01/month (blob storage only)

---

## ✅ Implementation Status

| Phase | Status | Details |
|-------|--------|---------|
| Phase 1: Code Preparation | ✅ Done | All files created and tested locally |
| Phase 2: Azure Resources | 🔲 Pending | Manual portal setup required |
| Phase 3: Deploy | 🔲 Pending | After Phase 2 |
| Phase 4: Verify | 🔲 Pending | After deployment |

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│        Azure Static Web Apps (Free Tier)            │
├─────────────────────────────────────────────────────┤
│  Static Files:     index.html, styles.css           │
│                                                     │
│  Managed Functions API (/api/*):                    │
│  ├── GET  /api/oura/data      → Get Oura data      │
│  ├── POST /api/oura/sync      → Sync Oura          │
│  ├── GET  /api/picooc/data    → Get Picooc data    │
│  ├── POST /api/picooc/sync    → Sync Picooc        │
│  ├── GET  /api/dashboard/*    → Dashboard endpoints │
│  └── TimerTrigger (hourly)    → Auto-sync both     │
├─────────────────────────────────────────────────────┤
│                    ↓ ↑                              │
├─────────────────────────────────────────────────────┤
│        Azure Blob Storage (~$0.01/month)            │
│  └── healthdata container                           │
│      ├── oura_data.json                             │
│      └── picooc_data.json                           │
└─────────────────────────────────────────────────────┘
```

---

## What We're NOT Doing (Overkill for this project)

| ❌ Skip | Why |
|---------|-----|
| Bicep/Terraform | Manual portal setup is fine for 3 resources |
| Separate Function App | SWA has built-in managed functions |
| Azure Key Vault | App Settings are sufficient for personal use |
| Azure Developer CLI (azd) | SWA CLI is simpler |
| Application Insights | Not needed for personal dashboard |
| VNET/Private endpoints | Public endpoints are fine |

---

## Prerequisites

- [ ] Azure account (wist214azure@gmail.com)
- [ ] Node.js installed (for SWA CLI)
- [ ] .NET 8 SDK installed

---

## Phase 1: Prepare the Code (Local)

### Step 1.1: Create Azure Functions API Project

```powershell
# Create new folder for Azure Functions
cd D:\Work\My\HealthAggregator
mkdir HealthAggregatorApi
cd HealthAggregatorApi

# Initialize .NET isolated Azure Functions project
func init --worker-runtime dotnet-isolated --target-framework net8.0
```

### Step 1.2: Add NuGet Packages

```powershell
dotnet add package Microsoft.Azure.Functions.Worker
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Http
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Timer
dotnet add package Azure.Storage.Blobs
dotnet add package Microsoft.Extensions.Http
```

### Step 1.3: Project Structure

```
HealthAggregatorApi/
├── HealthAggregatorApi.csproj
├── host.json
├── local.settings.json
├── Program.cs
├── Functions/
│   ├── SyncTimerFunction.cs      # Timer trigger (hourly)
│   ├── OuraFunctions.cs          # HTTP: /api/oura/*
│   ├── PicoocFunctions.cs        # HTTP: /api/picooc/*
│   └── DashboardFunctions.cs     # HTTP: /api/dashboard/*
├── Services/                      # Copy from existing project
│   ├── OuraDataService.cs
│   ├── PicoocDataService.cs
│   └── DashboardService.cs
├── Models/                        # Copy from existing project
│   ├── Oura/
│   └── Picooc/
└── Infrastructure/
    ├── BlobDataRepository.cs      # NEW: replaces FileDataRepository
    ├── OuraApiClient.cs           # Copy from existing
    └── PicoocApiClient.cs         # Copy from existing
```

### Step 1.4: Create BlobDataRepository

```csharp
// Infrastructure/BlobDataRepository.cs
using Azure.Storage.Blobs;
using System.Text.Json;

public class BlobDataRepository<T> : IDataRepository<T> where T : class, new()
{
    private readonly BlobContainerClient _container;
    private readonly string _blobName;
    private readonly JsonSerializerOptions _jsonOptions;

    public BlobDataRepository(string connectionString, string containerName, string blobName)
    {
        var client = new BlobServiceClient(connectionString);
        _container = client.GetBlobContainerClient(containerName);
        _container.CreateIfNotExists();
        _blobName = blobName;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<T?> GetAsync()
    {
        var blob = _container.GetBlobClient(_blobName);
        if (!await blob.ExistsAsync())
            return new T();

        var response = await blob.DownloadContentAsync();
        return JsonSerializer.Deserialize<T>(response.Value.Content, _jsonOptions);
    }

    public async Task SaveAsync(T data)
    {
        var blob = _container.GetBlobClient(_blobName);
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        await blob.UploadAsync(BinaryData.FromString(json), overwrite: true);
    }
}
```

### Step 1.5: Create Timer Sync Function

```csharp
// Functions/SyncTimerFunction.cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

public class SyncTimerFunction
{
    private readonly IOuraDataService _ouraService;
    private readonly IPicoocDataService _picoocService;
    private readonly ILogger<SyncTimerFunction> _logger;

    public SyncTimerFunction(
        IOuraDataService ouraService,
        IPicoocDataService picoocService,
        ILogger<SyncTimerFunction> logger)
    {
        _ouraService = ouraService;
        _picoocService = picoocService;
        _logger = logger;
    }

    [Function("SyncTimer")]
    public async Task Run([TimerTrigger("0 0 * * * *")] TimerInfo timer)
    {
        _logger.LogInformation("Hourly sync started at {time}", DateTime.UtcNow);

        try
        {
            // Sync Oura (last 7 days)
            var endDate = DateTime.UtcNow.AddDays(1);
            var startDate = DateTime.UtcNow.AddDays(-7);
            await _ouraService.SyncDataAsync(startDate, endDate);
            _logger.LogInformation("Oura sync completed");

            // Sync Picooc
            await _picoocService.SyncDataAsync();
            _logger.LogInformation("Picooc sync completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed");
        }
    }
}
```

### Step 1.6: Create HTTP Functions

```csharp
// Functions/OuraFunctions.cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class OuraFunctions
{
    private readonly IOuraDataService _service;

    public OuraFunctions(IOuraDataService service)
    {
        _service = service;
    }

    [Function("GetOuraData")]
    public async Task<HttpResponseData> GetData(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "oura/data")] HttpRequestData req)
    {
        var data = await _service.GetAllDataAsync();
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(new
        {
            sleepRecords = data.SleepRecords,
            dailySleep = data.DailySleep,
            readiness = data.Readiness,
            activity = data.Activity,
            personalInfo = data.PersonalInfo,
            lastSync = data.LastSync
        });
        return response;
    }

    [Function("SyncOuraData")]
    public async Task<HttpResponseData> Sync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "oura/sync")] HttpRequestData req)
    {
        var endDate = DateTime.UtcNow.AddDays(1);
        var startDate = DateTime.UtcNow.AddDays(-30);
        
        var data = await _service.SyncDataAsync(startDate, endDate);
        
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(new
        {
            success = true,
            message = "Sync completed",
            sleepCount = data.DailySleep.Count,
            lastSync = data.LastSync
        });
        return response;
    }
}
```

### Step 1.7: Configure local.settings.json

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "StorageConnectionString": "UseDevelopmentStorage=true",
    "Oura__AccessToken": "your-oura-token",
    "Picooc__Email": "your-email",
    "Picooc__Password": "your-password"
  }
}
```

### Step 1.8: Prepare Static Dashboard

```powershell
# Create dashboard folder for SWA
mkdir D:\Work\My\HealthAggregator\dashboard

# Copy static files
Copy-Item D:\Work\My\HealthAggregator\HealthAggregatorDotNet\wwwroot\* D:\Work\My\HealthAggregator\dashboard -Recurse
```

Update API URLs in `dashboard/index.html`:
```javascript
// Change from:
const API_BASE = 'http://localhost:3001';

// To (SWA handles routing automatically):
const API_BASE = '/api';
```

---

## Phase 2: Create Azure Resources (Portal)

### Step 2.1: Create Resource Group

1. Go to [Azure Portal](https://portal.azure.com)
2. Search "Resource groups" → Create
3. Settings:
   - **Subscription:** Your subscription
   - **Resource group:** `rg-healthaggregator`
   - **Region:** `West Europe` (or closest to you)

### Step 2.2: Create Storage Account

1. Search "Storage accounts" → Create
2. Settings:
   - **Resource group:** `rg-healthaggregator`
   - **Storage account name:** `sthealthaggregator` (must be globally unique)
   - **Region:** Same as resource group
   - **Performance:** Standard
   - **Redundancy:** LRS (cheapest)
3. After creation:
   - Go to "Containers" → Create container: `healthdata`
   - Go to "Access keys" → Copy connection string

### Step 2.3: Create Static Web App

1. Search "Static Web Apps" → Create
2. Settings:
   - **Resource group:** `rg-healthaggregator`
   - **Name:** `swa-healthaggregator`
   - **Plan type:** Free
   - **Region:** `West Europe`
   - **Deployment:** Other (we'll deploy via CLI)
3. After creation:
   - Go to "Configuration" → Add application settings:

| Name | Value |
|------|-------|
| `StorageConnectionString` | (paste from Step 2.2) |
| `Oura__AccessToken` | your-oura-token |
| `Picooc__Email` | your-email |
| `Picooc__Password` | your-password |

---

## Phase 3: Deploy (CLI)

### Step 3.1: Install SWA CLI

```powershell
npm install -g @azure/static-web-apps-cli
```

### Step 3.2: Test Locally

```powershell
cd D:\Work\My\HealthAggregator

# Start SWA with local functions
swa start ./dashboard --api-location ./HealthAggregatorApi
```

Open http://localhost:4280 to test.

### Step 3.3: Deploy to Azure

```powershell
# Login to Azure
swa login

# Deploy
swa deploy ./dashboard --api-location ./HealthAggregatorApi --env production
```

The CLI will output your live URL: `https://xxx.azurestaticapps.net`

---

## Phase 4: Verify

### Step 4.1: Test Endpoints

```powershell
# Replace with your actual URL
$baseUrl = "https://xxx.azurestaticapps.net"

# Test dashboard loads
Start-Process $baseUrl

# Test API
Invoke-RestMethod "$baseUrl/api/oura/data" | ConvertTo-Json -Depth 2
Invoke-RestMethod "$baseUrl/api/picooc/data" | ConvertTo-Json -Depth 2

# Trigger manual sync
Invoke-RestMethod "$baseUrl/api/oura/sync" -Method POST
Invoke-RestMethod "$baseUrl/api/picooc/sync" -Method POST
```

### Step 4.2: Check Timer Function

1. Azure Portal → Static Web App → Functions
2. Check "SyncTimer" is listed
3. View logs in "Monitor" section

---

## Checklist

### Phase 1: Code Preparation
- [ ] Create HealthAggregatorApi project
- [ ] Add NuGet packages
- [ ] Create BlobDataRepository
- [ ] Create SyncTimerFunction
- [ ] Create OuraFunctions
- [ ] Create PicoocFunctions
- [ ] Create DashboardFunctions
- [ ] Copy Services from existing project
- [ ] Copy Models from existing project
- [ ] Copy API clients from existing project
- [ ] Configure Program.cs with DI
- [ ] Prepare dashboard folder
- [ ] Update API URLs in index.html

### Phase 2: Azure Resources
- [ ] Create Resource Group
- [ ] Create Storage Account
- [ ] Create blob container `healthdata`
- [ ] Create Static Web App
- [ ] Configure app settings (secrets)

### Phase 3: Deploy
- [ ] Install SWA CLI
- [ ] Test locally with `swa start`
- [ ] Deploy with `swa deploy`

### Phase 4: Verify
- [ ] Dashboard loads
- [ ] API endpoints work
- [ ] Timer function registered
- [ ] Data persists in blob storage

---

## Cost Summary

| Resource | SKU | Monthly Cost |
|----------|-----|--------------|
| Static Web App | Free | $0.00 |
| Blob Storage | LRS | ~$0.01 |
| **Total** | | **~$0.01/month** |

---

## Troubleshooting

### Functions not showing in SWA
- Ensure `host.json` exists in API folder
- Check function runtime is `dotnet-isolated`

### CORS errors
- SWA handles CORS automatically for managed functions
- No configuration needed

### Timer not firing
- Check function logs in Azure Portal
- Verify `host.json` has correct settings

### Blob storage errors
- Verify connection string in app settings
- Check container `healthdata` exists
