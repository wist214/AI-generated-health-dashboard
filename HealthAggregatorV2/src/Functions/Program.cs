using HealthAggregatorV2.Functions.Extensions;
using HealthAggregatorV2.Infrastructure.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((hostContext, config) =>
    {
        // Add local.settings.json for local development
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        // Application Insights
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Database (shared with API)
        services.AddHealthDatabase(context.Configuration);

        // Repositories (shared with API)
        services.AddRepositories();

        // Sync services
        services.AddSyncServices(context.Configuration);

        // Memory cache for idempotency
        services.AddMemoryCache();

        // Logging configuration
        services.Configure<LoggerFilterOptions>(options =>
        {
            // Remove the default rule that was added by AddApplicationInsights
            var toRemove = options.Rules.FirstOrDefault(rule =>
                rule.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
            if (toRemove is not null)
            {
                options.Rules.Remove(toRemove);
            }
        });
    })
    .Build();

await host.RunAsync();
