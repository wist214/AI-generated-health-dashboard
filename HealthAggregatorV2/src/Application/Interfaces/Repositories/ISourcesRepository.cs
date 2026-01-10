using HealthAggregatorV2.Domain.Entities;

namespace HealthAggregatorV2.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for Source entity operations.
/// </summary>
public interface ISourcesRepository : IRepository<Source>
{
    /// <summary>
    /// Gets a source by its provider name.
    /// </summary>
    Task<Source?> GetByProviderNameAsync(string providerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all enabled sources.
    /// </summary>
    Task<IEnumerable<Source>> GetEnabledSourcesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last synced timestamp for a source.
    /// </summary>
    Task UpdateLastSyncedAsync(long sourceId, DateTime syncedAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sources.
    /// </summary>
    new Task<IEnumerable<Source>> GetAllAsync(CancellationToken cancellationToken = default);
}
