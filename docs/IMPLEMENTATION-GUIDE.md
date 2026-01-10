# Implementation Guide - Step-by-Step Instructions

## Overview

This guide provides a **sequential, phase-by-phase approach** to implementing the refactored Health Aggregator system. Each phase builds on the previous one and includes verification steps to ensure everything works before proceeding.

**Important:** Read [docs/V2-STRUCTURE.md](V2-STRUCTURE.md) first to understand the HealthAggregatorV2 folder structure.

---

## Prerequisites

- .NET 8 SDK installed
- Node.js 18+ and npm installed
- Azure SQL Database connection string (or local SQL Server)
- Git repository cloned
- Visual Studio 2022 / VS Code / Rider

---

## Implementation Phases

### Phase 0: Preparation ⚙️

**Goal:** Set up the HealthAggregatorV2 folder structure and solution file.

**Duration:** 5 minutes

**Steps:**

1. **Create HealthAggregatorV2 root folder:**
   ```bash
   # Navigate to repository root
   cd d:\Work\My\HealthAggregator

   # Create V2 root folder
   mkdir HealthAggregatorV2
   cd HealthAggregatorV2

   # Create main folders
   mkdir src
   mkdir tests
   ```

2. **Create V2 solution:**
   ```bash
   dotnet new sln -n HealthAggregatorV2
   ```

3. **Verify existing system still works:**
   ```bash
   cd ..
   cd HealthAggregatorApi
   dotnet build
   # Should build successfully
   cd ..
   ```

**Verification:**
- ✅ `HealthAggregatorV2/` folder exists
- ✅ `HealthAggregatorV2/src/` folder exists
- ✅ `HealthAggregatorV2/tests/` folder exists
- ✅ `HealthAggregatorV2/HealthAggregatorV2.sln` exists
- ✅ `HealthAggregatorApi/` still builds successfully

**Next:** Proceed to Phase 1

---

## Phase 1: Domain Layer (Foundation) 🏗️

**Goal:** Create the shared domain models that both API and Functions will use.

**Duration:** 1-2 hours

**Reference:** [docs/plans/02-database-ef-core-implementation.md](plans/02-database-ef-core-implementation.md) - Section 3 & 4

**Steps:**

1. **Create Domain project:**
   ```bash
   # Ensure you're in HealthAggregatorV2 folder
   cd d:\Work\My\HealthAggregator\HealthAggregatorV2

   cd src
   dotnet new classlib -n Domain -f net8.0
   cd ..
   ```

2. **Add to solution:**
   ```bash
   dotnet sln add src/Domain/Domain.csproj
   ```

3. **Create folder structure in Domain project:**
   ```bash
   cd src/Domain
   mkdir Entities
   mkdir Enums
   mkdir Common
   cd ..\..
   ```

4. **Implement entities** (in `src/Domain/Entities/` folder):
   - `Source.cs` - Data source entities (Oura, Picooc, Cronometer)
   - `MetricType.cs` - Metric type definitions
   - `Measurement.cs` - Individual measurements
   - `DailySummary.cs` - Daily aggregated data

   **Example:** `Entities/Source.cs`
   ```csharp
   namespace HealthAggregatorV2.Domain.Entities;

   public class Source
   {
       public long Id { get; set; }
       public string ProviderName { get; set; } = string.Empty;
       public bool IsEnabled { get; set; }
       public DateTime? LastSyncedAt { get; set; }
       public string? ApiKeyEncrypted { get; set; }
       public DateTime CreatedAt { get; set; }
       public DateTime UpdatedAt { get; set; }

       // Navigation property
       public ICollection<Measurement> Measurements { get; set; } = new List<Measurement>();
   }
   ```

5. **Implement base entity** (in `Common/` folder):
   - `BaseEntity.cs` - Common properties (CreatedAt, UpdatedAt)

6. **Build the project:**
   ```bash
   dotnet build src/Domain
   ```

**Verification:**
- ✅ Domain project builds without errors
- ✅ All entity classes created with proper properties
- ✅ Navigation properties defined correctly

**Files Created:**
```
HealthAggregatorV2/src/Domain/
├── Domain.csproj
├── Entities/
│   ├── Source.cs
│   ├── MetricType.cs
│   ├── Measurement.cs
│   └── DailySummary.cs
├── Common/
│   └── BaseEntity.cs
└── Enums/
    └── (any enums if needed)
```

**Next:** Proceed to Phase 2

---

## Phase 2: Infrastructure Layer (Data Access) 💾

**Goal:** Implement EF Core DbContext, configurations, and repositories.

**Duration:** 2-3 hours

**Reference:** [docs/plans/02-database-ef-core-implementation.md](plans/02-database-ef-core-implementation.md) - Section 5-8

**Steps:**

1. **Create Infrastructure project:**
   ```bash
   cd src
   dotnet new classlib -n Infrastructure -f net8.0
   cd ..
   ```

2. **Add NuGet packages:**
   ```bash
   cd src/Infrastructure

   dotnet add package Microsoft.EntityFrameworkCore
   dotnet add package Microsoft.EntityFrameworkCore.SqlServer
   dotnet add package Microsoft.EntityFrameworkCore.Design
   dotnet add package Azure.Identity
   cd ..\..
   ```

3. **Add project reference to Domain:**
   ```bash
   cd src/Infrastructure
   dotnet add reference ../Domain/Domain.csproj
   cd ..\..
   ```

4. **Add to solution:**
   ```bash
   dotnet sln add src/Infrastructure/Infrastructure.csproj
   ```

5. **Create folder structure:**
   ```bash
   cd src/Infrastructure
   mkdir Data
   mkdir Data\Configurations
   mkdir Repositories
   mkdir Repositories\Interfaces
   cd ..\..
   ```

