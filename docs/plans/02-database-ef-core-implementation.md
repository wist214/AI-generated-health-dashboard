# Database and EF Core 8 Implementation Plan

## 1. Overview

This document outlines the comprehensive implementation plan for the Health Aggregator database layer using **Entity Framework Core 8** with **Azure SQL Database**, following **SOLID**, **DRY**, and **KISS** principles with Microsoft best practices for 2024-2026.

### 1.1 Technology Stack

- **ORM**: Entity Framework Core 8.0
- **Database**: Azure SQL Database (Free tier - 32 MB)
- **Migrations**: EF Core Migrations
- **Connection Management**: Azure Identity with Managed Identity
- **Performance**: Query optimization, indexing strategy, connection pooling
- **Resilience**: Retry logic with exponential backoff

### 1.2 Architecture Goals

- **Data Integrity**: Strong foreign keys, constraints, and validation
- **Performance**: Optimized indexes for common query patterns
- **Scalability**: Efficient queries with AsNoTracking for read operations
- **Maintainability**: Clear entity relationships and configurations
- **Testability**: Repository pattern with in-memory database support
- **Auditability**: Created/Updated timestamps on all entities

---

## 2. Project Location

**IMPORTANT:** This database implementation will be part of the V2 refactored system in the `HealthAggregatorV2/` folder.

**Location:**
- Domain models: `HealthAggregatorV2/src/Domain/`
- Infrastructure (EF Core, repositories): `HealthAggregatorV2/src/Infrastructure/`
- Migrations will be in: `HealthAggregatorV2/src/Api/` (API project)

**Existing V1 system:** `HealthAggregatorApi/` remains untouched and operational.

**Database:** Both V1 and V2 systems can initially share the same Azure SQL database (different tables/schema if needed), or use separate databases during migration period.

---

## 3. Database Schema Design

### 3.1 Entity Relationship Diagram

```
┌─────────────────┐         ┌──────────────────┐         ┌─────────────────┐
│     Sources     │1       *│   Measurements   │*       1│   MetricTypes   │
├─────────────────┤◄────────┤──────────────────┤◄────────┤─────────────────┤
│ Id (PK)         │         │ Id (PK)          │         │ Id (PK)         │
│ ProviderName    │         │ MetricTypeId (FK)│         │ Name            │
│ IsEnabled       │         │ SourceId (FK)    │         │ Unit            │
│ LastSyncedAt    │         │ Value            │         │ Category        │
│ ApiKeyEncrypted │         │ Timestamp        │         │ Description     │
│ CreatedAt       │         │ CreatedAt        │         │ CreatedAt       │
│ UpdatedAt       │         │ UpdatedAt        │         │ UpdatedAt       │
└─────────────────┘         └──────────────────┘         └─────────────────┘
                                     │
                                     │
                            ┌────────▼───────────┐
                            │  DailySummaries   │
                            ├───────────────────┤
                            │ Id (PK)           │
                            │ Date              │
                            │ SleepScore        │
                            │ ReadinessScore    │
                            │ ActivityScore     │
                            │ Steps             │
                            │ CaloriesBurned    │
                            │ Weight            │
                            │ CreatedAt         │
                            │ UpdatedAt         │
                            └───────────────────┘
```

### 2.2 Table Definitions

#### Sources Table
```sql
CREATE TABLE Sources (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    ProviderName NVARCHAR(100) NOT NULL,
    IsEnabled BIT NOT NULL DEFAULT 1,
    LastSyncedAt DATETIME2(7) NULL,
    ApiKeyEncrypted NVARCHAR(500) NULL,
    ConfigurationJson NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT UQ_Sources_ProviderName UNIQUE (ProviderName)
);

CREATE INDEX IX_Sources_IsEnabled ON Sources(IsEnabled);
```

#### MetricTypes Table (Reference Data)
```sql
CREATE TABLE MetricTypes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Unit NVARCHAR(50) NOT NULL,
    Category NVARCHAR(50) NOT NULL, -- 'Sleep', 'Activity', 'Body', 'Nutrition'
    Description NVARCHAR(500) NULL,
    MinValue FLOAT NULL,
    MaxValue FLOAT NULL,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT UQ_MetricTypes_Name UNIQUE (Name)
);

CREATE INDEX IX_MetricTypes_Category ON MetricTypes(Category);
```

