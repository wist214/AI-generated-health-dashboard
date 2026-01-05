using HealthAggregator.Core.Models.Picooc;
using HealthAggregator.Core.Services;

namespace HealthAggregator.Core.Interfaces;

/// <summary>
/// Interface for Picooc data business logic.
/// Single Responsibility: Business logic for Picooc data operations.
/// </summary>
public interface IPicoocDataService
{
    /// <summary>
    /// Gets all cached Picooc measurements.
    /// </summary>
    Task<List<HealthMeasurement>> GetAllMeasurementsAsync();

    /// <summary>
    /// Synchronizes data from Picooc cloud and merges with existing cache.
    /// </summary>
    Task<List<HealthMeasurement>> SyncDataAsync();

    /// <summary>
    /// Gets the latest measurement.
    /// </summary>
    Task<HealthMeasurement?> GetLatestMeasurementAsync();

    /// <summary>
    /// Gets statistics for measurements in the specified date range.
    /// </summary>
    Task<MeasurementStats> GetStatsAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Gets the last sync timestamp.
    /// </summary>
    Task<DateTime?> GetLastSyncTimeAsync();

    /// <summary>
    /// Checks if the service is available (Docker running).
    /// </summary>
    Task<bool> IsServiceAvailableAsync();
}
