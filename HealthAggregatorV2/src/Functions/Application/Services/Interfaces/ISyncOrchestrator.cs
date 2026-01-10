using HealthAggregatorV2.Functions.Application.DTOs;

namespace HealthAggregatorV2.Functions.Application.Services.Interfaces;

/// <summary>
/// Orchestrates sync operations across all data sources.
/// </summary>
public interface ISyncOrchestrator
{
    /// <summary>
    /// Sync all enabled data sources.
    /// </summary>
    Task<SyncResult> SyncAllSourcesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sync a specific data source by name.
    /// </summary>
    Task<SyncResult> SyncSourceAsync(string sourceName, CancellationToken cancellationToken = default);
}
