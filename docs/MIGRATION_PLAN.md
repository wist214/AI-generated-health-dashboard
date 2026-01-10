# Health Aggregator - Azure Migration Plan

## Executive Summary

This document outlines a comprehensive migration plan for transitioning the Health Aggregator application from its current Azure Functions-based architecture to a modern, scalable architecture featuring:

- **Frontend**: React TypeScript SPA hosted on Azure Static Web Apps
- **Backend API**: .NET 8 Minimal API on Azure App Service with EF Core + Azure SQL
- **Background Processing**: Azure Functions (.NET 8 isolated) with Timer Triggers
- **Database**: Azure SQL Database (Basic/Serverless tier)

---

## 1. Current Architecture Analysis

### 1.1 Current State

| Component | Current Implementation |
|-----------|----------------------|
| **Backend** | Azure Functions v4 (.NET 8 isolated worker) |
| **Frontend** | Vanilla JavaScript/HTML SPA (dashboard folder) |
| **Data Storage** | Azure Blob Storage (JSON files) |
| **API Style** | HTTP-triggered Azure Functions |
| **Background Jobs** | Timer-triggered Azure Functions |
| **Data Sources** | Oura Ring API, Picooc API, Cronometer API |

### 1.2 Current Project Structure

```
HealthAggregatorApi/
├── Core/
│   ├── Interfaces/        # Service contracts
│   ├── Models/            # Domain models (Oura, Picooc, Cronometer)
│   └── Services/          # Business logic
├── Infrastructure/
│   ├── ExternalApis/      # Third-party API clients
│   └── Persistence/       # BlobDataRepository<T>
├── Functions/             # Azure Functions endpoints
├── dashboard/             # Current frontend (vanilla JS)
└── Program.cs             # DI configuration
```

### 1.3 Current Pain Points

1. **No relational data model** - JSON blob storage limits query flexibility
2. **Coupled architecture** - API and background sync share the same deployment
3. **Limited scalability** - Cannot scale API and background jobs independently
4. **Frontend limitations** - Vanilla JavaScript lacks type safety and component architecture

---

## 2. Target Architecture

### 2.1 High-Level Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Azure Cloud                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│   ┌────────────────────┐       ┌────────────────────┐                        │
│   │  Azure Static      │       │  Azure App Service │                        │
│   │  Web Apps          │──────▶│  (.NET 8 API)      │                        │
│   │  (React SPA)       │ REST  │  Minimal API +     │                        │
│   └────────────────────┘       │  EF Core           │                        │
│            │                   └─────────┬──────────┘                        │
│            │                             │                                    │
│            │                             │ EF Core                            │
│            │                             ▼                                    │
│   ┌────────┴───────────┐       ┌────────────────────┐                        │
│   │  GitHub Actions    │       │  Azure SQL         │                        │
│   │  CI/CD Pipeline    │       │  Database          │◀───────────────┐       │
│   └────────────────────┘       │  (Serverless)      │                │       │
│                                └────────────────────┘                │       │
│                                                                      │       │
│   ┌────────────────────────────────────────────────────────────┐    │       │
│   │                    Azure Functions                          │    │       │
│   │                    (Flex Consumption)                       │    │       │
│   │  ┌──────────────────────────────────────────────────────┐  │    │       │
│   │  │  Timer Trigger (every 30-60 min)                     │  │    │       │
│   │  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │  │    │       │
│   │  │  │ Oura Sync   │  │ Picooc Sync │  │ Cronometer  │  │──┼────┘       │
│   │  │  └─────────────┘  └─────────────┘  │ Sync        │  │  │            │
│   │  │                                    └─────────────┘  │  │            │
│   │  └──────────────────────────────────────────────────────┘  │            │
│   └────────────────────────────────────────────────────────────┘            │
│                                                                               │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 2.2 Azure Services Summary

| Service | Purpose | SKU/Tier | Est. Monthly Cost |
|---------|---------|----------|-------------------|
| Azure Static Web Apps | React SPA hosting | Free | $0 |
| Azure App Service | .NET 8 API hosting | **Free (F1)** | $0 |
| Azure SQL Database | Relational storage | **Free** (32GB limit) | $0 |
| Azure Functions | Background sync | **Consumption** (Free grant) | $0 |
| Application Insights | Monitoring | Free (5GB/month) | $0 |

**Estimated Total: $0/month** (within free tier limits)

---

## 3. Migration Phases

### Phase 1: Database Design & Setup (Week 1)

#### 3.1.1 Database Schema Design