6. **Implement DbContext** (`Data/HealthDbContext.cs`):
   ```csharp
   using Microsoft.EntityFrameworkCore;
   using HealthAggregatorV2.Domain.Entities;

   namespace HealthAggregatorV2.Infrastructure.Data;

   public class HealthDbContext : DbContext
   {
       public HealthDbContext(DbContextOptions<HealthDbContext> options)
           : base(options)
       {
       }

       public DbSet<Source> Sources => Set<Source>();
       public DbSet<MetricType> MetricTypes => Set<MetricType>();
       public DbSet<Measurement> Measurements => Set<Measurement>();
       public DbSet<DailySummary> DailySummaries => Set<DailySummary>();

       protected override void OnModelCreating(ModelBuilder modelBuilder)
       {
           base.OnModelCreating(modelBuilder);

           // Apply configurations
           modelBuilder.ApplyConfigurationsFromAssembly(typeof(HealthDbContext).Assembly);
       }

       public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
       {
           // Auto-update timestamps
           var entries = ChangeTracker.Entries()
               .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

           foreach (var entry in entries)
           {
               if (entry.Entity is BaseEntity entity)
               {
                   if (entry.State == EntityState.Added)
                       entity.CreatedAt = DateTime.UtcNow;

                   entity.UpdatedAt = DateTime.UtcNow;
               }
           }

           return base.SaveChangesAsync(cancellationToken);
       }
   }
   ```

7. **Implement EF Core configurations** (in `Data/Configurations/`):
   - `SourceConfiguration.cs`
   - `MetricTypeConfiguration.cs`
   - `MeasurementConfiguration.cs`
   - `DailySummaryConfiguration.cs`

   **Example:** `Data/Configurations/MeasurementConfiguration.cs`
   ```csharp
   using Microsoft.EntityFrameworkCore;
   using Microsoft.EntityFrameworkCore.Metadata.Builders;
   using HealthAggregatorV2.Domain.Entities;

   namespace HealthAggregatorV2.Infrastructure.Data.Configurations;

   public class MeasurementConfiguration : IEntityTypeConfiguration<Measurement>
   {
       public void Configure(EntityTypeBuilder<Measurement> builder)
       {
           builder.ToTable("Measurements");

           builder.HasKey(m => m.Id);

           builder.Property(m => m.MetricType)
               .IsRequired()
               .HasMaxLength(50);

           builder.Property(m => m.Value)
               .HasColumnType("FLOAT"); // Using double type

           builder.Property(m => m.Unit)
               .IsRequired()
               .HasMaxLength(20);

           // Indexes for performance
           builder.HasIndex(m => new { m.Timestamp, m.MetricType })
               .HasDatabaseName("IX_Measurements_Timestamp_MetricType");

           builder.HasIndex(m => m.SourceId)
               .HasDatabaseName("IX_Measurements_SourceId");

           // Relationships
           builder.HasOne(m => m.Source)
               .WithMany(s => s.Measurements)
               .HasForeignKey(m => m.SourceId)
               .OnDelete(DeleteBehavior.Cascade);
       }
   }
   ```

8. **Implement Repository interfaces** (in `Repositories/Interfaces/`):
   - `ISourcesRepository.cs`
   - `IMeasurementsRepository.cs`
   - `IDailySummaryRepository.cs`

9. **Implement Repository classes** (in `Repositories/`):
   - `SourcesRepository.cs`
   - `MeasurementsRepository.cs`
   - `DailySummaryRepository.cs`

   **Example:** `Repositories/MeasurementsRepository.cs`
   ```csharp
   using Microsoft.EntityFrameworkCore;
   using HealthAggregatorV2.Domain.Entities;
   using HealthAggregatorV2.Infrastructure.Data;
   using HealthAggregatorV2.Infrastructure.Repositories.Interfaces;

   namespace HealthAggregatorV2.Infrastructure.Repositories;

   public class MeasurementsRepository : IMeasurementsRepository
   {
       private readonly HealthDbContext _context;

       public MeasurementsRepository(HealthDbContext context)
       {
           _context = context;
       }

       public async Task<Measurement?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
       {
           return await _context.Measurements
               .Include(m => m.Source)
               .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
       }

       public async Task<IEnumerable<Measurement>> GetLatestByMetricTypeAsync(
           string metricType,
           int count = 1,
           CancellationToken cancellationToken = default)
       {
           return await _context.Measurements
               .AsNoTracking()
               .Where(m => m.MetricType == metricType)
               .OrderByDescending(m => m.Timestamp)
               .Take(count)
               .ToListAsync(cancellationToken);
       }

       public async Task AddAsync(Measurement measurement, CancellationToken cancellationToken = default)
       {
           await _context.Measurements.AddAsync(measurement, cancellationToken);
           await _context.SaveChangesAsync(cancellationToken);
       }

       public async Task AddRangeAsync(IEnumerable<Measurement> measurements, CancellationToken cancellationToken = default)
       {
           await _context.Measurements.AddRangeAsync(measurements, cancellationToken);
           await _context.SaveChangesAsync(cancellationToken);
       }
   }
   ```

10. **Build the project:**
    ```bash
    dotnet build src/Infrastructure
    ```

**Verification:**
- ✅ Infrastructure project builds without errors
- ✅ All repository interfaces and implementations created
- ✅ EF Core configurations applied correctly
- ✅ DbContext compiles successfully

**Files Created:**
```
HealthAggregatorV2/src/Infrastructure/
├── Infrastructure.csproj
├── Data/
│   ├── HealthDbContext.cs
│   └── Configurations/
│       ├── SourceConfiguration.cs
│       ├── MetricTypeConfiguration.cs
│       ├── MeasurementConfiguration.cs
│       └── DailySummaryConfiguration.cs
└── Repositories/
    ├── Interfaces/
    │   ├── ISourcesRepository.cs
    │   ├── IMeasurementsRepository.cs
    │   └── IDailySummaryRepository.cs
    └── (implementations)
```

**Next:** Proceed to Phase 3

---

## Phase 3: API Project (Minimal API) 🚀

**Goal:** Create the .NET 8 Minimal API with endpoints and services.

**Duration:** 3-4 hours

**Reference:** [docs/plans/01-backend-api-implementation.md](plans/01-backend-api-implementation.md)

**Steps:**

1. **Create API project:**
   ```bash
   cd src
   dotnet new web -n Api -f net8.0
   cd ..
   ```

