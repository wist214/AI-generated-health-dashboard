using HealthAggregatorV2.Api.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HealthAggregatorV2.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for dashboard operations.
/// </summary>
public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard")
            .WithTags("Dashboard")
            .WithOpenApi();

        group.MapGet("/summary", GetDashboardSummary)
            .WithName("GetDashboardSummary")
            .WithDescription("Gets the dashboard summary with latest metrics from all sources");

        group.MapGet("/history", GetDailySummaries)
            .WithName("GetDailySummaries")
            .WithDescription("Gets daily summaries within a date range");
    }

    private static async Task<IResult> GetDashboardSummary(
        IDashboardService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetDashboardSummaryAsync(cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetDailySummaries(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        IDashboardService service,
        CancellationToken cancellationToken)
    {
        // Default to last 30 days if not specified
        var toDate = to ?? DateTime.UtcNow.Date;
        var fromDate = from ?? toDate.AddDays(-30);

        if (fromDate > toDate)
        {
            return Results.BadRequest("'from' date must be before 'to' date");
        }

        var result = await service.GetDailySummariesAsync(fromDate, toDate, cancellationToken);
        return Results.Ok(result);
    }
}