```sql
-- Sources: Track data provider status
CREATE TABLE Sources (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProviderName NVARCHAR(50) NOT NULL UNIQUE,  -- 'Oura', 'Picooc', 'Cronometer'
    LastSyncTime DATETIME2 NULL,
    LastSyncStatus NVARCHAR(20) NOT NULL DEFAULT 'Unknown',  -- 'Success', 'Failed', 'InProgress'
    LastError NVARCHAR(MAX) NULL,
    IsEnabled BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Measurements: Unified metrics storage
CREATE TABLE Measurements (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Timestamp DATETIME2 NOT NULL,
    MetricType NVARCHAR(50) NOT NULL,  -- 'sleep_score', 'weight', 'steps', etc.
    Value DECIMAL(18,4) NOT NULL,
    Unit NVARCHAR(20) NOT NULL,        -- 'score', 'kg', 'count', 'hours', etc.
    SourceId INT NOT NULL FOREIGN KEY REFERENCES Sources(Id),
    ExternalId NVARCHAR(255) NULL,     -- Original ID from provider
    Metadata NVARCHAR(MAX) NULL,       -- JSON for additional attributes
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    INDEX IX_Measurements_Timestamp_MetricType (Timestamp DESC, MetricType),
    INDEX IX_Measurements_Source (SourceId, Timestamp DESC)
);

-- DailySummary: Pre-aggregated daily metrics for fast dashboard loads
CREATE TABLE DailySummary (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Date DATE NOT NULL,
    SourceId INT NOT NULL FOREIGN KEY REFERENCES Sources(Id),
    SleepScore INT NULL,
    ReadinessScore INT NULL,
    ActivityScore INT NULL,
    Steps INT NULL,
    Weight DECIMAL(5,2) NULL,
    BodyFat DECIMAL(5,2) NULL,
    CaloriesConsumed INT NULL,
    ProteinGrams DECIMAL(6,2) NULL,
    SleepDurationHours DECIMAL(4,2) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    UNIQUE(Date, SourceId),
    INDEX IX_DailySummary_Date (Date DESC)
);

-- MetricTypes: Reference table for metric definitions
CREATE TABLE MetricTypes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL UNIQUE,
    DisplayName NVARCHAR(100) NOT NULL,
    Unit NVARCHAR(20) NOT NULL,
    Category NVARCHAR(50) NOT NULL,    -- 'Sleep', 'Activity', 'Body', 'Nutrition'
    Description NVARCHAR(500) NULL
);
```

#### 3.1.2 EF Core Entity Models

Create new entity classes following these patterns:

```csharp
// Core/Entities/Source.cs
public class Source
{
    public int Id { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public DateTime? LastSyncTime { get; set; }
    public string LastSyncStatus { get; set; } = "Unknown";
    public string? LastError { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public ICollection<Measurement> Measurements { get; set; } = new List<Measurement>();
    public ICollection<DailySummary> DailySummaries { get; set; } = new List<DailySummary>();
}

// Core/Entities/Measurement.cs
public class Measurement
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string MetricType { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int SourceId { get; set; }
    public string? ExternalId { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public Source Source { get; set; } = null!;
}
```

#### 3.1.3 Azure SQL Best Practices Applied

Based on Microsoft documentation and Azure best practices:

1. **Use Serverless tier** for cost optimization with auto-pause
2. **Enable Azure AD authentication** with Managed Identity (passwordless)
3. **Connection string format**:
   ```
   Data Source=<server>.database.windows.net;Initial Catalog=<database>;Authentication=Active Directory Default;Encrypt=True;
   ```
4. **Configure connection resiliency** with retry logic:
   ```csharp
   options.UseSqlServer(connectionString, sqlOptions =>
   {
       sqlOptions.EnableRetryOnFailure(
           maxRetryCount: 5,
           maxRetryDelay: TimeSpan.FromSeconds(30),
           errorNumbersToAdd: null);
   });
   ```

---

### Phase 2: Backend API Development (Week 2-3)

#### 3.2.1 Project Structure (New API Project)

```
HealthAggregator.Api/
├── HealthAggregator.Api.csproj
├── Program.cs                     # Minimal API setup + DI
├── appsettings.json
├── appsettings.Development.json
│
├── Endpoints/                     # Minimal API endpoint definitions
│   ├── MetricsEndpoints.cs
│   ├── SourcesEndpoints.cs
│   └── DashboardEndpoints.cs
│
├── Data/
│   ├── HealthDbContext.cs         # EF Core DbContext
│   ├── Configurations/            # Entity configurations
│   │   ├── SourceConfiguration.cs
│   │   └── MeasurementConfiguration.cs
│   └── Migrations/                # EF Core migrations
│
├── Services/                      # Business logic
│   ├── MetricsService.cs
│   └── DashboardService.cs
│
├── DTOs/                          # API request/response models
│   ├── MetricDto.cs
│   ├── SourceStatusDto.cs
│   └── DashboardSummaryDto.cs
│
└── Extensions/
    └── ServiceCollectionExtensions.cs
```