2. **Add NuGet packages:**
   ```bash
   cd src/Api

   dotnet add package Microsoft.EntityFrameworkCore.Design
   dotnet add package Swashbuckle.AspNetCore
   dotnet add package Azure.Identity
   dotnet add package Microsoft.ApplicationInsights.AspNetCore
   cd ..\..
   ```

3. **Add project references:**
   ```bash
   cd src/Api
   dotnet add reference ../Domain/Domain.csproj
   dotnet add reference ../Infrastructure/Infrastructure.csproj
   cd ..\..
   ```

4. **Add to solution:**
   ```bash
   dotnet sln add src/Api/Api.csproj
   ```

5. **Create folder structure:**
   ```bash
   cd src/Api
   mkdir Endpoints
   mkdir Services
   mkdir Services\Interfaces
   mkdir DTOs
   mkdir Extensions
   mkdir Middleware
   cd ..\..
   ```

6. **Implement DTOs** (in `DTOs/` folder):
   - `MetricLatestDto.cs`
   - `DashboardSummaryDto.cs`
   - `SourceStatusDto.cs`

7. **Implement Services** (in `Services/` folder):
   - `IMetricsService.cs` (interface)
   - `MetricsService.cs` (implementation)
   - `IDashboardService.cs` (interface)
   - `DashboardService.cs` (implementation)

8. **Implement Endpoints** (in `Endpoints/` folder):
   - `MetricsEndpoints.cs`
   - `DashboardEndpoints.cs`
   - `SourcesEndpoints.cs`

   **Example:** `Endpoints/MetricsEndpoints.cs`
   ```csharp
   using Microsoft.AspNetCore.Mvc;
   using HealthAggregatorV2.Api.Services.Interfaces;

   namespace HealthAggregatorV2.Api.Endpoints;

   public static class MetricsEndpoints
   {
       public static void MapMetricsEndpoints(this IEndpointRouteBuilder app)
       {
           var group = app.MapGroup("/api/metrics")
               .WithTags("Metrics")
               .WithOpenApi();

           group.MapGet("/latest/{metricType}", GetLatestMetric)
               .WithName("GetLatestMetric")
               .WithSummary("Get the latest measurement for a specific metric type");

           group.MapGet("/range", GetMetricsInRange)
               .WithName("GetMetricsInRange")
               .WithSummary("Get measurements within a date range");
       }

       private static async Task<IResult> GetLatestMetric(
           string metricType,
           [FromServices] IMetricsService service,
           CancellationToken cancellationToken)
       {
           var result = await service.GetLatestMetricAsync(metricType, cancellationToken);
           return result != null ? Results.Ok(result) : Results.NotFound();
       }

       private static async Task<IResult> GetMetricsInRange(
           [FromQuery] DateTime from,
           [FromQuery] DateTime to,
           [FromQuery] string? metricType,
           [FromServices] IMetricsService service,
           CancellationToken cancellationToken)
       {
           var result = await service.GetMetricsInRangeAsync(from, to, metricType, cancellationToken);
           return Results.Ok(result);
       }
   }
   ```

9. **Implement Extension methods** (in `Extensions/` folder):
   - `ServiceCollectionExtensions.cs` - For DI registration

   **Example:** `Extensions/ServiceCollectionExtensions.cs`
   ```csharp
   using Microsoft.EntityFrameworkCore;
   using HealthAggregatorV2.Infrastructure.Data;
   using HealthAggregatorV2.Infrastructure.Repositories;
   using HealthAggregatorV2.Infrastructure.Repositories.Interfaces;
   using HealthAggregatorV2.Api.Services;
   using HealthAggregatorV2.Api.Services.Interfaces;

   namespace HealthAggregatorV2.Api.Extensions;

   public static class ServiceCollectionExtensions
   {
       public static IServiceCollection AddDatabaseContext(
           this IServiceCollection services,
           IConfiguration configuration)
       {
           services.AddDbContext<HealthDbContext>(options =>
           {
               options.UseSqlServer(
                   configuration.GetConnectionString("HealthDb"),
                   sqlServerOptions =>
                   {
                       sqlServerOptions.MigrationsAssembly("Api");
                       sqlServerOptions.EnableRetryOnFailure(
                           maxRetryCount: 5,
                           maxRetryDelay: TimeSpan.FromSeconds(30),
                           errorNumbersToAdd: null);
                   });
           });

           return services;
       }

       public static IServiceCollection AddRepositories(this IServiceCollection services)
       {
           services.AddScoped<ISourcesRepository, SourcesRepository>();
           services.AddScoped<IMeasurementsRepository, MeasurementsRepository>();
           services.AddScoped<IDailySummaryRepository, DailySummaryRepository>();

           return services;
       }

       public static IServiceCollection AddApplicationServices(this IServiceCollection services)
       {
           services.AddScoped<IMetricsService, MetricsService>();
           services.AddScoped<IDashboardService, DashboardService>();

           return services;
       }
   }
   ```

10. **Update Program.cs:**
    ```csharp
    using HealthAggregatorV2.Api.Extensions;
    using HealthAggregatorV2.Api.Endpoints;

    var builder = WebApplication.CreateBuilder(args);

    // ===== Service Registration =====

    // Database
    builder.Services.AddDatabaseContext(builder.Configuration);

    // Repositories
    builder.Services.AddRepositories();

    // Application Services
    builder.Services.AddApplicationServices();

    // API Documentation
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new()
        {
            Title = "Health Aggregator API",
            Version = "v1"
        });
    });

    // CORS for SPA
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("SpaPolicy", policy =>
        {
            policy.WithOrigins(
                    builder.Configuration["AllowedOrigins:SPA"] ?? "http://localhost:5173",
                    builder.Configuration["AllowedOrigins:SWA"] ?? "https://*.azurestaticapps.net"
                )
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
    });

    var app = builder.Build();

    // ===== Middleware Pipeline =====

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors("SpaPolicy");

    // Map endpoints
    app.MapMetricsEndpoints();
    app.MapDashboardEndpoints();
    app.MapSourcesEndpoints();

    app.MapGet("/", () => "Health Aggregator API v1.0");

    app.Run();
    ```

