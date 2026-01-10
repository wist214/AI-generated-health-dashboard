# Backend API Implementation Plan (.NET 8 Minimal API)

## 1. Overview

This document outlines the implementation plan for the Health Aggregator Backend API using **.NET 8 Minimal API** architecture, following **SOLID**, **DRY**, and **KISS** principles with the latest Microsoft best practices.

### 1.1 Technology Stack

- **Framework**: .NET 8 Minimal API
- **Hosting**: Azure App Service (Free F1 tier)
- **Database Access**: Entity Framework Core 8
- **Dependency Injection**: Built-in ASP.NET Core DI
- **Monitoring**: Application Insights
- **Azure SQL Connection**: Managed Identity

### 1.2 Architecture Goals

- **SOLID Principles**: Each component has a single responsibility
- **DRY**: Shared logic extracted into reusable services
- **KISS**: Simple, straightforward implementation without over-engineering
- **Testability**: Designed for E2E testing with Playwright
- **Extensibility**: Plugin architecture for future data source integrations

---

## 2. Project Location and Structure

### 2.1 Important: New Implementation Location

**CRITICAL:** This new implementation should be created in a **separate folder** to avoid modifying the existing working Azure Functions application.

**Existing V1 location:** `HealthAggregatorApi/` (current Azure Functions + dashboard)

**New V2 location:** Create new folder structure:
```
HealthAggregatorV2/
├── src/
│   ├── Api/                          # .NET 8 Minimal API
│   ├── Domain/                       # Shared domain models
│   ├── Infrastructure/               # Shared infrastructure (EF Core, repositories)
│   ├── Functions/                    # Azure Functions sync (separate from V1)
│   └── Spa/                          # React SPA
└── tests/
    ├── Api.Tests/
    ├── Functions.Tests/
    └── Spa.Tests/
```

**Rationale:**
- Keep existing system operational during migration
- Enable side-by-side comparison
- Allow gradual migration and testing
- Preserve rollback capability

### 2.2 API Project Structure

```
HealthAggregatorV2/src/Api/
├── Api.csproj
├── Program.cs                          # Application entry point + DI configuration
├── appsettings.json
├── appsettings.Development.json
│
├── Endpoints/                          # Minimal API endpoint definitions (Vertical Slice)
│   ├── MetricsEndpoints.cs             # Metrics-related endpoints
│   ├── SourcesEndpoints.cs             # Data source status endpoints
│   └── DashboardEndpoints.cs           # Dashboard aggregation endpoints
│
├── Application/                        # Business logic layer
│   ├── Services/                       # Service implementations
│   │   ├── Interfaces/
│   │   │   ├── IMetricsService.cs
│   │   │   ├── IDashboardService.cs
│   │   │   └── ISourceStatusService.cs
│   │   ├── MetricsService.cs           # Metrics aggregation and querying
│   │   ├── DashboardService.cs         # Dashboard data composition
│   │   └── SourceStatusService.cs      # Data source status management
│   │
│   └── DTOs/                           # Data Transfer Objects
│       ├── Requests/
│       │   ├── GetMetricsRangeRequest.cs
│       │   └── GetMetricHistoryRequest.cs
│       └── Responses/
│           ├── MetricLatestDto.cs
│           ├── MetricRangeDto.cs
│           ├── SourceStatusDto.cs
│           └── DashboardSummaryDto.cs
│
├── Domain/                             # Domain layer (EF Core entities)
│   ├── Entities/                       # EF Core entity models
│   │   ├── Source.cs
│   │   ├── Measurement.cs
│   │   ├── DailySummary.cs
│   │   └── MetricType.cs
│   │
│   └── Repositories/                   # Repository interfaces
│       ├── IMetricsRepository.cs
│       ├── ISourceRepository.cs
│       └── IDailySummaryRepository.cs
│
├── Infrastructure/                     # Infrastructure layer
│   ├── Data/
│   │   ├── HealthDbContext.cs          # EF Core DbContext
│   │   ├── Configurations/             # EF Core entity configurations
│   │   │   ├── SourceConfiguration.cs
│   │   │   ├── MeasurementConfiguration.cs
│   │   │   └── DailySummaryConfiguration.cs
│   │   └── Migrations/                 # EF Core migrations
│   │
│   └── Repositories/                   # Repository implementations
│       ├── MetricsRepository.cs
│       ├── SourceRepository.cs
│       └── DailySummaryRepository.cs
│
├── Extensions/                         # Extension methods for clean setup
│   ├── ServiceCollectionExtensions.cs  # DI registration extensions
│   ├── EndpointRouteBuilderExtensions.cs # Endpoint mapping extensions
│   └── WebApplicationExtensions.cs     # Middleware/pipeline extensions
│
└── Middleware/                         # Custom middleware
    ├── ExceptionHandlingMiddleware.cs  # Global exception handling
    └── RequestLoggingMiddleware.cs     # Request/response logging
```