#### Measurements Table (Hot Path - High Volume)
```sql
CREATE TABLE Measurements (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    MetricTypeId INT NOT NULL,
    SourceId BIGINT NOT NULL,
    Value FLOAT NOT NULL,
    Timestamp DATETIME2(7) NOT NULL,
    RawDataJson NVARCHAR(MAX) NULL, -- Store original API response
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_Measurements_MetricTypes FOREIGN KEY (MetricTypeId)
        REFERENCES MetricTypes(Id),
    CONSTRAINT FK_Measurements_Sources FOREIGN KEY (SourceId)
        REFERENCES Sources(Id),

    -- Prevent duplicate measurements
    CONSTRAINT UQ_Measurements_Unique UNIQUE (MetricTypeId, SourceId, Timestamp)
);

-- Optimized for queries: "Get latest metric by type"
CREATE INDEX IX_Measurements_MetricType_Timestamp
    ON Measurements(MetricTypeId, Timestamp DESC)
    INCLUDE (Value, SourceId);

-- Optimized for queries: "Get metrics in date range"
CREATE INDEX IX_Measurements_Timestamp
    ON Measurements(Timestamp DESC);

-- Optimized for queries: "Get metrics by source"
CREATE INDEX IX_Measurements_Source_Timestamp
    ON Measurements(SourceId, Timestamp DESC);
```

#### DailySummaries Table (Pre-aggregated Data)
```sql
CREATE TABLE DailySummaries (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Date DATE NOT NULL,
    SleepScore INT NULL,
    ReadinessScore INT NULL,
    ActivityScore INT NULL,
    Steps INT NULL,
    CaloriesBurned INT NULL,
    Weight FLOAT NULL,
    BodyFatPercentage FLOAT NULL,
    HeartRateAvg INT NULL,
    HeartRateMin INT NULL,
    HeartRateMax INT NULL,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT UQ_DailySummaries_Date UNIQUE (Date)
);

CREATE INDEX IX_DailySummaries_Date ON DailySummaries(Date DESC);
```

---

## 3. EF Core 8 Entity Models

### 3.1 Base Entity for Audit Tracking

```csharp
// Domain/Entities/BaseEntity.cs
public abstract class BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

```

### 3.2 Source Entity

```csharp
// Domain/Entities/Source.cs
public class Source : BaseEntity
{
    public long Id { get; set; }

    public string ProviderName { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public DateTime? LastSyncedAt { get; set; }

    // Encrypted using Azure Key Vault or Data Protection API
    public string? ApiKeyEncrypted { get; set; }

    // JSON configuration for provider-specific settings
    public string? ConfigurationJson { get; set; }

    // Navigation properties
    public virtual ICollection<Measurement> Measurements { get; set; } = new List<Measurement>();
}
```

### 3.3 MetricType Entity (Reference Data)

```csharp
// Domain/Entities/MetricType.cs
public class MetricType : BaseEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty; // e.g., "sleep_score"

    public string Unit { get; set; } = string.Empty; // e.g., "score", "steps", "kg"

    public string Category { get; set; } = string.Empty; // Sleep, Activity, Body, Nutrition

    public string? Description { get; set; }

    public double? MinValue { get; set; }

    public double? MaxValue { get; set; }

    // Navigation properties
    public virtual ICollection<Measurement> Measurements { get; set; } = new List<Measurement>();
}
```

### 3.4 Measurement Entity

```csharp
// Domain/Entities/Measurement.cs
public class Measurement : BaseEntity
{
    public long Id { get; set; }

    public int MetricTypeId { get; set; }

    public long SourceId { get; set; }

    public double Value { get; set; }

    public DateTime Timestamp { get; set; }

    // Store original API response for debugging/audit
    public string? RawDataJson { get; set; }

    // Navigation properties
    public virtual MetricType MetricType { get; set; } = null!;
    public virtual Source Source { get; set; } = null!;
}
```