11. **Create appsettings.json:**
    ```json
    {
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft.AspNetCore": "Warning"
        }
      },
      "AllowedHosts": "*",
      "ConnectionStrings": {
        "HealthDb": "Server=tcp:your-server.database.windows.net,1433;Database=HealthAggregator;User ID=your-user;Password=your-password;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
      },
      "AllowedOrigins": {
        "SPA": "http://localhost:5173",
        "SWA": "https://*.azurestaticapps.net"
      }
    }
    ```

12. **Create initial migration:**
    ```bash
    cd src/Api
    dotnet ef migrations add InitialCreate
    cd ..\..
    ```

13. **Build the API project:**
    ```bash
    dotnet build src/Api
    ```

14. **Run the API:**
    ```bash
    cd src/Api
    dotnet run
    ```

15. **Test in browser:**
    - Open: `http://localhost:5000` (or whatever port is shown)
    - Open Swagger: `http://localhost:5000/swagger`

**Verification:**
- ✅ API project builds successfully
- ✅ API starts without errors
- ✅ Swagger UI is accessible
- ✅ Can see all endpoints in Swagger
- ✅ Database migration created (check `Migrations/` folder)

**Optional:** Apply migration to database:
```bash
dotnet ef database update
```

**Files Created:**
```
HealthAggregatorV2/src/Api/
├── Api.csproj
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── Endpoints/
│   ├── MetricsEndpoints.cs
│   ├── DashboardEndpoints.cs
│   └── SourcesEndpoints.cs
├── Services/
│   ├── Interfaces/
│   └── (implementations)
├── DTOs/
├── Extensions/
│   └── ServiceCollectionExtensions.cs
└── Migrations/
    └── (EF Core migrations)
```

**Next:** Proceed to Phase 4

---

## Phase 4: React SPA (Frontend) ⚛️

**Goal:** Create the React TypeScript SPA with pixel-perfect design matching the current dashboard.

**Duration:** 4-6 hours

**Reference:** [docs/plans/03-react-spa-implementation.md](plans/03-react-spa-implementation.md)

**Steps:**

1. **Create React SPA with Vite:**
   ```bash
   cd src
   npm create vite@latest Spa -- --template react-ts
   cd Spa
   npm install
   cd ..\..
   ```

2. **Install dependencies:**
   ```bash
   cd src/Spa
   npm install @fluentui/react-components
   npm install @tanstack/react-query
   npm install axios
   npm install react-router-dom
   npm install chart.js react-chartjs-2
   ```

3. **Install dev dependencies:**
   ```bash
   npm install -D @types/node
   npm install -D vitest @vitest/ui
   npm install -D @testing-library/react @testing-library/jest-dom
   cd ..\..
   ```

4. **Create folder structure:**
   ```bash
   mkdir -p src/features/dashboard/components
   mkdir -p src/features/dashboard/hooks
   mkdir -p src/features/dashboard/services
   mkdir -p src/features/oura/components
   mkdir -p src/features/food/components
   mkdir -p src/shared/components
   mkdir -p src/shared/hooks
   mkdir -p src/shared/services
   mkdir -p src/shared/types
   mkdir -p src/shared/utils
   mkdir -p src/styles
   mkdir -p src/config
   ```

5. **Configure API client** (`src/shared/services/apiClient.ts`):
   ```typescript
   import axios from 'axios';

   const apiClient = axios.create({
     baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000/api',
     headers: {
       'Content-Type': 'application/json',
     },
   });

   apiClient.interceptors.response.use(
     (response) => response,
     (error) => {
       console.error('API Error:', error);
       return Promise.reject(error);
     }
   );

   export default apiClient;
   ```

6. **Create TypeScript types** (`src/shared/types/metrics.ts`):
   ```typescript
   export interface MetricLatest {
     metricType: string;
     value: number;
     unit: string;
     timestamp: string;
     sourceName: string;
   }

   export interface DashboardSummary {
     sleepScore?: number;
     readinessScore?: number;
     weight?: number;
     steps?: number;
     lastUpdated: string;
   }
   ```

7. **Create API service** (`src/features/dashboard/services/dashboardService.ts`):
   ```typescript
   import apiClient from '@/shared/services/apiClient';
   import { DashboardSummary, MetricLatest } from '@/shared/types/metrics';

   export const dashboardService = {
     async getSummary(): Promise<DashboardSummary> {
       const response = await apiClient.get<DashboardSummary>('/dashboard/summary');
       return response.data;
     },

     async getLatestMetric(metricType: string): Promise<MetricLatest> {
       const response = await apiClient.get<MetricLatest>(`/metrics/latest/${metricType}`);
       return response.data;
     },
   };
   ```

8. **Create React Query hooks** (`src/features/dashboard/hooks/useDashboardData.ts`):
   ```typescript
   import { useQuery } from '@tanstack/react-query';
   import { dashboardService } from '../services/dashboardService';

   export const useDashboardSummary = () => {
     return useQuery({
       queryKey: ['dashboard', 'summary'],
       queryFn: () => dashboardService.getSummary(),
       refetchInterval: 60000, // Refetch every minute
     });
   };

   export const useLatestMetric = (metricType: string) => {
     return useQuery({
       queryKey: ['metric', 'latest', metricType],
       queryFn: () => dashboardService.getLatestMetric(metricType),
       enabled: !!metricType,
     });
   };
   ```

9. **Copy design tokens from existing CSS** (`src/styles/tokens.css`):
   ```css
   :root {
     /* Colors - matching current dashboard */
     --color-primary: #00d4ff;
     --color-secondary: #7c3aed;
     --color-oura: #00a99d;
     --color-food: #f97316;

     --color-bg-dark: #1a1a2e;
     --color-bg-darker: #16213e;
     --color-surface: rgba(255, 255, 255, 0.05);
     --color-surface-hover: rgba(255, 255, 255, 0.1);

     /* Typography */
     --font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
     --font-size-xs: 0.75rem;
     --font-size-sm: 0.875rem;
     --font-size-base: 1rem;
     --font-size-lg: 1.125rem;
     --font-size-xl: 1.25rem;
     --font-size-2xl: 1.5rem;
     --font-size-3xl: 1.875rem;
     --font-size-4xl: 2.25rem;
     --font-size-metric: 3rem;

     /* Spacing */
     --spacing-xs: 0.25rem;
     --spacing-sm: 0.5rem;
     --spacing-md: 1rem;
     --spacing-lg: 1.5rem;
     --spacing-xl: 2rem;

     /* Border radius */
     --radius-sm: 8px;
     --radius-md: 12px;
     --radius-lg: 20px;

     /* Shadows */
     --shadow-sm: 0 2px 8px rgba(0, 0, 0, 0.1);
     --shadow-md: 0 4px 16px rgba(0, 0, 0, 0.2);
     --shadow-lg: 0 8px 32px rgba(0, 0, 0, 0.3);
   }
   ```

