using HealthAggregator.Core.DTOs;

namespace HealthAggregator.Core.Interfaces;

/// <summary>
/// Interface for dashboard data aggregation and calculations.
/// Single Responsibility: Aggregates and calculates dashboard metrics.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Gets the complete dashboard data including all metrics.
    /// </summary>
    Task<DashboardDto> GetDashboardDataAsync();

    /// <summary>
    /// Gets the metric cards for the dashboard.
    /// </summary>
    Task<List<MetricCardDto>> GetMetricCardsAsync();

    /// <summary>
    /// Gets progress comparison data.
    /// </summary>
    Task<ProgressComparisonDto> GetProgressComparisonAsync();

    /// <summary>
    /// Gets weekly trends data for charts.
    /// </summary>
    Task<WeeklyTrendsDto> GetWeeklyTrendsAsync();

    /// <summary>
    /// Gets generated insights based on health data.
    /// </summary>
    Task<List<InsightDto>> GenerateInsightsAsync();

    /// <summary>
    /// Gets quick statistics.
    /// </summary>
    Task<QuickStatsDto> GetQuickStatsAsync();
}
