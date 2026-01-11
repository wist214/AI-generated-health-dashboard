using HealthAggregatorV2.Functions.Application.Services;
using HealthAggregatorV2.Functions.Application.Services.Interfaces;
using HealthAggregatorV2.Functions.Infrastructure.ExternalApis.Cronometer;
using HealthAggregatorV2.Functions.Infrastructure.ExternalApis.Oura;
using HealthAggregatorV2.Functions.Infrastructure.ExternalApis.Picooc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace HealthAggregatorV2.Functions.Extensions;

/// <summary>
/// Extension methods for configuring Functions DI services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add all sync-related services to the DI container.
    /// </summary>
    public static IServiceCollection AddSyncServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register configuration options
        services.Configure<OuraApiOptions>(configuration.GetSection(OuraApiOptions.SectionName));
        services.Configure<PicoocApiOptions>(configuration.GetSection(PicoocApiOptions.SectionName));
        services.Configure<CronometerApiOptions>(configuration.GetSection(CronometerApiOptions.SectionName));

        // Register core sync services
        services.AddSingleton<IIdempotencyService, IdempotencyService>();
        services.AddScoped<ISyncOrchestrator, SyncOrchestrator>();
        services.AddScoped<IDailySummaryAggregationService, DailySummaryAggregationService>();

        // Register API clients with retry policies
        services.AddOuraClient(configuration);
        services.AddPicoocClient(configuration);
        services.AddCronometerReader(configuration);

        // Register sync services (keyed by source name for orchestrator)
        services.AddScoped<IDataSourceSyncService, OuraSyncService>();
        services.AddScoped<IDataSourceSyncService, PicoocSyncService>();
        services.AddScoped<IDataSourceSyncService, CronometerSyncService>();

        return services;
    }

    private static IServiceCollection AddOuraClient(this IServiceCollection services, IConfiguration configuration)
    {
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    // Logging handled by Polly
                });

        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromMinutes(1));

        services.AddHttpClient<IOuraApiClient, OuraApiClient>()
            .AddPolicyHandler(retryPolicy)
            .AddPolicyHandler(circuitBreakerPolicy);

        return services;
    }

    private static IServiceCollection AddPicoocClient(this IServiceCollection services, IConfiguration configuration)
    {
        // Picooc uses Docker exec, not HTTP, so just register directly
        services.AddScoped<IPicoocApiClient, PicoocApiClient>();
        return services;
    }

    private static IServiceCollection AddCronometerReader(this IServiceCollection services, IConfiguration configuration)
    {
        // Cronometer uses file-based CSV reader
        services.AddScoped<ICronometerDataReader, CronometerDataReader>();
        return services;
    }
}