10. **Create MetricCard component** (`src/shared/components/MetricCard.tsx`):
    ```tsx
    import React from 'react';
    import { Card } from '@fluentui/react-components';
    import styles from './MetricCard.module.css';

    interface MetricCardProps {
      title: string;
      value: number | string;
      unit: string;
      icon?: React.ReactNode;
      color?: 'primary' | 'oura' | 'food';
    }

    export const MetricCard: React.FC<MetricCardProps> = ({
      title,
      value,
      unit,
      icon,
      color = 'primary',
    }) => {
      return (
        <Card className={`${styles.metricCard} ${styles[color]}`}>
          <div className={styles.header}>
            {icon && <div className={styles.icon}>{icon}</div>}
            <h3 className={styles.title}>{title}</h3>
          </div>
          <div className={styles.body}>
            <div className={styles.value}>{value}</div>
            <div className={styles.unit}>{unit}</div>
          </div>
        </Card>
      );
    };
    ```

11. **Create MetricCard styles** (`src/shared/components/MetricCard.module.css`):
    ```css
    .metricCard {
      background: var(--color-surface);
      backdrop-filter: blur(10px);
      border-radius: var(--radius-lg);
      padding: var(--spacing-lg);
      border-top: 4px solid transparent;
      transition: transform 0.2s ease, box-shadow 0.2s ease;
    }

    .metricCard:hover {
      transform: translateY(-2px);
      box-shadow: var(--shadow-lg);
    }

    .metricCard.primary {
      border-top-color: var(--color-primary);
    }

    .metricCard.oura {
      border-top-color: var(--color-oura);
    }

    .metricCard.food {
      border-top-color: var(--color-food);
    }

    .value {
      font-size: var(--font-size-metric);
      font-weight: 700;
      background: linear-gradient(90deg, var(--color-primary), var(--color-secondary));
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      background-clip: text;
    }

    .unit {
      font-size: var(--font-size-sm);
      color: rgba(255, 255, 255, 0.6);
      margin-top: var(--spacing-xs);
    }
    ```

12. **Create Dashboard page** (`src/features/dashboard/DashboardPage.tsx`):
    ```tsx
    import React from 'react';
    import { useDashboardSummary } from './hooks/useDashboardData';
    import { MetricCard } from '@/shared/components/MetricCard';
    import styles from './DashboardPage.module.css';

    export const DashboardPage: React.FC = () => {
      const { data, isLoading, error } = useDashboardSummary();

      if (isLoading) return <div>Loading...</div>;
      if (error) return <div>Error loading dashboard</div>;

      return (
        <div className={styles.dashboard}>
          <h1 className={styles.title}>Health Dashboard</h1>

          <div className={styles.grid}>
            {data?.sleepScore && (
              <MetricCard
                title="Sleep Score"
                value={data.sleepScore}
                unit="score"
                color="oura"
              />
            )}

            {data?.readinessScore && (
              <MetricCard
                title="Readiness Score"
                value={data.readinessScore}
                unit="score"
                color="oura"
              />
            )}

            {data?.weight && (
              <MetricCard
                title="Weight"
                value={data.weight.toFixed(1)}
                unit="kg"
                color="primary"
              />
            )}

            {data?.steps && (
              <MetricCard
                title="Steps"
                value={data.steps.toLocaleString()}
                unit="steps"
                color="primary"
              />
            )}
          </div>
        </div>
      );
    };
    ```

13. **Setup React Router** (`src/App.tsx`):
    ```tsx
    import { BrowserRouter, Routes, Route } from 'react-router-dom';
    import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
    import { FluentProvider, webDarkTheme } from '@fluentui/react-components';
    import { DashboardPage } from './features/dashboard/DashboardPage';
    import './styles/global.css';

    const queryClient = new QueryClient();

    function App() {
      return (
        <QueryClientProvider client={queryClient}>
          <FluentProvider theme={webDarkTheme}>
            <BrowserRouter>
              <Routes>
                <Route path="/" element={<DashboardPage />} />
              </Routes>
            </BrowserRouter>
          </FluentProvider>
        </QueryClientProvider>
      );
    }

    export default App;
    ```

14. **Create .env file:**
    ```bash
    VITE_API_URL=http://localhost:5000/api
    ```

15. **Update vite.config.ts:**
    ```typescript
    import { defineConfig } from 'vite';
    import react from '@vitejs/plugin-react';
    import path from 'path';

    export default defineConfig({
      plugins: [react()],
      resolve: {
        alias: {
          '@': path.resolve(__dirname, './src'),
        },
      },
      server: {
        port: 5173,
        proxy: {
          '/api': {
            target: 'http://localhost:5000',
            changeOrigin: true,
          },
        },
      },
    });
    ```

16. **Build the SPA:**
    ```bash
    npm run build
    ```

17. **Run the SPA:**
    ```bash
    npm run dev
    ```

18. **Test in browser:**
    - Open: `http://localhost:5173`

**Verification:**
- ✅ SPA builds without errors
- ✅ Development server starts
- ✅ Can access dashboard at localhost:5173
- ✅ Design matches existing dashboard (dark theme, gradients, etc.)
- ✅ API calls work (check Network tab in DevTools)

