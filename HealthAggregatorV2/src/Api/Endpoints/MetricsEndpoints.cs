using HealthAggregatorV2.Api.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HealthAggregatorV2.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for metrics operations.
/// </summary>
public static class MetricsEndpoints
{
    public static void MapMetricsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/metrics")
            .WithTags("Metrics")
            .WithOpenApi();

        group.MapGet("/latest", GetLatestMetrics)
            .WithName("GetLatestMetrics")
            .WithDescription("Gets the latest value for each metric type");

        group.MapGet("/latest/{metricType}", GetLatestMetric)
            .WithName("GetLatestMetric")
            .WithDescription("Gets the latest value for a specific metric type");

        group.MapGet("/range", GetMetricsInRange)
            .WithName("GetMetricsInRange")
            .WithDescription("Gets metric values within a date range");

        group.MapGet("/category/{category}", GetMetricsByCategory)
            .WithName("GetMetricsByCategory")
            .WithDescription("Gets metrics filtered by category within a date range");
    }

    private static async Task<IResult> GetLatestMetrics(
        IMetricsService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetLatestMetricsAsync(cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetLatestMetric(
        string metricType,
        IMetricsService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetLatestMetricAsync(metricType, cancellationToken);
        return result is null
            ? Results.NotFound($"Metric type '{metricType}' not found")
            : Results.Ok(result);
    }

    private static async Task<IResult> GetMetricsInRange(
        [FromQuery] string metricType,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        IMetricsService service,
        CancellationToken cancellationToken)
    {
        if (from > to)
        {
            return Results.BadRequest("'from' date must be before 'to' date");
        }

        var result = await service.GetMetricsInRangeAsync(metricType, from, to, cancellationToken);
        return result is null
            ? Results.NotFound($"Metric type '{metricType}' not found")
            : Results.Ok(result);
    }

    private static async Task<IResult> GetMetricsByCategory(
        string category,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        IMetricsService service,
        CancellationToken cancellationToken)
    {
        if (from > to)
        {
            return Results.BadRequest("'from' date must be before 'to' date");
        }

        var result = await service.GetMetricsByCategoryAsync(category, from, to, cancellationToken);
        return Results.Ok(result);
    }
}