#### 3.2.2 API Endpoints Design

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/metrics/latest` | GET | Get latest metrics from all sources |
| `/api/metrics/range` | GET | Get metrics within date range (`?from=&to=`) |
| `/api/metrics/{type}/history` | GET | Get history for specific metric type |
| `/api/sources/status` | GET | Get all data source sync statuses |
| `/api/dashboard/summary` | GET | Get aggregated dashboard data |
| `/api/dashboard/trends` | GET | Get trend data for charts |

#### 3.2.3 Minimal API Implementation Pattern

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Configure EF Core with Azure SQL
builder.Services.AddDbContext<HealthDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING");
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, 
            maxRetryDelay: TimeSpan.FromSeconds(30), 
            errorNumbersToAdd: null);
    });
});

// Register services
builder.Services.AddScoped<IMetricsService, MetricsService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Configure CORS for SPA
builder.Services.AddCors(options =>
{
    options.AddPolicy("SpaPolicy", policy =>
    {
        policy.WithOrigins(
            "https://<your-swa>.azurestaticapps.net",
            "http://localhost:3000"  // Dev
        )
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("SpaPolicy");

// Map endpoints
app.MapMetricsEndpoints();
app.MapSourcesEndpoints();
app.MapDashboardEndpoints();

app.Run();

// Endpoints/MetricsEndpoints.cs
public static class MetricsEndpoints
{
    public static void MapMetricsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/metrics").WithTags("Metrics");
        
        group.MapGet("/latest", async (IMetricsService service) =>
        {
            var metrics = await service.GetLatestMetricsAsync();
            return Results.Ok(metrics);
        });
        
        group.MapGet("/range", async (
            [FromQuery] DateTime from, 
            [FromQuery] DateTime to,
            IMetricsService service) =>
        {
            var metrics = await service.GetMetricsInRangeAsync(from, to);
            return Results.Ok(metrics);
        });
    }
}
```

#### 3.2.4 App Service Best Practices Applied

1. **Use System-Assigned Managed Identity** for Azure SQL connection
2. **Enable Application Insights** for monitoring
3. **Configure health checks** for reliability
4. **Use Key Vault references** for secrets
5. **Enable HTTPS only** and TLS 1.2+

---

### Phase 3: React Frontend Development (Week 3-4)

#### 3.3.1 React Project Structure

```
health-aggregator-spa/
├── package.json
├── tsconfig.json
├── vite.config.ts
├── index.html
│
├── src/
│   ├── main.tsx                   # Entry point
│   ├── App.tsx                    # Root component
│   ├── vite-env.d.ts
│   │
│   ├── api/                       # API client layer
│   │   ├── client.ts              # Fetch wrapper
│   │   ├── metricsApi.ts
│   │   └── types.ts               # API response types
│   │
│   ├── components/                # Reusable UI components
│   │   ├── common/
│   │   │   ├── Card.tsx
│   │   │   ├── LoadingSpinner.tsx
│   │   │   └── ErrorBoundary.tsx
│   │   ├── charts/
│   │   │   ├── TrendChart.tsx
│   │   │   └── MetricGauge.tsx
│   │   └── layout/
│   │       ├── Header.tsx
│   │       ├── Navigation.tsx
│   │       └── Layout.tsx
│   │
│   ├── features/                  # Feature-based modules
│   │   ├── dashboard/
│   │   │   ├── Dashboard.tsx
│   │   │   ├── MetricCard.tsx
│   │   │   └── useDashboard.ts
│   │   ├── sleep/
│   │   │   └── SleepTab.tsx
│   │   ├── weight/
│   │   │   └── WeightTab.tsx
│   │   └── activity/
│   │       └── ActivityTab.tsx
│   │
│   ├── hooks/                     # Custom React hooks
│   │   ├── useApi.ts
│   │   └── useMetrics.ts
│   │
│   ├── types/                     # TypeScript types
│   │   ├── metrics.ts
│   │   └── sources.ts
│   │
│   └── styles/
│       └── globals.css
│
├── public/
│   └── favicon.ico
│
└── staticwebapp.config.json       # SWA routing config
```

#### 3.3.2 Key React Components

