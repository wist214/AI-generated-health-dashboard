using HealthAggregatorV2.Api.Application.Services;
using HealthAggregatorV2.Api.Application.Services.Interfaces;

namespace HealthAggregatorV2.Api.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register API services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds API application services to the DI container.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IMetricsService, MetricsService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ISourceStatusService, SourceStatusService>();

        return services;
    }
}
