using HealthAggregatorV2.Domain.Entities;

namespace HealthAggregatorV2.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for Measurement entity operations.
/// This is the high-volume repository requiring optimized queries.
/// </summary>
public interface IMeasurementsRepository : IRepository<Measurement>
{
    /// <summary>
    /// Gets the latest measurement for a specific metric type.
    /// </summary>
    Task<Measurement?> GetLatestByMetricTypeAsync(
        string metricTypeName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest N measurements for a specific metric type.
    /// </summary>
    Task<IEnumerable<Measurement>> GetLatestByMetricTypeAsync(
        string metricTypeName,
        int count,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets measurements within a date range, optionally filtered by metric type.
    /// </summary>
    Task<IEnumerable<Measurement>> GetByDateRangeAsync(
        DateTime from,
        DateTime to,
        string? metricTypeName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest measurement for a specific metric type and source.
    /// </summary>
    Task<Measurement?> GetLatestByMetricTypeAndSourceAsync(
        string metricTypeName,
        long sourceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a measurement already exists (to prevent duplicates).
    /// </summary>
    Task<bool> ExistsAsync(
        int metricTypeId,
        long sourceId,
        DateTime timestamp,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets measurements by source within a date range.
    /// </summary>
    Task<IEnumerable<Measurement>> GetBySourceAsync(
        long sourceId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);
}