---

## 3. SOLID Principles Application

### 3.1 Single Responsibility Principle (SRP)

**Implementation:**
- **Endpoints**: Only handle HTTP request/response mapping
- **Services**: Only contain business logic for their domain
- **Repositories**: Only handle data access for specific entities
- **DTOs**: Only represent data shapes for API contracts

**Example:**
```csharp
// MetricsEndpoints.cs - ONLY handles HTTP concerns
public static class MetricsEndpoints
{
    public static void MapMetricsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/metrics")
            .WithTags("Metrics")
            .WithOpenApi();

        group.MapGet("/latest", GetLatestMetrics);
        group.MapGet("/range", GetMetricsInRange);
        group.MapGet("/{type}/history", GetMetricHistory);
    }

    private static async Task<IResult> GetLatestMetrics(
        IMetricsService service)
    {
        var result = await service.GetLatestMetricsAsync();
        return Results.Ok(result);
    }
}

// MetricsService.cs - ONLY contains business logic
public class MetricsService : IMetricsService
{
    private readonly IMetricsRepository _repository;
    private readonly ILogger<MetricsService> _logger;

    public MetricsService(
        IMetricsRepository repository,
        ILogger<MetricsService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<MetricLatestDto> GetLatestMetricsAsync()
    {
        // Business logic only
        var latestMeasurements = await _repository.GetLatestByTypeAsync();
        return MapToDto(latestMeasurements);
    }
}
```

### 3.2 Open/Closed Principle (OCP)

**Implementation:**
- Use **interfaces** for all services and repositories
- Design for extension through **strategy pattern** for data source adapters
- Use **middleware pipeline** for cross-cutting concerns

**Example:**
```csharp
// Open for extension through interface implementation
public interface IMetricsService
{
    Task<MetricLatestDto> GetLatestMetricsAsync();
    Task<IEnumerable<MetricRangeDto>> GetMetricsInRangeAsync(
        DateTime from, DateTime to);
}

// Closed for modification - new functionality via new implementations
public class CachedMetricsService : IMetricsService
{
    private readonly IMetricsService _innerService;
    private readonly IMemoryCache _cache;

    public CachedMetricsService(
        IMetricsService innerService,
        IMemoryCache cache)
    {
        _innerService = innerService;
        _cache = cache;
    }

    // Decorator pattern - adds caching without modifying original
}
```

### 3.3 Liskov Substitution Principle (LSP)

**Implementation:**
- All implementations of interfaces must be **fully substitutable**
- Repositories return consistent data contracts
- Services throw well-defined exceptions

**Example:**
```csharp
// Interface contract
public interface IMetricsRepository
{
    Task<IEnumerable<Measurement>> GetLatestByTypeAsync();
}

// Implementation 1: SQL Repository
public class MetricsRepository : IMetricsRepository
{
    public async Task<IEnumerable<Measurement>> GetLatestByTypeAsync()
    {
        // Returns IEnumerable<Measurement> - contract satisfied
    }
}

// Implementation 2: InMemory Repository (for testing)
public class InMemoryMetricsRepository : IMetricsRepository
{
    public async Task<IEnumerable<Measurement>> GetLatestByTypeAsync()
    {
        // Returns IEnumerable<Measurement> - contract satisfied
    }
}
```

### 3.4 Interface Segregation Principle (ISP)

**Implementation:**
- **Small, focused interfaces** instead of large ones
- Clients only depend on methods they use

**Example:**
```csharp
// BAD: Large interface
public interface IMetricsManager
{
    Task<MetricLatestDto> GetLatestMetricsAsync();
    Task InsertMetricAsync(Measurement metric);
    Task DeleteMetricAsync(long id);
    Task<IEnumerable<MetricRangeDto>> GetMetricsInRangeAsync(...);
}

// GOOD: Segregated interfaces
public interface IMetricsQueryService
{
    Task<MetricLatestDto> GetLatestMetricsAsync();
    Task<IEnumerable<MetricRangeDto>> GetMetricsInRangeAsync(...);
}

public interface IMetricsCommandService
{
    Task InsertMetricAsync(Measurement metric);
    Task DeleteMetricAsync(long id);
}
```

### 3.5 Dependency Inversion Principle (DIP)

**Implementation:**
- High-level modules (Services) depend on **abstractions** (Interfaces)
- Low-level modules (Repositories) also implement abstractions
- Dependencies injected via **constructor injection**

