using HealthAggregatorApi.Core.Models.Picooc;

namespace HealthAggregatorApi.Core.Interfaces;

/// <summary>
/// Interface for Picooc data business logic.
/// </summary>
public interface IPicoocDataService
{
    Task<List<HealthMeasurement>> GetAllMeasurementsAsync();
    Task<List<HealthMeasurement>> SyncDataAsync();
    Task<HealthMeasurement?> GetLatestMeasurementAsync();
    Task<DateTime?> GetLastSyncTimeAsync();
    bool IsConfigured();
}