```typescript
// src/api/client.ts
const API_BASE_URL = import.meta.env.VITE_API_URL || '/api';

export async function fetchApi<T>(
  endpoint: string, 
  options?: RequestInit
): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options?.headers,
    },
  });
  
  if (!response.ok) {
    throw new Error(`API Error: ${response.status}`);
  }
  
  return response.json();
}

// src/api/metricsApi.ts
import { fetchApi } from './client';
import { MetricLatest, MetricRange } from '../types/metrics';

export const metricsApi = {
  getLatest: () => fetchApi<MetricLatest>('/metrics/latest'),
  
  getRange: (from: string, to: string) => 
    fetchApi<MetricRange>(`/metrics/range?from=${from}&to=${to}`),
    
  getSourceStatus: () => fetchApi<SourceStatus[]>('/sources/status'),
};

// src/features/dashboard/Dashboard.tsx
import { useQuery } from '@tanstack/react-query';
import { metricsApi } from '../../api/metricsApi';
import { MetricCard } from './MetricCard';
import { TrendChart } from '../../components/charts/TrendChart';

export function Dashboard() {
  const { data: metrics, isLoading, error } = useQuery({
    queryKey: ['metrics', 'latest'],
    queryFn: metricsApi.getLatest,
    refetchInterval: 60000, // Refresh every minute
  });

  if (isLoading) return <LoadingSpinner />;
  if (error) return <ErrorMessage error={error} />;

  return (
    <div className="dashboard-grid">
      <MetricCard 
        title="Sleep Score" 
        value={metrics?.sleepScore} 
        icon="💤" 
      />
      <MetricCard 
        title="Weight" 
        value={metrics?.weight} 
        unit="kg" 
        icon="⚖️" 
      />
      {/* More metric cards */}
      <TrendChart data={metrics?.trends} />
    </div>
  );
}
```

#### 3.3.3 Azure Static Web Apps Configuration

```json
// staticwebapp.config.json
{
  "navigationFallback": {
    "rewrite": "/index.html",
    "exclude": ["/api/*", "*.{css,js,png,jpg,svg,ico}"]
  },
  "routes": [
    {
      "route": "/api/*",
      "allowedRoles": ["authenticated"]
    }
  ],
  "responseOverrides": {
    "401": {
      "statusCode": 302,
      "redirect": "/.auth/login/aad"
    }
  },
  "globalHeaders": {
    "X-Content-Type-Options": "nosniff",
    "X-Frame-Options": "DENY",
    "Content-Security-Policy": "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'"
  }
}
```

#### 3.3.4 Static Web Apps Best Practices Applied

1. **Use SWA CLI for local development**: `npm install -g @azure/static-web-apps-cli`
2. **Initialize with**: `npx swa init --yes`
3. **Configure API proxy** for backend connection
4. **Use preview environments** for PRs
5. **Enable authentication** if needed (Azure AD integration built-in)

---

### Phase 4: Background Sync Functions (Week 4)

#### 3.4.1 Azure Functions Project Structure

```
HealthAggregator.Sync/
├── HealthAggregator.Sync.csproj
├── host.json
├── local.settings.json
├── Program.cs
│
├── Functions/
│   ├── SyncTimerFunction.cs       # Main timer trigger
│   ├── OuraSyncFunction.cs        # Oura-specific HTTP trigger (manual)
│   └── ManualSyncFunction.cs      # On-demand sync trigger
│
├── Services/
│   ├── IOuraSyncService.cs
│   ├── OuraSyncService.cs
│   ├── IPicoocSyncService.cs
│   ├── PicoocSyncService.cs
│   ├── ICronometerSyncService.cs
│   └── CronometerSyncService.cs
│
├── ApiClients/                    # Reuse from existing project
│   ├── OuraApiClient.cs
│   ├── PicoocApiClient.cs
│   └── CronometerApiClient.cs
│
└── Data/
    └── SyncDbContext.cs           # Shared EF Core context
```

#### 3.4.2 Timer Trigger Implementation

```csharp
// Program.cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Add EF Core
builder.Services.AddDbContext<SyncDbContext>(options =>
{
    var connectionString = builder.Configuration["AZURE_SQL_CONNECTIONSTRING"];
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
    });
});

// Register sync services
builder.Services.AddHttpClient();
builder.Services.AddScoped<IOuraSyncService, OuraSyncService>();
builder.Services.AddScoped<IPicoocSyncService, PicoocSyncService>();
builder.Services.AddScoped<ICronometerSyncService, CronometerSyncService>();

// Application Insights
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

var app = builder.Build();
app.Run();

// Functions/SyncTimerFunction.cs
public class SyncTimerFunction
{
    private readonly IOuraSyncService _ouraSync;
    private readonly IPicoocSyncService _picoocSync;
    private readonly ICronometerSyncService _cronometerSync;
    private readonly ILogger<SyncTimerFunction> _logger;

    public SyncTimerFunction(
        IOuraSyncService ouraSync,
        IPicoocSyncService picoocSync,
        ICronometerSyncService cronometerSync,
        ILogger<SyncTimerFunction> logger)
    {
        _ouraSync = ouraSync;
        _picoocSync = picoocSync;
        _cronometerSync = cronometerSync;
        _logger = logger;
    }

    /// <summary>
    /// Runs every 30 minutes at minute 0 and 30.
    /// CRON: "0 */30 * * * *"
    /// </summary>
    [Function("SyncTimer")]
    public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo timer)
    {
        _logger.LogInformation("Sync timer triggered at {Time}", DateTime.UtcNow);
        
        var tasks = new List<Task>
        {
            SafeSync("Oura", () => _ouraSync.SyncAsync()),
            SafeSync("Picooc", () => _picoocSync.SyncAsync()),
            SafeSync("Cronometer", () => _cronometerSync.SyncAsync())
        };
        
        await Task.WhenAll(tasks);
        
        _logger.LogInformation("Sync completed. Next run: {Next}", timer.ScheduleStatus?.Next);
    }
    
    private async Task SafeSync(string source, Func<Task> syncAction)
    {
        try
        {
            await syncAction();
            _logger.LogInformation("{Source} sync completed successfully", source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Source} sync failed", source);
        }
    }
}
```

