using HealthAggregatorV2.Api.Endpoints;

namespace HealthAggregatorV2.Api.Extensions;

/// <summary>
/// Extension methods for IEndpointRouteBuilder to map API endpoints.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps all API endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapMetricsEndpoints();
        app.MapDashboardEndpoints();
        app.MapSourcesEndpoints();

        return app;
    }
}
