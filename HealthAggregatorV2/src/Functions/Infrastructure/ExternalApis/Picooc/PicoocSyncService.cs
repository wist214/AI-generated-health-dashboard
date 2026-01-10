using HealthAggregatorV2.Application.Interfaces.Repositories;
using HealthAggregatorV2.Domain.Entities;
using HealthAggregatorV2.Functions.Application.DTOs;
using HealthAggregatorV2.Functions.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace HealthAggregatorV2.Functions.Infrastructure.ExternalApis.Picooc;

/// <summary>
/// Sync service for Picooc smart scale data.
/// Fetches weight and body composition data via SmartScaleConnect.
/// </summary>
public class PicoocSyncService : IDataSourceSyncService
{
    private readonly IPicoocApiClient _picoocClient;
    private readonly IMeasurementsRepository _measurementsRepository;
    private readonly IMetricTypesRepository _metricTypesRepository;
    private readonly ISourcesRepository _sourcesRepository;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<PicoocSyncService> _logger;

    // Picooc metric type names (must match seeded data in snake_case)
    private const string WeightMetric = "weight";
    private const string BmiMetric = "bmi";
    private const string BodyFatMetric = "body_fat";
    private const string MuscleMassMetric = "skeletal_muscle_mass";
    private const string BoneMassMetric = "bone_mass";
    private const string WaterMetric = "body_water";
    private const string VisceralFatMetric = "visceral_fat";
    private const string BmrMetric = "basal_metabolism";

    public string SourceName => "Picooc";