#### 3.4.3 Azure Functions Best Practices Applied

1. **Use Consumption plan** (Y1) - free tier with 1M executions/month grant
2. **Use .NET 8 isolated worker model** (not in-process)
3. **Attach User-Assigned Managed Identity** for SQL access
4. **Configure storage role assignments**:
   - Storage Blob Data Owner
   - Storage Blob Data Contributor
   - Storage Queue Data Contributor
   - Storage Table Data Contributor
5. **Enable Application Insights** for monitoring (5GB free/month)
6. **Use extension bundles** version `[4.*, 5.0.0)`

---

### Phase 5: Infrastructure as Code (Week 4-5)

#### 3.5.1 Bicep Structure

```
infra/
├── main.bicep                     # Main orchestration
├── main.parameters.json
│
├── modules/
│   ├── staticwebapp.bicep         # Azure Static Web Apps
│   ├── appservice.bicep           # App Service + Plan
│   ├── functionapp.bicep          # Azure Functions
│   ├── sqlserver.bicep            # Azure SQL Server + DB
│   ├── storage.bicep              # Storage Account
│   ├── keyvault.bicep             # Key Vault
│   ├── appinsights.bicep          # Application Insights
│   └── identity.bicep             # User-Assigned Managed Identity
│
└── scripts/
    └── deploy.ps1                 # Deployment script
```

#### 3.5.2 Main Bicep Template

```bicep
// infra/main.bicep
targetScope = 'resourceGroup'

@description('Environment name')
@allowed(['dev', 'staging', 'prod'])
param environment string = 'dev'

@description('Azure region')
param location string = resourceGroup().location

@description('Base name for resources')
param baseName string = 'healthagg'

// Unique suffix for globally unique names
var uniqueSuffix = uniqueString(resourceGroup().id)
var resourceBaseName = '${baseName}${environment}'

// User-Assigned Managed Identity (for passwordless auth)
module identity 'modules/identity.bicep' = {
  name: 'identity'
  params: {
    name: '${resourceBaseName}-id'
    location: location
  }
}

// Application Insights (Free tier: 5GB/month included)
module appInsights 'modules/appinsights.bicep' = {
  name: 'appinsights'
  params: {
    name: '${resourceBaseName}-ai'
    location: location
    dailyCapGb: 0.1  // 100MB/day = ~3GB/month (stays within 5GB free)
    retentionDays: 90  // Free retention up to 90 days
  }
}

// Storage Account (for Functions)
module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    name: '${baseName}${uniqueSuffix}'
    location: location
    identityPrincipalId: identity.outputs.principalId
  }
}

// Azure SQL Database
module sql 'modules/sqlserver.bicep' = {
  name: 'sql'
  params: {
    serverName: '${resourceBaseName}-sql'
    databaseName: 'HealthAggregator'
    location: location
    identityPrincipalId: identity.outputs.principalId
  }
}

// App Service (API) - Free F1 tier
module appService 'modules/appservice.bicep' = {
  name: 'appservice'
  params: {
    name: '${resourceBaseName}-api'
    location: location
    sku: 'F1'  // Free tier
    identityId: identity.outputs.id
    sqlConnectionString: sql.outputs.connectionString
    appInsightsKey: appInsights.outputs.instrumentationKey
    appInsightsConnectionString: appInsights.outputs.connectionString
  }
}

// Azure Functions (Sync) - Consumption plan (free grant)
module functions 'modules/functionapp.bicep' = {
  name: 'functions'
  params: {
    name: '${resourceBaseName}-func'
    location: location
    sku: 'Y1'  // Consumption plan (1M free executions/month)
    identityId: identity.outputs.id
    storageAccountName: storage.outputs.name
    sqlConnectionString: sql.outputs.connectionString
    appInsightsKey: appInsights.outputs.instrumentationKey
    appInsightsConnectionString: appInsights.outputs.connectionString
  }
}

// Static Web App (Frontend)
module staticWebApp 'modules/staticwebapp.bicep' = {
  name: 'staticwebapp'
  params: {
    name: '${resourceBaseName}-swa'
    location: location
    apiUrl: appService.outputs.url
  }
}

// Outputs
output staticWebAppUrl string = staticWebApp.outputs.url
output apiUrl string = appService.outputs.url
output functionsUrl string = functions.outputs.url
```

