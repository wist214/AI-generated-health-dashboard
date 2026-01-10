using HealthAggregatorV2.Application.Interfaces.Repositories;
using HealthAggregatorV2.Functions.Application.DTOs;
using HealthAggregatorV2.Functions.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace HealthAggregatorV2.Functions.Application.Services;

/// <summary>
/// Orchestrates sync operations across all data sources.
/// </summary>
public class SyncOrchestrator : ISyncOrchestrator
{
    private readonly IEnumerable<IDataSourceSyncService> _syncServices;
    private readonly ISourcesRepository _sourcesRepository;
    private readonly ILogger<SyncOrchestrator> _logger;

    public SyncOrchestrator(
        IEnumerable<IDataSourceSyncService> syncServices,
        ISourcesRepository sourcesRepository,
        ILogger<SyncOrchestrator> logger)
    {
        _syncServices = syncServices;
        _sourcesRepository = sourcesRepository;
        _logger = logger;
    }

    public async Task<SyncResult> SyncAllSourcesAsync(CancellationToken cancellationToken = default)
    {
        var overallResult = new SyncResult
        {
            StartedAt = DateTime.UtcNow
        };

        // Get enabled sources from database
        var enabledSources = (await _sourcesRepository.GetEnabledSourcesAsync(cancellationToken)).ToList();
        var enabledSourceNames = enabledSources.Select(s => s.ProviderName).ToHashSet();

        _logger.LogInformation(
            "Starting sync for {Count} enabled sources: {Sources}",
            enabledSourceNames.Count,
            string.Join(", ", enabledSourceNames));

        foreach (var syncService in _syncServices)
        {
            // Skip if source is not enabled
            if (!enabledSourceNames.Contains(syncService.SourceName))
            {
                _logger.LogDebug("Skipping disabled source: {SourceName}", syncService.SourceName);
                continue;
            }

            try
            {
                _logger.LogInformation("Starting sync for source: {SourceName}", syncService.SourceName);

                var result = await syncService.SyncAsync(cancellationToken);

                if (result.IsSuccess)
                {
                    overallResult.SuccessCount++;
                    overallResult.TotalRecordsSynced += result.RecordsSynced;

                    // Update source LastSyncedAt
                    var source = enabledSources.FirstOrDefault(s => s.ProviderName == syncService.SourceName);
                    if (source != null)
                    {
                        source.LastSyncedAt = DateTime.UtcNow;
                        await _sourcesRepository.UpdateAsync(source, cancellationToken);
                    }

                    _logger.LogInformation(
                        "Sync completed for {SourceName}: {RecordsSynced} records synced",
                        syncService.SourceName,
                        result.RecordsSynced);
                }
                else
                {
                    overallResult.FailedCount++;
                    _logger.LogWarning(
                        "Sync failed for {SourceName}: {ErrorMessage}",
                        syncService.SourceName,
                        result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                overallResult.FailedCount++;
                _logger.LogError(ex, "Exception during sync for source: {SourceName}", syncService.SourceName);
            }
        }

        overallResult.IsSuccess = overallResult.FailedCount == 0;
        overallResult.CompletedAt = DateTime.UtcNow;

        return overallResult;
    }

    public async Task<SyncResult> SyncSourceAsync(
        string sourceName,
        CancellationToken cancellationToken = default)
    {
        var syncService = _syncServices.FirstOrDefault(s =>
            s.SourceName.Equals(sourceName, StringComparison.OrdinalIgnoreCase));

        if (syncService == null)
        {
            throw new InvalidOperationException($"No sync service found for source: {sourceName}");
        }

        // Check if source is enabled
        var source = await _sourcesRepository.GetByProviderNameAsync(sourceName, cancellationToken);
        if (source == null)
        {
            throw new InvalidOperationException($"Source '{sourceName}' not found in database");
        }

        if (!source.IsEnabled)
        {
            _logger.LogWarning("Attempting to sync disabled source: {SourceName}", sourceName);
        }

        var result = await syncService.SyncAsync(cancellationToken);

        // Update LastSyncedAt on success
        if (result.IsSuccess)
        {
            source.LastSyncedAt = DateTime.UtcNow;
            await _sourcesRepository.UpdateAsync(source, cancellationToken);
        }

        return result;
    }
}
