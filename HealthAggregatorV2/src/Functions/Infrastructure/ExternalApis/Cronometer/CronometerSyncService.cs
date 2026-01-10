using HealthAggregatorV2.Application.Interfaces.Repositories;
using HealthAggregatorV2.Domain.Entities;
using HealthAggregatorV2.Functions.Application.DTOs;
using HealthAggregatorV2.Functions.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace HealthAggregatorV2.Functions.Infrastructure.ExternalApis.Cronometer;

/// <summary>
/// Sync service for Cronometer nutrition data.
/// Reads from CSV exports and stores as measurements.
/// </summary>
public class CronometerSyncService : IDataSourceSyncService
{
    private readonly ICronometerDataReader _dataReader;
    private readonly IMeasurementsRepository _measurementsRepository;
    private readonly IMetricTypesRepository _metricTypesRepository;
    private readonly ISourcesRepository _sourcesRepository;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<CronometerSyncService> _logger;

    // Cronometer metric type names (must match seeded data in snake_case)
    private const string CaloriesMetric = "calories_consumed";
    private const string ProteinMetric = "protein";
    private const string CarbohydratesMetric = "carbs";
    private const string FatMetric = "fat";
    private const string FiberMetric = "fiber";
    private const string SugarMetric = "sugars";
    private const string SodiumMetric = "sodium";
    private const string CholesterolMetric = "cholesterol";
    private const string SaturatedFatMetric = "fat"; // Using fat metric for saturated fat (simplified)
    private const string WaterIntakeMetric = "water";

    public string SourceName => "Cronometer";

