using HealthAggregatorV2.Api.Application.DTOs.Responses;
using HealthAggregatorV2.Api.Application.Services.Interfaces;
using HealthAggregatorV2.Application.Interfaces.Repositories;
using HealthAggregatorV2.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace HealthAggregatorV2.Api.Application.Services;

/// <summary>
/// Service for querying and aggregating metrics data.
/// </summary>
public class MetricsService : IMetricsService
{
    private readonly IMeasurementsRepository _measurementsRepository;
    private readonly IMetricTypesRepository _metricTypesRepository;
    private readonly ILogger<MetricsService> _logger;

    public MetricsService(
        IMeasurementsRepository measurementsRepository,
        IMetricTypesRepository metricTypesRepository,
        ILogger<MetricsService> logger)
    {
        _measurementsRepository = measurementsRepository;
        _metricTypesRepository = metricTypesRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<MetricLatestDto>> GetLatestMetricsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting latest metrics for all types");

        var latestMeasurements = await _measurementsRepository.GetLatestByMetricTypeAsync(cancellationToken);

        return latestMeasurements.Select(m => new MetricLatestDto(
            MetricType: m.MetricType.Name,
            Value: m.Value,
            Unit: m.MetricType.Unit,
            Timestamp: m.Timestamp,
            SourceName: m.Source.ProviderName,
            Category: m.MetricType.Category.ToString()
        ));
    }

    public async Task<MetricLatestDto?> GetLatestMetricAsync(string metricType, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting latest metric for type: {MetricType}", metricType);

        var metricTypeEntity = await _metricTypesRepository.GetByNameAsync(metricType, cancellationToken);
        if (metricTypeEntity == null)
        {
            _logger.LogWarning("Metric type not found: {MetricType}", metricType);
            return null;
        }

        var measurement = await _measurementsRepository.GetLatestByMetricTypeIdAsync(metricTypeEntity.Id, cancellationToken);
        if (measurement == null)
        {
            return null;
        }

        return new MetricLatestDto(
            MetricType: measurement.MetricType.Name,
            Value: measurement.Value,
            Unit: measurement.MetricType.Unit,
            Timestamp: measurement.Timestamp,
            SourceName: measurement.Source.ProviderName,
            Category: measurement.MetricType.Category.ToString()
        );
    }

    public async Task<MetricRangeDto?> GetMetricsInRangeAsync(
        string metricType,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting metrics in range for type: {MetricType}, from: {From}, to: {To}", metricType, from, to);

        var metricTypeEntity = await _metricTypesRepository.GetByNameAsync(metricType, cancellationToken);
        if (metricTypeEntity == null)
        {
            _logger.LogWarning("Metric type not found: {MetricType}", metricType);
            return null;
        }

        var measurements = await _measurementsRepository.GetByDateRangeAsync(
            metricTypeEntity.Id,
            from,
            to,
            cancellationToken);

        return new MetricRangeDto(
            MetricType: metricTypeEntity.Name,
            Unit: metricTypeEntity.Unit,
            Category: metricTypeEntity.Category.ToString(),
            DataPoints: measurements.Select(m => new MetricDataPointDto(
                Value: m.Value,
                Timestamp: m.Timestamp,
                SourceName: m.Source.ProviderName
            ))
        );
    }

    public async Task<IEnumerable<MetricRangeDto>> GetMetricsByCategoryAsync(
        string category,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting metrics by category: {Category}, from: {From}, to: {To}", category, from, to);

        if (!Enum.TryParse<MetricCategory>(category, ignoreCase: true, out var metricCategory))
        {
            _logger.LogWarning("Invalid category: {Category}", category);
            return [];
        }

        var metricTypes = await _metricTypesRepository.GetByCategoryAsync(metricCategory, cancellationToken);

        var results = new List<MetricRangeDto>();
        foreach (var metricType in metricTypes)
        {
            var measurements = await _measurementsRepository.GetByDateRangeAsync(
                metricType.Id,
                from,
                to,
                cancellationToken);

            results.Add(new MetricRangeDto(
                MetricType: metricType.Name,
                Unit: metricType.Unit,
                Category: metricType.Category.ToString(),
                DataPoints: measurements.Select(m => new MetricDataPointDto(
                    Value: m.Value,
                    Timestamp: m.Timestamp,
                    SourceName: m.Source.ProviderName
                ))
            ));
        }

        return results;
    }
}
