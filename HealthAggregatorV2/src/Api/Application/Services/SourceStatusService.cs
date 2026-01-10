using HealthAggregatorV2.Api.Application.DTOs.Responses;
using HealthAggregatorV2.Api.Application.Services.Interfaces;
using HealthAggregatorV2.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace HealthAggregatorV2.Api.Application.Services;

/// <summary>
/// Service for managing data source status.
/// </summary>
public class SourceStatusService : ISourceStatusService
{
    private readonly ISourcesRepository _sourcesRepository;
    private readonly IMeasurementsRepository _measurementsRepository;
    private readonly ILogger<SourceStatusService> _logger;

    public SourceStatusService(
        ISourcesRepository sourcesRepository,
        IMeasurementsRepository measurementsRepository,
        ILogger<SourceStatusService> logger)
    {
        _sourcesRepository = sourcesRepository;
        _measurementsRepository = measurementsRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<SourceStatusDto>> GetAllSourceStatusAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting status for all sources");

        var sources = await _sourcesRepository.GetAllAsync(cancellationToken);
        var results = new List<SourceStatusDto>();

        foreach (var source in sources)
        {
            var count = await _measurementsRepository.GetCountBySourceAsync(source.Id, cancellationToken);
            results.Add(new SourceStatusDto(
                Id: source.Id,
                ProviderName: source.ProviderName,
                IsEnabled: source.IsEnabled,
                LastSyncedAt: source.LastSyncedAt,
                MeasurementCount: count
            ));
        }

        return results;
    }

    public async Task<SourceStatusDto?> GetSourceStatusAsync(string sourceName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting status for source: {SourceName}", sourceName);

        var source = await _sourcesRepository.GetByProviderNameAsync(sourceName, cancellationToken);
        if (source == null)
        {
            _logger.LogWarning("Source not found: {SourceName}", sourceName);
            return null;
        }

        var count = await _measurementsRepository.GetCountBySourceAsync(source.Id, cancellationToken);

        return new SourceStatusDto(
            Id: source.Id,
            ProviderName: source.ProviderName,
            IsEnabled: source.IsEnabled,
            LastSyncedAt: source.LastSyncedAt,
            MeasurementCount: count
        );
    }
}