    public PicoocSyncService(
        IPicoocApiClient picoocClient,
        IMeasurementsRepository measurementsRepository,
        IMetricTypesRepository metricTypesRepository,
        ISourcesRepository sourcesRepository,
        IIdempotencyService idempotencyService,
        ILogger<PicoocSyncService> logger)
    {
        _picoocClient = picoocClient;
        _measurementsRepository = measurementsRepository;
        _metricTypesRepository = metricTypesRepository;
        _sourcesRepository = sourcesRepository;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Get the Picooc data source
            var dataSource = await _sourcesRepository.GetByProviderNameAsync(SourceName, cancellationToken);
            if (dataSource == null)
            {
                return SyncResult.Failure($"Data source '{SourceName}' not found in database");
            }

            // Determine start date for sync
            var startDate = dataSource.LastSyncedAt?.Date.AddDays(-1) // Overlap by 1 day for safety
                ?? DateTime.UtcNow.Date.AddDays(-30); // Initial backfill

            _logger.LogInformation("Syncing Picooc data from {StartDate}", startDate);

            // Load metric types
            var metricTypes = await LoadMetricTypesAsync(cancellationToken);

            // Fetch measurements from Picooc
            var measurements = await _picoocClient.GetMeasurementsAsync(startDate, cancellationToken);
            var count = 0;

            foreach (var measurement in measurements)
            {
                if (!DateTime.TryParse(measurement.Timestamp, out var timestamp))
                {
                    _logger.LogWarning("Invalid timestamp in measurement: {Timestamp}", measurement.Timestamp);
                    continue;
                }

                // Weight
                if (measurement.Weight.HasValue && metricTypes.TryGetValue(WeightMetric, out var weightType))
                {
                    if (!await _idempotencyService.IsDuplicateAsync(weightType.Id, dataSource.Id, timestamp, cancellationToken))
                    {
                        await SaveMeasurementAsync(weightType, dataSource, timestamp, measurement.Weight.Value, cancellationToken);
                        count++;
                    }
                }

                // BMI
                if (measurement.Bmi.HasValue && metricTypes.TryGetValue(BmiMetric, out var bmiType))
                {
                    if (!await _idempotencyService.IsDuplicateAsync(bmiType.Id, dataSource.Id, timestamp, cancellationToken))
                    {
                        await SaveMeasurementAsync(bmiType, dataSource, timestamp, measurement.Bmi.Value, cancellationToken);
                        count++;
                    }
                }

                // Body Fat
                if (measurement.BodyFat.HasValue && metricTypes.TryGetValue(BodyFatMetric, out var bodyFatType))
                {
                    if (!await _idempotencyService.IsDuplicateAsync(bodyFatType.Id, dataSource.Id, timestamp, cancellationToken))
                    {
                        await SaveMeasurementAsync(bodyFatType, dataSource, timestamp, measurement.BodyFat.Value, cancellationToken);
                        count++;
                    }
                }

                // Muscle Mass
                if (measurement.MuscleMass.HasValue && metricTypes.TryGetValue(MuscleMassMetric, out var muscleMassType))
                {
                    if (!await _idempotencyService.IsDuplicateAsync(muscleMassType.Id, dataSource.Id, timestamp, cancellationToken))
                    {
                        await SaveMeasurementAsync(muscleMassType, dataSource, timestamp, measurement.MuscleMass.Value, cancellationToken);
                        count++;
                    }
                }

                // Bone Mass
                if (measurement.BoneMass.HasValue && metricTypes.TryGetValue(BoneMassMetric, out var boneMassType))
                {
                    if (!await _idempotencyService.IsDuplicateAsync(boneMassType.Id, dataSource.Id, timestamp, cancellationToken))
                    {
                        await SaveMeasurementAsync(boneMassType, dataSource, timestamp, measurement.BoneMass.Value, cancellationToken);
                        count++;
                    }
                }

                // Body Water
                if (measurement.Water.HasValue && metricTypes.TryGetValue(WaterMetric, out var waterType))
                {
                    if (!await _idempotencyService.IsDuplicateAsync(waterType.Id, dataSource.Id, timestamp, cancellationToken))
                    {
                        await SaveMeasurementAsync(waterType, dataSource, timestamp, measurement.Water.Value, cancellationToken);
                        count++;
                    }
                }

                // Visceral Fat
                if (measurement.VisceralFat.HasValue && metricTypes.TryGetValue(VisceralFatMetric, out var visceralFatType))
                {
                    if (!await _idempotencyService.IsDuplicateAsync(visceralFatType.Id, dataSource.Id, timestamp, cancellationToken))
                    {
                        await SaveMeasurementAsync(visceralFatType, dataSource, timestamp, measurement.VisceralFat.Value, cancellationToken);
                        count++;
                    }
                }

                // BMR
                if (measurement.Bmr.HasValue && metricTypes.TryGetValue(BmrMetric, out var bmrType))
                {
                    if (!await _idempotencyService.IsDuplicateAsync(bmrType.Id, dataSource.Id, timestamp, cancellationToken))
                    {
                        await SaveMeasurementAsync(bmrType, dataSource, timestamp, measurement.Bmr.Value, cancellationToken);
                        count++;
                    }
                }
            }

            _logger.LogInformation(
                "Picooc sync completed: {Count} records in {Duration}ms",
                count,
                (DateTime.UtcNow - startTime).TotalMilliseconds);

            return SyncResult.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Picooc data");
            return SyncResult.Failure(ex.Message);
        }
    }

    private async Task<Dictionary<string, MetricType>> LoadMetricTypesAsync(CancellationToken cancellationToken)
    {
        var allMetricTypes = await _metricTypesRepository.GetAllAsync(cancellationToken);
        return allMetricTypes.ToDictionary(m => m.Name, m => m);
    }

    private async Task SaveMeasurementAsync(
        MetricType metricType,
        Source dataSource,
        DateTime timestamp,
        decimal value,
        CancellationToken cancellationToken)
    {
        var measurement = new Measurement
        {
            MetricTypeId = metricType.Id,
            SourceId = dataSource.Id,
            Timestamp = timestamp,
            Value = (double)value,
            CreatedAt = DateTime.UtcNow
        };

        await _measurementsRepository.AddAsync(measurement, cancellationToken);

        // Mark as processed in idempotency cache
        _idempotencyService.MarkAsProcessed(metricType.Id, dataSource.Id, timestamp);
    }
}