    public CronometerSyncService(
        ICronometerDataReader dataReader,
        IMeasurementsRepository measurementsRepository,
        IMetricTypesRepository metricTypesRepository,
        ISourcesRepository sourcesRepository,
        IIdempotencyService idempotencyService,
        ILogger<CronometerSyncService> logger)
    {
        _dataReader = dataReader;
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
            // Get the Cronometer data source
            var dataSource = await _sourcesRepository.GetByProviderNameAsync(SourceName, cancellationToken);
            if (dataSource == null)
            {
                return SyncResult.Failure($"Data source '{SourceName}' not found in database");
            }

            // Determine date range for sync
            var endDate = DateTime.UtcNow.Date;
            var startDate = dataSource.LastSyncedAt?.Date.AddDays(-1) // Overlap by 1 day for safety
                ?? endDate.AddDays(-30); // Initial backfill

            _logger.LogInformation(
                "Syncing Cronometer data from {StartDate} to {EndDate}",
                startDate,
                endDate);

            // Load metric types
            var metricTypes = await LoadMetricTypesAsync(cancellationToken);

            // Fetch nutrition data
            var nutritionData = await _dataReader.GetNutritionDataAsync(startDate, endDate, cancellationToken);
            var count = 0;

            foreach (var nutrition in nutritionData)
            {
                var timestamp = nutrition.Date;

                // Calories
                if (nutrition.Calories.HasValue && metricTypes.TryGetValue(CaloriesMetric, out var caloriesType))
                {
                    if (!await _idempotencyService.IsDuplicateAsync(caloriesType.Id, dataSource.Id, timestamp, cancellationToken))
                    {
                        await SaveMeasurementAsync(caloriesType, dataSource, timestamp, nutrition.Calories.Value, cancellationToken);
                        count++;
                    }
                }

                // Protein
                if (nutrition.Protein.HasValue && metricTypes.TryGetValue(ProteinMetric, out var proteinType))
                {
                    if (!await _idempotencyService.IsDuplicateAsync(proteinType.Id, dataSource.Id, timestamp, cancellationToken))
                    {
                        await SaveMeasurementAsync(proteinType, dataSource, timestamp, nutrition.Protein.Value, cancellationToken);
                        count++;
                    }
                }

                // Carbohydrates
                if (nutrition.Carbohydrates.HasValue && metricTypes.TryGetValue(CarbohydratesMetric, out var carbsType))
                {
                    if (!await _idempotencyService.IsDuplicateAsync(carbsType.Id, dataSource.Id, timestamp, cancellationToken))
                    {
                        await SaveMeasurementAsync(carbsType, dataSource, timestamp, nutrition.Carbohydrates.Value, cancellationToken);
                        count++;
                    }
                }

                // Fat
                if (nutrition.Fat.HasValue && metricTypes.TryGetValue(FatMetric, out var fatType))
                {
                    if (!await _idempotencyService.IsDuplicateAsync(fatType.Id, dataSource.Id, timestamp, cancellationToken))
                    {
                        await SaveMeasurementAsync(fatType, dataSource, timestamp, nutrition.Fat.Value, cancellationToken);
                        count++;
                    }
                }

                // Fiber
                if (nutrition.Fiber.HasValue && metricTypes.TryGetValue(FiberMetric, out var fiberType))
                {
                    if (!await _idempotencyService.IsDuplicateAsync(fiberType.Id, dataSource.Id, timestamp, cancellationToken))
                    {
                        await SaveMeasurementAsync(fiberType, dataSource, timestamp, nutrition.Fiber.Value, cancellationToken);
                        count++;
                    }
                }

                // Sugar
                if (nutrition.Sugar.HasValue && metricTypes.TryGetValue(SugarMetric, out var sugarType))
                {
                    if (!await _idempotencyService.IsDuplicateAsync(sugarType.Id, dataSource.Id, timestamp, cancellationToken))
                    {
                        await SaveMeasurementAsync(sugarType, dataSource, timestamp, nutrition.Sugar.Value, cancellationToken);
                        count++;
                    }
                }

                // Sodium
                if (nutrition.Sodium.HasValue && metricTypes.TryGetValue(SodiumMetric, out var sodiumType))
                {
                    if (!await _idempotencyService.IsDuplicateAsync(sodiumType.Id, dataSource.Id, timestamp, cancellationToken))
                    {
                        await SaveMeasurementAsync(sodiumType, dataSource, timestamp, nutrition.Sodium.Value, cancellationToken);
                        count++;
                    }
                }

                // Cholesterol
                if (nutrition.Cholesterol.HasValue && metricTypes.TryGetValue(CholesterolMetric, out var cholesterolType))
                {
                    if (!await _idempotencyService.IsDuplicateAsync(cholesterolType.Id, dataSource.Id, timestamp, cancellationToken))
                    {
                        await SaveMeasurementAsync(cholesterolType, dataSource, timestamp, nutrition.Cholesterol.Value, cancellationToken);
                        count++;
                    }
                }

                // Saturated Fat
                if (nutrition.SaturatedFat.HasValue && metricTypes.TryGetValue(SaturatedFatMetric, out var satFatType))
                {
                    if (!await _idempotencyService.IsDuplicateAsync(satFatType.Id, dataSource.Id, timestamp, cancellationToken))
                    {
                        await SaveMeasurementAsync(satFatType, dataSource, timestamp, nutrition.SaturatedFat.Value, cancellationToken);
                        count++;
                    }
                }

                // Water Intake
                if (nutrition.Water.HasValue && metricTypes.TryGetValue(WaterIntakeMetric, out var waterType))
                {
                    if (!await _idempotencyService.IsDuplicateAsync(waterType.Id, dataSource.Id, timestamp, cancellationToken))
                    {
                        // Convert grams to liters (1000g = 1L)
                        var liters = nutrition.Water.Value / 1000m;
                        await SaveMeasurementAsync(waterType, dataSource, timestamp, liters, cancellationToken);
                        count++;
                    }
                }
            }

            _logger.LogInformation(
                "Cronometer sync completed: {Count} records in {Duration}ms",
                count,
                (DateTime.UtcNow - startTime).TotalMilliseconds);

            return SyncResult.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Cronometer data");
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