#### 3.5.3 SQL Server Module with Managed Identity

```bicep
// infra/modules/sqlserver.bicep
param serverName string
param databaseName string
param location string
param identityPrincipalId string

// Azure SQL Server
resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: serverName
  location: location
  properties: {
    administratorLogin: 'sqladmin'
    administratorLoginPassword: newGuid() // Will use Entra ID auth
    publicNetworkAccess: 'Enabled'
    minimalTlsVersion: '1.2'
  }
}

// Enable Azure AD authentication
resource sqlAdAdmin 'Microsoft.Sql/servers/administrators@2023-05-01-preview' = {
  parent: sqlServer
  name: 'ActiveDirectory'
  properties: {
    administratorType: 'ActiveDirectory'
    login: 'HealthAggregator-Identity'
    sid: identityPrincipalId
    tenantId: subscription().tenantId
  }
}

// Database (Free tier - 32GB limit, perfect for personal projects)
resource database 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'Free'  // Free tier: 32GB storage, 5 DTU
    tier: 'Free'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 34359738368  // 32GB max for free tier
  }
}

// Firewall rule for Azure services
resource firewallRule 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

output connectionString string = 'Data Source=${sqlServer.properties.fullyQualifiedDomainName};Initial Catalog=${databaseName};Authentication=Active Directory Default;Encrypt=True;'
output serverFqdn string = sqlServer.properties.fullyQualifiedDomainName
```

#### 3.5.4 Application Insights Module (Free Tier)

```bicep
// infra/modules/appinsights.bicep
param name string
param location string

@description('Daily data cap in GB (0.1 = 100MB/day, stays within 5GB free tier)')
param dailyCapGb float = 0.1

@description('Data retention in days (90 days free)')
@minValue(30)
@maxValue(730)
param retentionDays int = 90

// Log Analytics Workspace (required for Application Insights)
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${name}-law'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'  // Pay-per-GB, first 5GB/month free
    }
    retentionInDays: retentionDays
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    workspaceCapping: {
      dailyQuotaGb: dailyCapGb  // Cap to stay within free tier
    }
  }
}

// Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: name
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    RetentionInDays: retentionDays
  }
}

// Daily cap alert (optional - warns before hitting limit)
resource dailyCapAlert 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: '${name}-daily-cap-alert'
  location: location
  properties: {
    displayName: 'Application Insights Daily Cap Warning'
    description: 'Alert when approaching daily data cap'
    severity: 2
    enabled: true
    evaluationFrequency: 'PT1H'
    scopes: [
      logAnalytics.id
    ]
    windowSize: 'PT1H'
    criteria: {
      allOf: [
        {
          query: '''
            Usage
            | where TimeGenerated > ago(1d)
            | summarize TotalGB = sum(Quantity) / 1024
            | where TotalGB > ${dailyCapGb * 0.8}
          '''
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
        }
      ]
    }
    autoMitigate: false
  }
}

output instrumentationKey string = appInsights.properties.InstrumentationKey
output connectionString string = appInsights.properties.ConnectionString
output id string = appInsights.id
output logAnalyticsId string = logAnalytics.id
```

**Application Insights Free Tier Details:**
- **5 GB/month** data ingestion included free
- **90 days** data retention (free)
- Daily cap set to **0.1 GB (100 MB)** to stay within limits (~3GB/month)
- Alert configured to warn at **80% of daily cap**

---

### Phase 6: CI/CD Pipeline Setup (Week 5)

#### 3.6.1 GitHub Actions Workflow