**Files Created:**
```
HealthAggregatorV2/src/Spa/
├── package.json
├── vite.config.ts
├── tsconfig.json
├── .env
├── src/
│   ├── App.tsx
│   ├── main.tsx
│   ├── features/
│   │   └── dashboard/
│   │       ├── DashboardPage.tsx
│   │       ├── hooks/
│   │       └── services/
│   ├── shared/
│   │   ├── components/
│   │   │   └── MetricCard.tsx
│   │   ├── services/
│   │   │   └── apiClient.ts
│   │   └── types/
│   │       └── metrics.ts
│   └── styles/
│       ├── tokens.css
│       └── global.css
└── public/
```

**Next:** Proceed to Phase 5

---

## Phase 5: Azure Functions (Background Sync) ⚡

**Goal:** Create new Azure Functions for data synchronization.

**Duration:** 3-4 hours

**Reference:** [docs/plans/04-azure-functions-sync-implementation.md](plans/04-azure-functions-sync-implementation.md)

**Steps:**

1. **Create Functions project:**
   ```bash
   cd src
   dotnet new func -n Functions --worker-runtime dotnet-isolated
   cd ..
   ```

2. **Add NuGet packages:**
   ```bash
   cd src/Functions

   dotnet add package Microsoft.Azure.Functions.Worker
   dotnet add package Microsoft.Azure.Functions.Worker.Sdk
   dotnet add package Microsoft.ApplicationInsights.WorkerService
   cd ..\..
   ```

3. **Add project references:**
   ```bash
   cd src/Functions
   dotnet add reference ../Domain/Domain.csproj
   dotnet add reference ../Infrastructure/Infrastructure.csproj
   cd ..\..
   ```

4. **Add to solution:**
   ```bash
   dotnet sln add src/Functions/Functions.csproj
   ```

5. **Create folder structure:**
   ```bash
   cd src/Functions
   mkdir Functions
   mkdir Application
   mkdir Application\Services
   mkdir Application\Services\Interfaces
   mkdir Application\Services\DataSources
   mkdir Extensions
   cd ..\..
   ```

6. **Implement sync service interfaces** (`Application/Services/Interfaces/`):
   - `ISyncOrchestrator.cs`
   - `IDataSourceSyncService.cs`

7. **Implement concrete sync services** (`Application/Services/DataSources/`):
   - `OuraSyncService.cs`
   - `PicoocSyncService.cs`
   - `CronometerSyncService.cs`

8. **Implement orchestrator** (`Application/Services/SyncOrchestrator.cs`):
   ```csharp
   namespace HealthAggregatorV2.Functions.Application.Services;

   public class SyncOrchestrator : ISyncOrchestrator
   {
       private readonly IEnumerable<IDataSourceSyncService> _syncServices;
       private readonly ILogger<SyncOrchestrator> _logger;

       public SyncOrchestrator(
           IEnumerable<IDataSourceSyncService> syncServices,
           ILogger<SyncOrchestrator> logger)
       {
           _syncServices = syncServices;
           _logger = logger;
       }

       public async Task SyncAllSourcesAsync(CancellationToken cancellationToken = default)
       {
           _logger.LogInformation("Starting sync for all data sources");

           var tasks = _syncServices.Select(service =>
               SafeSyncAsync(service, cancellationToken));

           await Task.WhenAll(tasks);

           _logger.LogInformation("Completed sync for all data sources");
       }

       private async Task SafeSyncAsync(
           IDataSourceSyncService service,
           CancellationToken cancellationToken)
       {
           try
           {
               await service.SyncAsync(cancellationToken);
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error syncing {ServiceName}", service.GetType().Name);
           }
       }
   }
   ```

9. **Create Timer Function** (`Functions/SyncTimerFunction.cs`):
   ```csharp
   using Microsoft.Azure.Functions.Worker;
   using Microsoft.Extensions.Logging;
   using HealthAggregatorV2.Functions.Application.Services.Interfaces;

   namespace HealthAggregatorV2.Functions.Functions;

   public class SyncTimerFunction
   {
       private readonly ISyncOrchestrator _orchestrator;
       private readonly ILogger<SyncTimerFunction> _logger;

       public SyncTimerFunction(
           ISyncOrchestrator orchestrator,
           ILogger<SyncTimerFunction> logger)
       {
           _orchestrator = orchestrator;
           _logger = logger;
       }

       [Function("SyncTimer")]
       public async Task Run(
           [TimerTrigger("0 */30 * * * *")] TimerInfo timer,
           CancellationToken cancellationToken)
       {
           _logger.LogInformation("Timer trigger function executed at: {Now}", DateTime.UtcNow);

           await _orchestrator.SyncAllSourcesAsync(cancellationToken);

           _logger.LogInformation("Next timer schedule: {Next}", timer.ScheduleStatus?.Next);
       }
   }
   ```

10. **Update Program.cs:**
    ```csharp
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using HealthAggregatorV2.Functions.Extensions;

    var host = new HostBuilder()
        .ConfigureFunctionsWebApplication()
        .ConfigureServices(services =>
        {
            services.AddApplicationInsightsTelemetryWorkerService();
            services.ConfigureFunctionsApplicationInsights();

            // Database
            services.AddDatabaseContext();

            // Repositories
            services.AddRepositories();

            // Sync services
            services.AddSyncServices();
        })
        .Build();

    await host.RunAsync();
    ```

11. **Create extension methods** (`Extensions/ServiceCollectionExtensions.cs`):
    ```csharp
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using HealthAggregatorV2.Infrastructure.Data;
    using HealthAggregatorV2.Infrastructure.Repositories;
    using HealthAggregatorV2.Infrastructure.Repositories.Interfaces;
    using HealthAggregatorV2.Functions.Application.Services;
    using HealthAggregatorV2.Functions.Application.Services.Interfaces;
    using HealthAggregatorV2.Functions.Application.Services.DataSources;

    namespace HealthAggregatorV2.Functions.Extensions;

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabaseContext(this IServiceCollection services)
        {
            services.AddDbContext<HealthDbContext>((provider, options) =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                options.UseSqlServer(configuration.GetConnectionString("HealthDb"));
            });

            return services;
        }

        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IMeasurementsRepository, MeasurementsRepository>();
            services.AddScoped<ISourcesRepository, SourcesRepository>();

            return services;
        }

        public static IServiceCollection AddSyncServices(this IServiceCollection services)
        {
            services.AddScoped<ISyncOrchestrator, SyncOrchestrator>();

            // Register individual sync services
            services.AddScoped<IDataSourceSyncService, OuraSyncService>();
            services.AddScoped<IDataSourceSyncService, PicoocSyncService>();
            services.AddScoped<IDataSourceSyncService, CronometerSyncService>();

            return services;
        }
    }
    ```