**Example:**
```csharp
// High-level module depends on abstraction
public class DashboardService : IDashboardService
{
    private readonly IMetricsRepository _metricsRepo;
    private readonly IDailySummaryRepository _summaryRepo;

    // Depends on interfaces, not concrete implementations
    public DashboardService(
        IMetricsRepository metricsRepo,
        IDailySummaryRepository summaryRepo)
    {
        _metricsRepo = metricsRepo;
        _summaryRepo = summaryRepo;
    }
}
```

---

## 4. DRY (Don't Repeat Yourself) Implementation

### 4.1 Extension Methods for DI Registration

```csharp
// Extensions/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<IMetricsService, MetricsService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ISourceStatusService, SourceStatusService>();

        return services;
    }

    public static IServiceCollection AddRepositories(
        this IServiceCollection services)
    {
        services.AddScoped<IMetricsRepository, MetricsRepository>();
        services.AddScoped<ISourceRepository, SourceRepository>();
        services.AddScoped<IDailySummaryRepository, DailySummaryRepository>();

        return services;
    }

    public static IServiceCollection AddDatabaseContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<HealthDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING");
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);

                sqlOptions.CommandTimeout(30);
            });
        });

        return services;
    }
}

// Usage in Program.cs - DRY!
builder.Services
    .AddDatabaseContext(builder.Configuration)
    .AddRepositories()
    .AddApplicationServices();
```

### 4.2 Result Pattern for Error Handling

```csharp
// Shared Result<T> type to avoid repetitive try-catch blocks
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}

// Service usage
public async Task<Result<MetricLatestDto>> GetLatestMetricsAsync()
{
    try
    {
        var data = await _repository.GetLatestByTypeAsync();
        return Result<MetricLatestDto>.Success(MapToDto(data));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get latest metrics");
        return Result<MetricLatestDto>.Failure(ex.Message);
    }
}
```

---

## 5. KISS (Keep It Simple, Stupid) Implementation

### 5.1 Minimal API Simplicity

**Principle:** Use Minimal API to avoid controller boilerplate

```csharp
// Simple, direct endpoint mapping
app.MapGet("/api/metrics/latest", async (IMetricsService service) =>
{
    var result = await service.GetLatestMetricsAsync();
    return Results.Ok(result);
});

// With validation - still simple
app.MapGet("/api/metrics/range", async (
    [FromQuery] DateTime? from,
    [FromQuery] DateTime? to,
    IMetricsService service) =>
{
    if (!from.HasValue || !to.HasValue)
        return Results.BadRequest("Both 'from' and 'to' parameters are required");

    var result = await service.GetMetricsInRangeAsync(from.Value, to.Value);
    return Results.Ok(result);
});
```

### 5.2 Avoid Over-Abstraction

**Don't:**
```csharp
// Over-engineered factory pattern
public interface IServiceFactory
{
    IMetricsService CreateMetricsService();
}

public class ServiceFactory : IServiceFactory { ... }
```

**Do:**
```csharp
// Simple DI - let the container handle it
builder.Services.AddScoped<IMetricsService, MetricsService>();
```

### 5.3 Simple DTOs

```csharp
// Simple, clear DTOs
public record MetricLatestDto(
    string MetricType,
    double Value,
    string Unit,
    DateTime Timestamp,
    string SourceName
);

// Record types are immutable and concise
public record DashboardSummaryDto
{
    public int? SleepScore { get; init; }
    public int? ReadinessScore { get; init; }
    public double? Weight { get; init; }
    public int? Steps { get; init; }
    public DateTime LastUpdated { get; init; }
}
```

---

## 6. Database Access with EF Core 8

### 6.1 DbContext Configuration

```csharp
// Infrastructure/Data/HealthDbContext.cs
public class HealthDbContext : DbContext
{
    public HealthDbContext(DbContextOptions<HealthDbContext> options)
        : base(options)
    {
    }

    public DbSet<Source> Sources => Set<Source>();
    public DbSet<Measurement> Measurements => Set<Measurement>();
    public DbSet<DailySummary> DailySummaries => Set<DailySummary>();
    public DbSet<MetricType> MetricTypes => Set<MetricType>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(HealthDbContext).Assembly);
    }
}
```

### 6.2 Entity Configuration (Fluent API)

```csharp
// Infrastructure/Data/Configurations/MeasurementConfiguration.cs
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

        builder.HasIndex(m => new { m.SourceId, m.Timestamp })
            .HasDatabaseName("IX_Measurements_Source");

        // Relationship
        builder.HasOne(m => m.Source)
            .WithMany(s => s.Measurements)
            .HasForeignKey(m => m.SourceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### 6.3 Repository Pattern

```csharp
// Infrastructure/Repositories/MetricsRepository.cs
public class MetricsRepository : IMetricsRepository
{
    private readonly HealthDbContext _context;

