using HealthAggregatorV2.Api.Application.DTOs.Responses;

namespace HealthAggregatorV2.Api.Application.Services.Interfaces;

/// <summary>
/// Service for aggregating dashboard data.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Gets the dashboard summary with latest metrics from all sources.
    /// </summary>
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets daily summaries within a date range.
    /// </summary>
    Task<IEnumerable<DailySummaryDto>> GetDailySummariesAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}