12. **Create local.settings.json:**
    ```json
    {
      "IsEncrypted": false,
      "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
        "ConnectionStrings:HealthDb": "Server=tcp:your-server.database.windows.net,1433;Database=HealthAggregator;..."
      }
    }
    ```

13. **Build Functions project:**
    ```bash
    dotnet build src/Functions
    ```

14. **Run Functions locally:**
    ```bash
    cd src/Functions
    func start
    cd ..\..
    ```

**Verification:**
- ✅ Functions project builds successfully
- ✅ Timer function is registered and visible
- ✅ Function can connect to database
- ✅ Sync logic executes without errors

**Files Created:**
```
HealthAggregatorV2/src/Functions/
├── Functions.csproj
├── Program.cs
├── host.json
├── local.settings.json
├── Functions/
│   └── SyncTimerFunction.cs
├── Application/
│   └── Services/
│       ├── Interfaces/
│       ├── SyncOrchestrator.cs
│       └── DataSources/
│           ├── OuraSyncService.cs
│           ├── PicoocSyncService.cs
│           └── CronometerSyncService.cs
└── Extensions/
    └── ServiceCollectionExtensions.cs
```

**Next:** Proceed to Phase 6

---

## Phase 6: Testing Setup 🧪

**Goal:** Create test projects for unit tests and E2E tests.

**Duration:** 2-3 hours

**Reference:** [docs/plans/06-testing-strategy.md](plans/06-testing-strategy.md)

**Steps:**

1. **Create test projects:**
   ```bash
   cd tests

   # API unit tests
   dotnet new xunit -n Api.Tests -f net8.0

   # API E2E tests
   dotnet new xunit -n Api.E2E -f net8.0

   # Functions unit tests
   dotnet new xunit -n Functions.Tests -f net8.0

   cd ..
   ```

2. **Add test projects to solution:**
   ```bash
   dotnet sln add tests/Api.Tests/Api.Tests.csproj
   dotnet sln add tests/Api.E2E/Api.E2E.csproj
   dotnet sln add tests/Functions.Tests/Functions.Tests.csproj
   ```

3. **Add NuGet packages to test projects:**
   ```bash
   # API Tests
   cd tests/Api.Tests
   dotnet add package Moq
   dotnet add package FluentAssertions
   dotnet add package Microsoft.EntityFrameworkCore.InMemory

   # API E2E
   cd ../Api.E2E
   dotnet add package Microsoft.AspNetCore.Mvc.Testing
   dotnet add package Microsoft.Playwright

   # Functions Tests
   cd ../Functions.Tests
   dotnet add package Moq
   dotnet add package FluentAssertions

   cd ..\..
   ```

4. **Add project references:**
   ```bash
   cd tests/Api.Tests
   dotnet add reference ../../src/Api/Api.csproj
   dotnet add reference ../../src/Domain/Domain.csproj

   cd ../Api.E2E
   dotnet add reference ../../src/Api/Api.csproj

   cd ../Functions.Tests
   dotnet add reference ../../src/Functions/Functions.csproj

   cd ..\..
   ```

5. **Create sample unit test** (`tests/Api.Tests/Services/MetricsServiceTests.cs`):
   ```csharp
   using Xunit;
   using Moq;
   using FluentAssertions;
   using HealthAggregatorV2.Api.Services;
   using HealthAggregatorV2.Infrastructure.Repositories.Interfaces;

   namespace HealthAggregatorV2.Api.Tests.Services;

   public class MetricsServiceTests
   {
       private readonly Mock<IMeasurementsRepository> _mockRepository;
       private readonly MetricsService _service;

       public MetricsServiceTests()
       {
           _mockRepository = new Mock<IMeasurementsRepository>();
           _service = new MetricsService(_mockRepository.Object);
       }

       [Fact]
       public async Task GetLatestMetric_ReturnsMetric_WhenExists()
       {
           // Arrange
           var metricType = "weight";
           // Setup mock...

           // Act
           var result = await _service.GetLatestMetricAsync(metricType);

           // Assert
           result.Should().NotBeNull();
       }
   }
   ```

6. **Setup React component tests:**
   ```bash
   cd src/Spa

   npm install -D vitest @vitest/ui @testing-library/react @testing-library/jest-dom jsdom
   cd ..\..
   ```

7. **Create vitest.config.ts:**
   ```typescript
   import { defineConfig } from 'vitest/config';
   import react from '@vitejs/plugin-react';
   import path from 'path';

   export default defineConfig({
     plugins: [react()],
     test: {
       environment: 'jsdom',
       globals: true,
       setupFiles: ['./src/test/setup.ts'],
     },
     resolve: {
       alias: {
         '@': path.resolve(__dirname, './src'),
       },
     },
   });
   ```

8. **Create test setup** (`src/test/setup.ts`):
   ```typescript
   import '@testing-library/jest-dom';
   ```

9. **Create sample React test** (`src/shared/components/MetricCard.test.tsx`):
   ```tsx
   import { describe, it, expect } from 'vitest';
   import { render, screen } from '@testing-library/react';
   import { MetricCard } from './MetricCard';

   describe('MetricCard', () => {
     it('renders metric value correctly', () => {
       render(
         <MetricCard
           title="Weight"
           value={75.5}
           unit="kg"
           color="primary"
         />
       );

       expect(screen.getByText('75.5')).toBeInTheDocument();
       expect(screen.getByText('kg')).toBeInTheDocument();
     });
   });
   ```

10. **Run tests:**
    ```bash
    # .NET tests
    dotnet test

    # React tests
    cd src/Spa
    npm run test
    cd ..\..
    ```

**Verification:**
- ✅ All test projects build successfully
- ✅ Sample tests run and pass
- ✅ Test coverage can be measured

