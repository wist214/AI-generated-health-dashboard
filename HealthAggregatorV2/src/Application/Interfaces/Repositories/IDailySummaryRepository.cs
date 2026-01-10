using HealthAggregatorV2.Domain.Entities;

namespace HealthAggregatorV2.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for DailySummary entity operations.
/// </summary>
public interface IDailySummaryRepository : IRepository<DailySummary>
{
    /// <summary>
    /// Gets the daily summary for a specific date.
    /// </summary>
    Task<DailySummary?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets daily summaries within a date range.
    /// </summary>
    Task<IEnumerable<DailySummary>> GetByDateRangeAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent daily summaries.
    /// </summary>
    Task<IEnumerable<DailySummary>> GetLatestAsync(
        int count,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a daily summary for a specific date.
    /// </summary>
    Task<DailySummary> UpsertAsync(DailySummary summary, CancellationToken cancellationToken = default);
}
