using HealthAggregator.Core.Models.Picooc;

namespace HealthAggregator.Core.Interfaces;

/// <summary>
/// Interface for Picooc scale data synchronization.
/// Single Responsibility: Only handles syncing data from Picooc cloud.
/// </summary>
public interface IPicoocSyncClient
{
    /// <summary>
    /// Checks if the service is available and configured.
    /// </summary>
    Task<bool> CheckDockerAvailableAsync();

    /// <summary>
    /// Checks if credentials are configured (sync method name).
    /// </summary>
    bool IsConfigured();

    /// <summary>
    /// Synchronizes data from Picooc cloud service.
    /// </summary>
    /// <returns>List of health measurements from the cloud</returns>
    Task<IEnumerable<HealthMeasurement>> SyncFromCloudAsync();
}