**Files Created:**
```
HealthAggregatorV2/tests/
├── Api.Tests/
│   ├── Api.Tests.csproj
│   └── Services/
│       └── MetricsServiceTests.cs
├── Api.E2E/
│   └── Api.E2E.csproj
└── Functions.Tests/
    └── Functions.Tests.csproj

HealthAggregatorV2/src/Spa/
├── vitest.config.ts
└── src/
    ├── test/
    │   └── setup.ts
    └── shared/components/
        └── MetricCard.test.tsx
```

**Next:** Proceed to Phase 7

---

## Phase 7: Integration & Deployment 🚀

**Goal:** Ensure all components work together and prepare for deployment.

**Duration:** 2-3 hours

**Steps:**

1. **Verify end-to-end flow:**
   ```bash
   # Terminal 1: Start V2 API
   cd HealthAggregatorV2/src/Api
   dotnet run

   # Terminal 2: Start V2 SPA
   cd HealthAggregatorV2/src/Spa
   npm run dev

   # Terminal 3: Start V2 Functions (optional)
   cd HealthAggregatorV2/src/Functions
   func start
   ```

2. **Test complete user flow:**
   - Open browser to `http://localhost:5173`
   - Verify dashboard loads
   - Check that metrics are fetched from API
   - Verify data displays correctly
   - Check browser console for errors

3. **Apply database migrations:**
   ```bash
   cd HealthAggregatorV2/src/Api
   dotnet ef database update
   ```

4. **Seed initial data (optional):**
   - Create seed data script
   - Insert sample Sources, MetricTypes
   - Insert test Measurements

5. **Run all tests:**
   ```bash
   # Ensure you're in HealthAggregatorV2 folder
   cd HealthAggregatorV2

   # Run all .NET tests
   dotnet test

   # Run React tests
   cd src/Spa
   npm run test
   cd ..\..
   ```

6. **Build for production:**
   ```bash
   cd HealthAggregatorV2

   # Build API
   cd src/Api
   dotnet publish -c Release -o ./publish

   # Build SPA
   cd ../Spa
   npm run build

   # Build Functions
   cd ../Functions
   dotnet publish -c Release -o ./publish
   cd ..\..\..
   ```

7. **Create deployment documentation:**
   - Document Azure App Service setup for API
   - Document Azure Static Web Apps setup for SPA
   - Document Azure Functions deployment
   - Document database connection strings
   - Document environment variables

8. **Compare with existing system:**
   - Run V1 system at `http://localhost:7071/dashboard/` (HealthAggregatorApi)
   - Run V2 system at `http://localhost:5173` (HealthAggregatorV2)
   - Verify visual parity
   - Compare data accuracy
   - Check performance

**Verification:**
- ✅ All V2 components run simultaneously without conflicts
- ✅ Complete user flow works end-to-end
- ✅ All tests pass
- ✅ Production builds succeed
- ✅ No console errors in browser
- ✅ V2 API returns correct data
- ✅ Database migrations applied successfully
- ✅ V1 system still works independently

---

## Success Criteria ✅

After completing all phases, you should have:

1. ✅ **Domain Layer** - Compiles successfully
2. ✅ **Infrastructure Layer** - Compiles successfully with EF Core
3. ✅ **API** - Runs at localhost:5000 with Swagger UI
4. ✅ **React SPA** - Runs at localhost:5173 with matching design
5. ✅ **Functions** - Runs locally with timer trigger
6. ✅ **Tests** - All test projects compile and run
7. ✅ **Database** - Migrations applied successfully
8. ✅ **Integration** - All components work together
9. ✅ **Existing System** - Still works at `HealthAggregatorApi/`

---

## Troubleshooting

### Common Issues:

**Build Errors:**
- Check .NET 8 SDK is installed: `dotnet --version`
- Check Node.js version: `node --version`
- Restore packages: `dotnet restore`
- Clear build artifacts: `dotnet clean`

**Database Connection:**
- Verify connection string in appsettings.json
- Check Azure SQL firewall rules
- Test connection with SQL Server Management Studio

**Port Conflicts:**
- API default: 5000 (can change in launchSettings.json)
- SPA default: 5173 (can change in vite.config.ts)
- Functions default: 7071 (can change in host.json)
- Existing system: 7071 (may need to stop this first)

**CORS Errors:**
- Verify CORS policy in API Program.cs
- Check allowed origins match SPA port
- Ensure credentials: true if needed

**React Build Errors:**
- Delete node_modules and package-lock.json
- Run `npm install` again
- Check for TypeScript errors: `npm run type-check`

---

## Next Steps After Implementation

1. **Gradual Migration:**
   - Run both systems side-by-side
   - Redirect subset of users to new system
   - Monitor for issues
   - Gradually increase traffic to new system

2. **Data Migration:**
   - Export data from old system
   - Import into new database schema
   - Validate data integrity

3. **Monitoring:**
   - Set up Application Insights
   - Configure alerts
   - Monitor performance

4. **Documentation:**
   - API documentation (Swagger)
   - User guide
   - Developer onboarding docs

5. **Decommission Old System:**
   - Archive `HealthAggregatorApi/` folder
   - Update Azure resources
   - Clean up old deployments

---

## Quick Reference Commands

```bash
# Navigate to V2 folder
cd HealthAggregatorV2

# Build everything
dotnet build

# Run V2 API
cd src/Api && dotnet run

# Run V2 SPA
cd src/Spa && npm run dev

# Run V2 Functions
cd src/Functions && func start

# Run tests
dotnet test

# Create migration
cd src/Api && dotnet ef migrations add MigrationName

# Apply migration
cd src/Api && dotnet ef database update

# Build for production
dotnet publish -c Release
cd src/Spa && npm run build
```

---

## Contact & Support

If you encounter issues during implementation:

1. Check [docs/V2-STRUCTURE.md](V2-STRUCTURE.md) for HealthAggregatorV2 folder structure
2. Review specific plan files for detailed guidance
3. Check existing V1 system for reference implementations
4. Review git history for context

---

**Good luck with the implementation! 🚀**