```yaml
# .github/workflows/deploy.yml
name: Deploy Health Aggregator

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  workflow_dispatch:

env:
  AZURE_RESOURCE_GROUP: healthaggregator-rg
  AZURE_LOCATION: eastus
  DOTNET_VERSION: '8.0.x'
  NODE_VERSION: '20.x'

jobs:
  # Build and test API
  build-api:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      
      - name: Restore dependencies
        run: dotnet restore HealthAggregator.Api/HealthAggregator.Api.csproj
      
      - name: Build
        run: dotnet build HealthAggregator.Api/HealthAggregator.Api.csproj --configuration Release --no-restore
      
      - name: Test
        run: dotnet test --no-restore --verbosity normal
      
      - name: Publish
        run: dotnet publish HealthAggregator.Api/HealthAggregator.Api.csproj -c Release -o ./publish/api
      
      - name: Upload API artifact
        uses: actions/upload-artifact@v4
        with:
          name: api
          path: ./publish/api

  # Build React SPA
  build-spa:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}
          cache: 'npm'
          cache-dependency-path: health-aggregator-spa/package-lock.json
      
      - name: Install dependencies
        run: npm ci
        working-directory: health-aggregator-spa
      
      - name: Build
        run: npm run build
        working-directory: health-aggregator-spa
        env:
          VITE_API_URL: ${{ secrets.API_URL }}
      
      - name: Upload SPA artifact
        uses: actions/upload-artifact@v4
        with:
          name: spa
          path: health-aggregator-spa/dist

  # Build Azure Functions
  build-functions:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      
      - name: Publish Functions
        run: dotnet publish HealthAggregator.Sync/HealthAggregator.Sync.csproj -c Release -o ./publish/functions
      
      - name: Upload Functions artifact
        uses: actions/upload-artifact@v4
        with:
          name: functions
          path: ./publish/functions

  # Deploy Infrastructure
  deploy-infra:
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    outputs:
      staticWebAppUrl: ${{ steps.deploy.outputs.staticWebAppUrl }}
      apiUrl: ${{ steps.deploy.outputs.apiUrl }}
    steps:
      - uses: actions/checkout@v4
      
      - name: Azure Login
        uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}
      
      - name: Deploy Bicep
        id: deploy
        uses: azure/arm-deploy@v2
        with:
          resourceGroupName: ${{ env.AZURE_RESOURCE_GROUP }}
          template: ./infra/main.bicep
          parameters: environment=prod

  # Deploy API to App Service
  deploy-api:
    needs: [build-api, deploy-infra]
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    steps:
      - name: Download API artifact
        uses: actions/download-artifact@v4
        with:
          name: api
          path: ./api
      
      - name: Azure Login
        uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}
      
      - name: Deploy to App Service
        uses: azure/webapps-deploy@v3
        with:
          app-name: healthaggprod-api
          package: ./api
      
      - name: Run EF Migrations
        run: |
          dotnet tool install -g dotnet-ef
          dotnet ef database update --connection "${{ secrets.SQL_CONNECTION_STRING }}"

  # Deploy SPA to Static Web Apps
  deploy-spa:
    needs: [build-spa, deploy-infra]
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Download SPA artifact
        uses: actions/download-artifact@v4
        with:
          name: spa
          path: ./spa
      
      - name: Deploy to Static Web Apps
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.SWA_DEPLOYMENT_TOKEN }}
          action: 'upload'
          app_location: './spa'
          skip_app_build: true

  # Deploy Functions
  deploy-functions:
    needs: [build-functions, deploy-infra]
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    steps:
      - name: Download Functions artifact
        uses: actions/download-artifact@v4
        with:
          name: functions
          path: ./functions
      
      - name: Azure Login
        uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}
      
      - name: Deploy to Azure Functions
        uses: Azure/functions-action@v1
        with:
          app-name: healthaggprod-func
          package: ./functions
```

---

## 4. Data Migration Strategy

### 4.1 Migration Steps

1. **Export existing data** from Blob Storage
2. **Transform JSON to relational format**
3. **Import to Azure SQL** using bulk insert
4. **Verify data integrity**
5. **Switch traffic to new system**

### 4.2 Migration Script (PowerShell)

```powershell
# scripts/migrate-data.ps1

# 1. Download JSON files from Blob Storage
$storageAccount = "healthaggregatorprod"
$container = "health-data"

az storage blob download --account-name $storageAccount --container $container --name "oura_data.json" --file "./oura_data.json"
az storage blob download --account-name $storageAccount --container $container --name "picooc_data.json" --file "./picooc_data.json"
az storage blob download --account-name $storageAccount --container $container --name "cronometer_data.json" --file "./cronometer_data.json"

# 2. Run data transformation and import
dotnet run --project ./tools/DataMigration/DataMigration.csproj
```

---

## 5. Testing Strategy

### 5.1 Test Categories

| Category | Tools | Coverage Target |
|----------|-------|-----------------|
| Unit Tests | xUnit, Moq | 80%+ |
| Integration Tests | WebApplicationFactory | Critical paths |
| E2E Tests | Playwright | Happy paths |
| Load Tests | Azure Load Testing | API endpoints |

### 5.2 Local Development Testing

