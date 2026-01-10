using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HealthAggregatorV2.Application.Interfaces.Repositories;
using HealthAggregatorV2.Infrastructure.Data;
using HealthAggregatorV2.Infrastructure.Repositories;

namespace HealthAggregatorV2.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering Infrastructure services in DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the HealthDbContext with SQL Server configuration.
    /// </summary>
    public static IServiceCollection AddHealthDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("HealthDb")
            ?? configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string not found. Please configure 'ConnectionStrings:HealthDb' or 'ConnectionStrings:AZURE_SQL_CONNECTIONSTRING'.");
        }

        services.AddDbContext<HealthDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlServerOptions =>
            {
                // Connection resiliency for transient failures
                sqlServerOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);

                // Command timeout
                sqlServerOptions.CommandTimeout(60);

                // Enable split queries for better performance with multiple includes
                sqlServerOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });

            // Use NoTracking by default for better read performance
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        return services;
    }

    /// <summary>
    /// Adds the HealthDbContext with SQL Server configuration and migration assembly.
    /// Use this overload when migrations are in a different project (e.g., Api project).
    /// </summary>
    public static IServiceCollection AddHealthDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        string migrationsAssembly)
    {
        var connectionString = configuration.GetConnectionString("HealthDb")
            ?? configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string not found. Please configure 'ConnectionStrings:HealthDb' or 'ConnectionStrings:AZURE_SQL_CONNECTIONSTRING'.");
        }

        services.AddDbContext<HealthDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlServerOptions =>
            {
                // Specify migrations assembly
                sqlServerOptions.MigrationsAssembly(migrationsAssembly);

                // Connection resiliency for transient failures
                sqlServerOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);

                // Command timeout
                sqlServerOptions.CommandTimeout(60);

                // Enable split queries for better performance with multiple includes
                sqlServerOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });

            // Use NoTracking by default for better read performance
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        return services;
    }

    /// <summary>
    /// Adds all repository implementations to the DI container.
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ISourcesRepository, SourcesRepository>();
        services.AddScoped<IMetricTypesRepository, MetricTypesRepository>();
        services.AddScoped<IMeasurementsRepository, MeasurementsRepository>();
        services.AddScoped<IDailySummaryRepository, DailySummaryRepository>();

        return services;
    }

    /// <summary>
    /// Adds all Infrastructure services (database + repositories).
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string? migrationsAssembly = null)
    {
        if (string.IsNullOrEmpty(migrationsAssembly))
        {
            services.AddHealthDatabase(configuration);
        }
        else
        {
            services.AddHealthDatabase(configuration, migrationsAssembly);
        }

        services.AddRepositories();

        return services;
    }
}