    public MetricsRepository(HealthDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Measurement>> GetLatestByTypeAsync()
    {
        return await _context.Measurements
            .Include(m => m.Source)
            .GroupBy(m => m.MetricType)
            .Select(g => g.OrderByDescending(m => m.Timestamp).First())
            .AsNoTracking() // Performance: read-only
            .ToListAsync();
    }

    public async Task<IEnumerable<Measurement>> GetByTypeInRangeAsync(
        string metricType,
        DateTime from,
        DateTime to)
    {
        return await _context.Measurements
            .Where(m => m.MetricType == metricType
                     && m.Timestamp >= from
                     && m.Timestamp <= to)
            .OrderBy(m => m.Timestamp)
            .AsNoTracking()
            .ToListAsync();
    }
}
```

---

## 7. Program.cs Configuration (Entry Point)

```csharp
using HealthAggregatorV2.Api.Extensions;
using HealthAggregatorV2.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ===== Service Registration =====

// Database
builder.Services.AddDatabaseContext(builder.Configuration);

// Repositories & Services
builder.Services
    .AddRepositories()
    .AddApplicationServices();

// API Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Health Aggregator API",
        Version = "v1"
    });
});

// CORS for SPA (public API - no authentication)
builder.Services.AddCors(options =>
{
    options.AddPolicy("SpaPolicy", policy =>
    {
        policy.WithOrigins(
                builder.Configuration["AllowedOrigins:SPA"] ?? "http://localhost:3000",
                builder.Configuration["AllowedOrigins:SWA"] ?? "https://*.azurestaticapps.net"
            )
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<HealthDbContext>();

// ===== Build Application =====

var app = builder.Build();

// ===== Middleware Pipeline =====

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("SpaPolicy");

// Custom middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

// Health checks
app.MapHealthChecks("/health");

// Map endpoints
app.MapMetricsEndpoints();
app.MapSourcesEndpoints();
app.MapDashboardEndpoints();

app.Run();

// Make Program class accessible for testing
public partial class Program { }
```

---

## 8. Testing Strategy for E2E with Playwright

### 8.1 Test-Friendly Design

**Key Principles:**
1. **Stable endpoints**: Versioned API routes
2. **Deterministic responses**: Seeded test data
3. **Idempotent operations**: Safe to run multiple times
4. **Clear error messages**: Detailed validation errors

### 8.2 Test Data Seeding

```csharp
// Infrastructure/Data/DbInitializer.cs
public static class DbInitializer
{
    public static async Task SeedTestDataAsync(HealthDbContext context)
    {
        if (await context.Sources.AnyAsync())
            return; // Already seeded

        var ouraSource = new Source
        {
            ProviderName = "Oura",
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Sources.Add(ouraSource);
        await context.SaveChangesAsync();

        context.Measurements.AddRange(
            new Measurement
            {
                MetricType = "sleep_score",
                Value = 85,
                Unit = "score",
                Timestamp = DateTime.UtcNow.AddDays(-1),
                SourceId = ouraSource.Id,
                CreatedAt = DateTime.UtcNow
            }
        );

        await context.SaveChangesAsync();
    }
}
```

### 8.3 Playwright Test Example

```csharp
// Tests/E2E/MetricsApiTests.cs
[TestClass]
public class MetricsApiTests : PlaywrightTest
{
    private IAPIRequestContext _apiContext = null!;
    private const string BaseUrl = "http://localhost:5000";

    [TestInitialize]
    public async Task Setup()
    {
        await CreateAPIRequestContext();
    }

    private async Task CreateAPIRequestContext()
    {
        _apiContext = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = BaseUrl
        });
    }

    [TestMethod]
    public async Task GetLatestMetrics_ReturnsOk()
    {
        // Act
        var response = await _apiContext.GetAsync("/api/metrics/latest");

        // Assert
        Assert.IsTrue(response.Ok);
        Assert.AreEqual(200, response.Status);

        var json = await response.JsonAsync();
        Assert.IsNotNull(json);
    }

    [TestMethod]
    public async Task GetMetricsInRange_WithValidDates_ReturnsMetrics()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-7).ToString("o");
        var to = DateTime.UtcNow.ToString("o");

        // Act
        var response = await _apiContext.GetAsync(
            $"/api/metrics/range?from={from}&to={to}");

        // Assert
        Assert.IsTrue(response.Ok);
        var json = await response.JsonAsync();
        Assert.IsNotNull(json);
    }
}
```

---

## 9. Performance Best Practices

### 9.1 DbContext Pooling

```csharp
builder.Services.AddDbContextPool<HealthDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
    });
}, poolSize: 128); // Pool size for high-traffic scenarios
```

### 9.2 Response Caching

```csharp
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder.Cache());
});