### 3.5 DailySummary Entity

```csharp
// Domain/Entities/DailySummary.cs
public class DailySummary : BaseEntity
{
    public long Id { get; set; }

    public DateOnly Date { get; set; }

    // Oura scores
    public int? SleepScore { get; set; }
    public int? ReadinessScore { get; set; }
    public int? ActivityScore { get; set; }

    // Activity metrics
    public int? Steps { get; set; }
    public int? CaloriesBurned { get; set; }

    // Body metrics
    public double? Weight { get; set; }
    public double? BodyFatPercentage { get; set; }

    // Heart rate
    public int? HeartRateAvg { get; set; }
    public int? HeartRateMin { get; set; }
    public int? HeartRateMax { get; set; }
}
```

---

## 4. EF Core Entity Configurations (Fluent API)

### 4.1 BaseEntityConfiguration (DRY Principle)

```csharp
// Infrastructure/Data/Configurations/BaseEntityConfiguration.cs
public abstract class BaseEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        // Apply common audit fields
        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
    }
}
```

### 4.2 SourceConfiguration

```csharp
// Infrastructure/Data/Configurations/SourceConfiguration.cs
public class SourceConfiguration : BaseEntityConfiguration<Source>
{
    public override void Configure(EntityTypeBuilder<Source> builder)
    {
        base.Configure(builder);

        builder.ToTable("Sources");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.ProviderName)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(s => s.ProviderName)
            .IsUnique()
            .HasDatabaseName("UQ_Sources_ProviderName");

        builder.Property(s => s.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.ApiKeyEncrypted)
            .HasMaxLength(500);

        builder.Property(s => s.ConfigurationJson)
            .HasColumnType("NVARCHAR(MAX)");

        // Index for active sources
        builder.HasIndex(s => s.IsEnabled)
            .HasDatabaseName("IX_Sources_IsEnabled");

        // Navigation
        builder.HasMany(s => s.Measurements)
            .WithOne(m => m.Source)
            .HasForeignKey(m => m.SourceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### 4.3 MetricTypeConfiguration

```csharp
// Infrastructure/Data/Configurations/MetricTypeConfiguration.cs
public class MetricTypeConfiguration : BaseEntityConfiguration<MetricType>
{
    public override void Configure(EntityTypeBuilder<MetricType> builder)
    {
        base.Configure(builder);

        builder.ToTable("MetricTypes");

        builder.HasKey(mt => mt.Id);

        builder.Property(mt => mt.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(mt => mt.Name)
            .IsUnique()
            .HasDatabaseName("UQ_MetricTypes_Name");

        builder.Property(mt => mt.Unit)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(mt => mt.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(mt => mt.Category)
            .HasDatabaseName("IX_MetricTypes_Category");

        builder.Property(mt => mt.Description)
            .HasMaxLength(500);

        builder.Property(mt => mt.MinValue)
            .HasColumnType("FLOAT");

        builder.Property(mt => mt.MaxValue)
            .HasColumnType("FLOAT");

        // Navigation
        builder.HasMany(mt => mt.Measurements)
            .WithOne(m => m.MetricType)
            .HasForeignKey(m => m.MetricTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### 4.4 MeasurementConfiguration (Performance-Critical)

```csharp
// Infrastructure/Data/Configurations/MeasurementConfiguration.cs
public class MeasurementConfiguration : BaseEntityConfiguration<Measurement>
{
    public override void Configure(EntityTypeBuilder<Measurement> builder)
    {
        base.Configure(builder);

        builder.ToTable("Measurements");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Value)
            .IsRequired()
            .HasColumnType("FLOAT");

        builder.Property(m => m.Timestamp)
            .IsRequired()
            .HasColumnType("DATETIME2(7)");

        builder.Property(m => m.RawDataJson)
            .HasColumnType("NVARCHAR(MAX)");

        // Unique constraint to prevent duplicates
        builder.HasIndex(m => new { m.MetricTypeId, m.SourceId, m.Timestamp })
            .IsUnique()
            .HasDatabaseName("UQ_Measurements_Unique");

        // Performance indexes
        builder.HasIndex(m => new { m.MetricTypeId, m.Timestamp })
            .IsDescending(false, true) // MetricTypeId ASC, Timestamp DESC
            .HasDatabaseName("IX_Measurements_MetricType_Timestamp")
            .IncludeProperties(m => new { m.Value, m.SourceId });

        builder.HasIndex(m => m.Timestamp)
            .IsDescending()
            .HasDatabaseName("IX_Measurements_Timestamp");

        builder.HasIndex(m => new { m.SourceId, m.Timestamp })
            .IsDescending(false, true)
            .HasDatabaseName("IX_Measurements_Source_Timestamp");

        // Relationships
        builder.HasOne(m => m.MetricType)
            .WithMany(mt => mt.Measurements)
            .HasForeignKey(m => m.MetricTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Source)
            .WithMany(s => s.Measurements)
            .HasForeignKey(m => m.SourceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### 4.5 DailySummaryConfiguration

```csharp
// Infrastructure/Data/Configurations/DailySummaryConfiguration.cs
public class DailySummaryConfiguration : BaseEntityConfiguration<DailySummary>
{
    public override void Configure(EntityTypeBuilder<DailySummary> builder)
    {
        base.Configure(builder);

        builder.ToTable("DailySummaries");

        builder.HasKey(ds => ds.Id);

        builder.Property(ds => ds.Date)
            .IsRequired()
            .HasColumnType("DATE");

        builder.HasIndex(ds => ds.Date)
            .IsUnique()
            .IsDescending()
            .HasDatabaseName("UQ_DailySummaries_Date");

        builder.Property(ds => ds.Weight)
            .HasColumnType("FLOAT");

        builder.Property(ds => ds.BodyFatPercentage)
            .HasColumnType("FLOAT");
    }
}
```

---

## 5. DbContext Configuration

### 5.1 HealthDbContext

```csharp
// Infrastructure/Data/HealthDbContext.cs
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

        // Apply all entity configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HealthDbContext).Assembly);

        // Seed reference data
        SeedMetricTypes(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Automatically update UpdatedAt timestamp
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    private static void SeedMetricTypes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MetricType>().HasData(
            new MetricType { Id = 1, Name = "sleep_score", Unit = "score", Category = "Sleep",
                MinValue = 0, MaxValue = 100, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new MetricType { Id = 2, Name = "readiness_score", Unit = "score", Category = "Sleep",
                MinValue = 0, MaxValue = 100, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new MetricType { Id = 3, Name = "activity_score", Unit = "score", Category = "Activity",
                MinValue = 0, MaxValue = 100, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new MetricType { Id = 4, Name = "steps", Unit = "steps", Category = "Activity",
                MinValue = 0, MaxValue = 100000, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new MetricType { Id = 5, Name = "weight", Unit = "kg", Category = "Body",
                MinValue = 20, MaxValue = 300, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new MetricType { Id = 6, Name = "body_fat", Unit = "%", Category = "Body",
                MinValue = 3, MaxValue = 70, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new MetricType { Id = 7, Name = "heart_rate", Unit = "bpm", Category = "Activity",
                MinValue = 30, MaxValue = 220, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new MetricType { Id = 8, Name = "calories_burned", Unit = "kcal", Category = "Activity",
                MinValue = 0, MaxValue = 10000, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new MetricType { Id = 9, Name = "protein", Unit = "g", Category = "Nutrition",
                MinValue = 0, MaxValue = 500, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new MetricType { Id = 10, Name = "carbs", Unit = "g", Category = "Nutrition",
                MinValue = 0, MaxValue = 1000, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
    }
}
```

### 5.2 DbContext Registration with Connection Resiliency

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

            options.UseSqlServer(connectionString, sqlServerOptions =>
            {
                // Connection resiliency for transient failures
                sqlServerOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);

                // Command timeout
                sqlServerOptions.CommandTimeout(60);

                // Migration assembly (if in separate project)
                sqlServerOptions.MigrationsAssembly("HealthAggregatorV2.Api");

                // Enable split queries for better performance
                sqlServerOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });

            // Development optimizations
            if (configuration.GetValue<bool>("EnableSensitiveDataLogging"))
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }

            // Query tracking behavior
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        return services;
    }
}
```

---

## 6. Repository Pattern Implementation

### 6.1 Generic Repository Interface

```csharp
// Domain/Repositories/IRepository.cs
public interface IRepository<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
```

### 6.2 Measurements Repository

```csharp
// Domain/Repositories/IMeasurementsRepository.cs
public interface IMeasurementsRepository : IRepository<Measurement>
{
    Task<IEnumerable<Measurement>> GetLatestByMetricTypeAsync(
        string metricType,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Measurement>> GetByMetricTypeInRangeAsync(
        string metricType,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<Measurement?> GetLatestByMetricTypeAndSourceAsync(
        string metricType,
        long sourceId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        int metricTypeId,
        long sourceId,
        DateTime timestamp,
        CancellationToken cancellationToken = default);
}

// Infrastructure/Repositories/MeasurementsRepository.cs
public class MeasurementsRepository : IMeasurementsRepository
{
    private readonly HealthDbContext _context;
    private readonly ILogger<MeasurementsRepository> _logger;

    public MeasurementsRepository(
        HealthDbContext context,
        ILogger<MeasurementsRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Measurement>> GetLatestByMetricTypeAsync(
        string metricType,
        CancellationToken cancellationToken = default)
    {
        return await _context.Measurements
            .Include(m => m.MetricType)
            .Include(m => m.Source)
            .Where(m => m.MetricType.Name == metricType)
            .OrderByDescending(m => m.Timestamp)
            .Take(1)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Measurement>> GetByMetricTypeInRangeAsync(
        string metricType,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await _context.Measurements
            .Include(m => m.MetricType)
            .Include(m => m.Source)
            .Where(m => m.MetricType.Name == metricType
                     && m.Timestamp >= from
                     && m.Timestamp <= to)
            .OrderBy(m => m.Timestamp)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Measurement?> GetLatestByMetricTypeAndSourceAsync(
        string metricType,
        long sourceId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Measurements
            .Include(m => m.MetricType)
            .Where(m => m.MetricType.Name == metricType && m.SourceId == sourceId)
            .OrderByDescending(m => m.Timestamp)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        int metricTypeId,
        long sourceId,
        DateTime timestamp,
        CancellationToken cancellationToken = default)
    {
        return await _context.Measurements
            .AsNoTracking()
            .AnyAsync(m => m.MetricTypeId == metricTypeId
                        && m.SourceId == sourceId
                        && m.Timestamp == timestamp,
                cancellationToken);
    }

    public async Task<Measurement?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Measurements
            .Include(m => m.MetricType)
            .Include(m => m.Source)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Measurement>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Measurements
            .Include(m => m.MetricType)
            .Include(m => m.Source)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Measurement> AddAsync(Measurement entity, CancellationToken cancellationToken = default)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.Measurements.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<Measurement> entities, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entity in entities)
        {
            entity.CreatedAt = now;
            entity.UpdatedAt = now;
        }

        await _context.Measurements.AddRangeAsync(entities, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Measurement entity, CancellationToken cancellationToken = default)
    {
        _context.Measurements.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Measurement entity, CancellationToken cancellationToken = default)
    {
        _context.Measurements.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

---

## 7. Migration Strategy

### 7.1 Initial Migration

```bash
# Create initial migration
dotnet ef migrations add InitialCreate -o Infrastructure/Data/Migrations

# Review generated migration code
# Verify Up() and Down() methods

# Apply migration to local database
dotnet ef database update

# Generate SQL script for production
dotnet ef migrations script -o migrate.sql
```

### 7.2 Migration Best Practices

```csharp
// Infrastructure/Data/Migrations/20260110000000_InitialCreate.cs
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create tables
        migrationBuilder.CreateTable(
            name: "Sources",
            columns: table => new
            {
                Id = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ProviderName = table.Column<string>(maxLength: 100, nullable: false),
                IsEnabled = table.Column<bool>(nullable: false, defaultValue: true),
                LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                ApiKeyEncrypted = table.Column<string>(maxLength: 500, nullable: true),
                ConfigurationJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Sources", x => x.Id);
            });

        // Create indexes
        migrationBuilder.CreateIndex(
            name: "UQ_Sources_ProviderName",
            table: "Sources",
            column: "ProviderName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Sources_IsEnabled",
            table: "Sources",
            column: "IsEnabled",
            filter: "[IsDeleted] = 0");

        // Additional tables...
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("Measurements");
        migrationBuilder.DropTable("DailySummaries");
        migrationBuilder.DropTable("MetricTypes");
        migrationBuilder.DropTable("Sources");
    }
}
```

### 7.3 Migration Automation in CI/CD

```yaml
# .github/workflows/deploy-api.yml
- name: Generate EF Core Migration Script
  run: |
    dotnet tool install --global dotnet-ef
    dotnet ef migrations script --idempotent --output migrate.sql --project HealthAggregatorV2/src/Api

- name: Apply Database Migration
  uses: azure/sql-action@v2
  with:
    connection-string: ${{ secrets.AZURE_SQL_CONNECTION_STRING }}
    path: migrate.sql
```

---

## 8. Performance Optimization

### 8.1 Query Optimization Patterns

```csharp
// GOOD: Projection to avoid loading entire entities
public async Task<IEnumerable<MetricSummaryDto>> GetMetricSummariesAsync()
{
    return await _context.Measurements
        .Select(m => new MetricSummaryDto
        {
            MetricType = m.MetricType.Name,
            Value = m.Value,
            Timestamp = m.Timestamp
        })
        .AsNoTracking()
        .ToListAsync();
}

// GOOD: AsNoTracking for read-only queries
public async Task<IEnumerable<Measurement>> GetLatestMetricsAsync()
{
    return await _context.Measurements
        .AsNoTracking() // No change tracking overhead
        .OrderByDescending(m => m.Timestamp)
        .Take(10)
        .ToListAsync();
}

// BAD: Loading unnecessary data
public async Task<IEnumerable<Measurement>> GetMetricsBad()
{
    return await _context.Measurements
        .Include(m => m.Source)
        .Include(m => m.MetricType)
        .ToListAsync(); // Loads entire table!
}
```

### 8.2 Index Strategy

```csharp
// Composite index for common query pattern
builder.HasIndex(m => new { m.MetricTypeId, m.Timestamp })
    .IncludeProperties(m => new { m.Value, m.SourceId }); // Covering index

// Index for active sources
builder.HasIndex(s => s.IsEnabled);
```

### 8.3 Connection Pooling

```csharp
// appsettings.json
{
  "ConnectionStrings": {
    "AZURE_SQL_CONNECTIONSTRING": "Server=tcp:myserver.database.windows.net;Database=healthdb;Max Pool Size=100;Min Pool Size=10;"
  }
}
```

### 8.4 Batch Operations

```csharp
// Efficient bulk insert
public async Task ImportMeasurementsAsync(IEnumerable<Measurement> measurements)
{
    var batches = measurements.Chunk(1000); // Process in batches of 1000

    foreach (var batch in batches)
    {
        await _context.Measurements.AddRangeAsync(batch);
        await _context.SaveChangesAsync();
    }
}
```

---

## 9. Azure SQL Free Tier Optimization

### 9.1 Database Size Management

**Free Tier Limit: 32 MB**

```csharp
// Strategy 1: Archive old measurements
public async Task ArchiveOldMeasurementsAsync(int retentionDays = 90)
{
    var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

    var oldMeasurements = await _context.Measurements
        .Where(m => m.Timestamp < cutoffDate)
        .ToListAsync();

    // Option 1: Delete
    _context.Measurements.RemoveRange(oldMeasurements);

    // Option 2: Export to Azure Blob Storage then delete
    // await ExportToBlobStorageAsync(oldMeasurements);
    // _context.Measurements.RemoveRange(oldMeasurements);

    await _context.SaveChangesAsync();
}

// Strategy 2: Use DailySummaries instead of raw measurements
public async Task AggregateToSummariesAsync()
{
    var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

    var summary = await _context.Measurements
        .Where(m => DateOnly.FromDateTime(m.Timestamp) == yesterday)
        .GroupBy(m => 1)
        .Select(g => new DailySummary
        {
            Date = yesterday,
            Steps = g.Where(m => m.MetricType.Name == "steps").Sum(m => (int)m.Value),
            Weight = g.Where(m => m.MetricType.Name == "weight").Average(m => m.Value),
            // ...
        })
        .FirstOrDefaultAsync();

    if (summary != null)
    {
        await _context.DailySummaries.AddAsync(summary);
        await _context.SaveChangesAsync();
    }
}
```

### 9.2 Monitoring Database Size

```csharp
// Application/Services/DatabaseMaintenanceService.cs
public class DatabaseMaintenanceService
{
    private readonly HealthDbContext _context;
    private readonly ILogger<DatabaseMaintenanceService> _logger;

    public async Task<long> GetDatabaseSizeAsync()
    {
        var sql = @"
            SELECT SUM(reserved_page_count) * 8192.0 / 1024 / 1024 AS SizeMB
            FROM sys.dm_db_partition_stats";

        var sizeMb = await _context.Database
            .SqlQueryRaw<decimal>(sql)
            .FirstOrDefaultAsync();

        _logger.LogInformation("Database size: {SizeMB:F2} MB", sizeMb);

        return (long)sizeMb;
    }
}
```

---

## 10. Connection Resiliency and Retry Logic

### 10.1 Retry Policy Configuration

```csharp
public static IServiceCollection AddDatabaseContext(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.AddDbContext<HealthDbContext>(options =>
    {
        options.UseSqlServer(connectionString, sqlServerOptions =>
        {
            sqlServerOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: new[] {
                    -2,     // Timeout
                    -1,     // Connection failed
                    2,      // Network error
                    53,     // Connection timeout
                    64,     // SQL Server doesn't exist
                    233,    // Connection initialization error
                    10053,  // Transport-level error
                    10054,  // Connection forcibly closed
                    10060,  // Network unreachable
                    40197,  // Service error processing request
                    40501,  // Service busy
                    40613   // Database unavailable
                });
        });
    });

    return services;
}
```

### 10.2 Manual Retry with Polly

```csharp
// Install: Microsoft.Extensions.Http.Polly
services.AddHttpClient<IOuraService>()
    .AddTransientHttpErrorPolicy(policy =>
        policy.WaitAndRetryAsync(3, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

// Or for database operations
public async Task<Measurement> AddMeasurementWithRetryAsync(Measurement measurement)
{
    var policy = Policy
        .Handle<SqlException>()
        .Or<TimeoutException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (exception, timeSpan, retryCount, context) =>
            {
                _logger.LogWarning(
                    "Retry {RetryCount} after {TimeSpan} due to {Exception}",
                    retryCount, timeSpan, exception.Message);
            });

    return await policy.ExecuteAsync(async () =>
    {
        return await _repository.AddAsync(measurement);
    });
}
```

---

## 11. Testing Strategy

### 11.1 In-Memory Database for Unit Tests

```csharp
// Tests/Repositories/MeasurementsRepositoryTests.cs
[TestClass]
public class MeasurementsRepositoryTests
{
    private HealthDbContext _context = null!;
    private MeasurementsRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<HealthDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new HealthDbContext(options);
        _repository = new MeasurementsRepository(_context,
            Mock.Of<ILogger<MeasurementsRepository>>());

        SeedTestData();
    }

    private void SeedTestData()
    {
        var metricType = new MetricType
        {
            Id = 1,
            Name = "sleep_score",
            Unit = "score",
            Category = "Sleep"
        };

        var source = new Source
        {
            Id = 1,
            ProviderName = "Oura",
            IsEnabled = true
        };

        _context.MetricTypes.Add(metricType);
        _context.Sources.Add(source);
        _context.SaveChanges();
    }

    [TestMethod]
    public async Task GetLatestByMetricTypeAsync_ReturnsLatestMeasurement()
    {
        // Arrange
        var measurements = new[]
        {
            new Measurement { MetricTypeId = 1, SourceId = 1, Value = 80, Timestamp = DateTime.UtcNow.AddDays(-2) },
            new Measurement { MetricTypeId = 1, SourceId = 1, Value = 85, Timestamp = DateTime.UtcNow.AddDays(-1) },
            new Measurement { MetricTypeId = 1, SourceId = 1, Value = 90, Timestamp = DateTime.UtcNow }
        };

        await _context.Measurements.AddRangeAsync(measurements);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetLatestByMetricTypeAsync("sleep_score");

        // Assert
        Assert.AreEqual(90, result.First().Value);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

### 11.2 Integration Tests with SQL Server LocalDB

```csharp
[TestClass]
public class MeasurementsRepositoryIntegrationTests
{
    private const string ConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=HealthAggregatorTests;Trusted_Connection=True;";

    [TestInitialize]
    public async Task Setup()
    {
        var options = new DbContextOptionsBuilder<HealthDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        using var context = new HealthDbContext(options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    [TestMethod]
    public async Task UniqueConstraint_PreventsDuplicateMeasurements()
    {
        // Test unique constraint enforcement
    }
}
```

---

## 12. Gray Areas / Questions

### 12.1 Raw Data Storage

**Question:** Should we store the raw JSON response from external APIs?

**Options:**
- **Option 1:** Store in `RawDataJson` column (helps debugging, uses space)
- **Option 2:** Don't store raw data (saves space)
- **Option 3:** Store in Azure Blob Storage with reference

**Recommendation:** Option 1 for 90 days, then archive to Blob Storage.

### 12.2 Time Zone Handling

**Question:** How should we handle time zones for measurements?

**Options:**
- **Option 1:** Store all timestamps as UTC (recommended by Microsoft)
- **Option 2:** Store with time zone offset (`DATETIMEOFFSET`)
- **Option 3:** Store user's local time with separate TZ column

**Recommendation:** Option 1 (UTC only) for simplicity, convert to user TZ in UI.

---

## 13. Implementation Checklist

- [ ] Define entity models with proper data annotations
- [ ] Create entity configurations with Fluent API
- [ ] Configure BaseEntity for audit tracking
- [ ] Implement soft delete with query filters
- [ ] Configure DbContext with connection resiliency
- [ ] Set up DbContext pooling for performance
- [ ] Create initial EF Core migration
- [ ] Seed reference data (MetricTypes)
- [ ] Implement repository interfaces
- [ ] Implement repository classes with optimized queries
- [ ] Add composite indexes for query performance
- [ ] Configure unique constraints to prevent duplicates
- [ ] Test migrations on LocalDB
- [ ] Write unit tests with in-memory database
- [ ] Write integration tests with SQL Server
- [ ] Configure Azure SQL connection string with Managed Identity
- [ ] Test connection resiliency with retry policies
- [ ] Monitor database size for Free tier limits
- [ ] Implement data archival strategy
- [ ] Document migration rollback procedures
- [ ] Set up automated migration in CI/CD pipeline

---

## 14. References

### Microsoft Documentation

- [EF Core 8 What's New](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-8.0/whatsnew)
- [EF Core Performance Best Practices](https://learn.microsoft.com/en-us/ef/core/performance/)
- [Connection Resiliency](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency)
- [Azure SQL Database Best Practices](https://learn.microsoft.com/en-us/azure/azure-sql/database/performance-guidance)
- [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Query Tracking Behavior](https://learn.microsoft.com/en-us/ef/core/querying/tracking)
- [Split Queries](https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries)

### Best Practices

- Always use `AsNoTracking()` for read-only queries
- Use composite indexes for multi-column WHERE clauses
- Batch bulk operations in chunks of 1000
- Use connection pooling for high-traffic scenarios
- Implement retry logic for transient failures
- Store timestamps as UTC (DATETIME2)
- Use soft deletes with query filters for audit trail
- Seed reference data in migrations
- Use repository pattern for testability
- Monitor database size on Free tier (32 MB limit)
