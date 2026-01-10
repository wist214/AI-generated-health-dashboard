using HealthAggregatorV2.Functions.Application.DTOs;

namespace HealthAggregatorV2.Functions.Application.Services.Interfaces;

/// <summary>
/// Interface for data source sync services.
/// Each data source (Oura, Picooc, Cronometer) implements this interface.
/// </summary>
public interface IDataSourceSyncService
{
    /// <summary>
    /// The name of the data source (e.g., "Oura", "Picooc", "Cronometer").
    /// </summary>
    string SourceName { get; }

    /// <summary>
    /// Sync data from this source to the database.
    /// </summary>
    Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default);
}
