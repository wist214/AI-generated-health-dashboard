using HealthAggregatorApi.Core.Models.Picooc;

namespace HealthAggregatorApi.Core.Interfaces;

/// <summary>
/// Interface for Picooc scale data synchronization.
/// </summary>
public interface IPicoocSyncClient
{
    Task<bool> CheckAvailableAsync();
    bool IsConfigured();
    Task<IEnumerable<HealthMeasurement>> SyncFromCloudAsync();
}
