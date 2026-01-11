namespace HealthAggregatorV2.Functions.Application.Services.Interfaces;

/// <summary>
/// Service for aggregating measurements into daily summaries.
/// </summary>
public interface IDailySummaryAggregationService
{
    /// <summary>
    /// Aggregates measurements for a date range into daily summaries.
    /// Creates or updates DailySummary records for each day.
    /// </summary>
    /// <param name="startDate">Start date for aggregation (inclusive).</param>
    /// <param name="endDate">End date for aggregation (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of daily summaries created or updated.</returns>
    Task<int> AggregateDailySummariesAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}