```bash
# Start all services locally
# Terminal 1: API
cd HealthAggregator.Api && dotnet run

# Terminal 2: Functions (requires Azurite)
azurite --silent &
cd HealthAggregator.Sync && func start

# Terminal 3: SPA
cd health-aggregator-spa && npm run dev

# Terminal 4: Proxy for SWA (optional)
npx swa start http://localhost:3000 --api-location http://localhost:7071
```

---

## 6. Rollback Plan

### 6.1 Rollback Triggers
- API error rate > 5%
- P95 latency > 2 seconds
- Database connection failures
- Critical functionality broken

### 6.2 Rollback Procedure
1. Revert GitHub deployment
2. Switch App Service to previous slot
3. Restore database from backup (if schema changed)
4. Notify stakeholders

---

## 7. Timeline & Milestones

| Week | Phase | Deliverables |
|------|-------|-------------|
| 1 | Database Setup | Schema design, EF Core models, Migrations |
| 2 | API Development | Minimal API endpoints, Service layer |
| 3 | API + Frontend | Complete API, Start React SPA |
| 4 | Frontend + Functions | Complete SPA, Background sync |
| 5 | Infrastructure | Bicep templates, CI/CD pipeline |
| 6 | Testing & Migration | Tests, Data migration, Go-live |

---

## 8. Monitoring & Operations

### 8.1 Key Metrics to Monitor

| Metric | Target | Alert Threshold |
|--------|--------|-----------------|
| API Response Time (P95) | < 500ms | > 1000ms |
| Error Rate | < 1% | > 2% |
| Database DTU Usage | < 60% | > 80% |
| Function Duration | < 5 min | > 10 min |
| SPA Load Time | < 3s | > 5s |

### 8.2 Application Insights Queries

```kusto
// API Performance
requests
| where timestamp > ago(1h)
| summarize 
    count(),
    avg(duration),
    percentile(duration, 95)
by bin(timestamp, 5m)

// Sync Function Status
traces
| where customDimensions.Category == "SyncTimerFunction"
| where timestamp > ago(24h)
| project timestamp, message, customDimensions
| order by timestamp desc
```

---

## 9. Cost Optimization

### 9.1 Free Tier Strategy

This architecture is designed to run entirely within Azure free tiers:

| Service | Free Tier Limits | Notes |
|---------|-----------------|-------|
| **App Service F1** | 60 min CPU/day, 1GB RAM | Sufficient for low-traffic API |
| **Azure SQL Free** | 32GB storage, 5 DTU | Plenty for health metrics |
| **Functions Consumption** | 1M executions/month | Timer every 30min = ~1,440/month |
| **Static Web Apps Free** | 100GB bandwidth/month | Unlimited for personal use |
| **Application Insights** | 5GB logs/month | Adequate for monitoring |

### 9.2 Free Tier Limitations to Consider

1. **App Service F1**: No custom domains, no SSL, limited CPU (consider B1 ~$13/mo if needed)
2. **Azure SQL Free**: One free DB per subscription, limited DTUs
3. **Functions Consumption**: Cold starts may add latency
4. **No SLA** on free tiers - acceptable for personal projects

### 9.3 Cost Monitoring

- Set up Azure Cost Management alerts (alert if > $1/month)
- Review monthly cost trends
- Monitor free tier usage limits
- Upgrade to paid tiers only if usage exceeds free limits

---

## 10. Security Checklist

- [ ] Enable Azure AD authentication for all services
- [ ] Use Managed Identity for service-to-service auth
- [ ] Store secrets in Key Vault
- [ ] Enable HTTPS everywhere (TLS 1.2+)
- [ ] Configure CORS policies
- [ ] Enable SQL threat detection
- [ ] Implement rate limiting on API
- [ ] Enable diagnostic logging
- [ ] Regular security scanning with Azure Defender

---

## Appendix A: Required Azure CLI Commands

```bash
# Create Resource Group
az group create --name healthaggregator-rg --location eastus

# Deploy Infrastructure
az deployment group create \
  --resource-group healthaggregator-rg \
  --template-file infra/main.bicep \
  --parameters environment=prod

# Create Service Principal for CI/CD
az ad sp create-for-rbac \
  --name "healthaggregator-cicd" \
  --role contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/healthaggregator-rg \
  --sdk-auth
```

---

## Appendix B: Local Development Prerequisites

1. **.NET 8 SDK**
2. **Node.js 20.x LTS**
3. **Azure CLI**
4. **Azure Functions Core Tools v4**
5. **Azurite** (for local storage emulation)
6. **SQL Server LocalDB** or **Docker SQL Server**
7. **VS Code Extensions**:
   - Azure Functions
   - Azure Static Web Apps
   - C# Dev Kit

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-01-09 | GitHub Copilot | Initial migration plan |