app.UseOutputCache();

// Apply to endpoints
app.MapGet("/api/metrics/latest", async (IMetricsService service) =>
{
    return Results.Ok(await service.GetLatestMetricsAsync());
}).CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)));
```

### 9.3 Async All The Way

```csharp
// ALWAYS use async methods for I/O operations
public async Task<IEnumerable<Measurement>> GetLatestByTypeAsync()
{
    return await _context.Measurements
        .AsNoTracking()
        .ToListAsync(); // NOT .ToList()!
}
```

---

## 10. Extensibility Architecture (Plugin Pattern)

### 10.1 Data Source Adapter Interface

```csharp
// Application/Integrations/IDataSourceAdapter.cs
public interface IDataSourceAdapter
{
    string SourceName { get; }
    Task<IEnumerable<Measurement>> FetchLatestDataAsync();
    Task<bool> ValidateConnectionAsync();
}

// Future extensibility: Add new source without modifying existing code
public class FitbitAdapter : IDataSourceAdapter
{
    public string SourceName => "Fitbit";

    public async Task<IEnumerable<Measurement>> FetchLatestDataAsync()
    {
        // Fitbit API integration
        return await Task.FromResult(Array.Empty<Measurement>());
    }

    public async Task<bool> ValidateConnectionAsync()
    {
        // Validate Fitbit connection
        return await Task.FromResult(true);
    }
}

// Register in DI
builder.Services.AddScoped<IDataSourceAdapter, OuraAdapter>();
builder.Services.AddScoped<IDataSourceAdapter, FitbitAdapter>(); // NEW!
```

### 10.2 Strategy Pattern for Data Aggregation

```csharp
public interface IAggregationStrategy
{
    Task<DashboardSummaryDto> AggregateAsync(IEnumerable<Measurement> measurements);
}

public class DailyAverageStrategy : IAggregationStrategy { ... }
public class WeeklyTrendStrategy : IAggregationStrategy { ... }

// Inject collection of strategies
public class DashboardService
{
    private readonly IEnumerable<IAggregationStrategy> _strategies;

    public DashboardService(IEnumerable<IAggregationStrategy> strategies)
    {
        _strategies = strategies;
    }
}
```

---

## 11. Gray Areas / Questions

The following areas require clarification before finalizing implementation:

### 11.1 Rate Limiting

**Question:** Should we implement rate limiting on the Free F1 App Service tier?
- **Option 1:** No rate limiting (simplest)
- **Option 2:** Simple in-memory rate limiting middleware
- **Option 3:** Azure API Management (adds cost)

**Impact:** Affects API availability and potential abuse protection.

### 11.2 API Versioning Strategy

**Decision:** No versioning initially (KISS - Option 1)
- Start with simple `/api/metrics` endpoints
- Can add versioning later if breaking changes are needed
- Keeps initial implementation simple

**Impact:** Endpoints will use `/api/metrics/*` without version prefix.

---

## 12. Implementation Checklist

- [ ] Create solution and project structure
- [ ] Install NuGet packages (EF Core, Azure Identity, Application Insights)
- [ ] Define entity models and configurations
- [ ] Create initial EF Core migration
- [ ] Implement repository interfaces and classes
- [ ] Implement service interfaces and classes
- [ ] Define DTOs for all endpoints
- [ ] Create endpoint mappings (Minimal API)
- [ ] Configure DI in `Program.cs`
- [ ] Add exception handling middleware
- [ ] Configure CORS for SPA
- [ ] Add Application Insights telemetry
- [ ] Configure health checks
- [ ] Write E2E tests with Playwright
- [ ] Test locally with Azurite and LocalDB
- [ ] Document API with Swagger/OpenAPI
- [ ] Deploy to Azure App Service
- [ ] Configure Managed Identity for Azure SQL
- [ ] Verify in production

---

## 13. References

### Microsoft Documentation
- [ASP.NET Core Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [Entity Framework Core Best Practices](https://learn.microsoft.com/en-us/ef/core/performance/)
- [Dependency Injection in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [SOLID Principles in .NET](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles)

### Best Practices
- Minimize bundle size and overhead
- Use `AsNoTracking()` for read-only queries
- Enable connection resiliency for Azure SQL
- Use async/await consistently
- Follow RESTful API conventions
