using HealthAggregatorV2.Api.Application.DTOs.Responses;

namespace HealthAggregatorV2.Api.Application.Services.Interfaces;

/// <summary>
/// Service for querying and aggregating metrics data.
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// Gets the latest value for each metric type.
    /// </summary>
    Task<IEnumerable<MetricLatestDto>> GetLatestMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest value for a specific metric type.
    /// </summary>
    Task<MetricLatestDto?> GetLatestMetricAsync(string metricType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metric values within a date range.
    /// </summary>
    Task<MetricRangeDto?> GetMetricsInRangeAsync(
        string metricType,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metrics filtered by category within a date range.
    /// </summary>
    Task<IEnumerable<MetricRangeDto>> GetMetricsByCategoryAsync(
        string category,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}
