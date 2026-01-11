using HealthAggregatorV2.Application.Interfaces.Repositories;
using HealthAggregatorV2.Domain.Entities;
using HealthAggregatorV2.Functions.Application.DTOs;
using HealthAggregatorV2.Functions.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace HealthAggregatorV2.Functions.Infrastructure.ExternalApis.Oura;

/// <summary>
/// Sync service for Oura Ring data.
/// Fetches sleep, activity, and readiness data from Oura API and stores as measurements.
/// </summary>
public class OuraSyncService : IDataSourceSyncService
{
    private readonly IOuraApiClient _ouraClient;
    private readonly IMeasurementsRepository _measurementsRepository;
    private readonly IMetricTypesRepository _metricTypesRepository;
    private readonly ISourcesRepository _sourcesRepository;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<OuraSyncService> _logger;

    // Oura metric type names (must match seeded data in snake_case)
    private const string SleepScoreMetric = "sleep_score";
    private const string TotalSleepMetric = "total_sleep_duration";
    private const string DeepSleepMetric = "deep_sleep_duration";
    private const string RemSleepMetric = "rem_sleep_duration";
    private const string SleepEfficiencyMetric = "sleep_efficiency";
    private const string ReadinessScoreMetric = "readiness_score";
    private const string HrvMetric = "hrv_average";
    private const string RestingHeartRateMetric = "heart_rate_min";
    private const string ActivityScoreMetric = "activity_score";
    private const string StepsMetric = "steps";
    private const string CaloriesBurnedMetric = "total_calories";
    
    // Advanced Oura metrics
    private const string DailyStressMetric = "daily_stress";
    private const string ResilienceLevelMetric = "resilience_level";
    private const string Vo2MaxMetric = "vo2_max";
    private const string CardiovascularAgeMetric = "cardiovascular_age";
    private const string SpO2AverageMetric = "spo2_average";
    private const string OptimalBedtimeStartMetric = "optimal_bedtime_start";
    private const string OptimalBedtimeEndMetric = "optimal_bedtime_end";
    private const string WorkoutCountMetric = "workout_count";

    public string SourceName => "Oura";

    public OuraSyncService(
        IOuraApiClient ouraClient,
        IMeasurementsRepository measurementsRepository,
        IMetricTypesRepository metricTypesRepository,
        ISourcesRepository sourcesRepository,
        IIdempotencyService idempotencyService,
        ILogger<OuraSyncService> logger)
    {
        _ouraClient = ouraClient;
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
            // Get the Oura data source
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
                "Syncing Oura data from {StartDate} to {EndDate}",
                startDate,
                endDate);

            // Load metric types
            var metricTypes = await LoadMetricTypesAsync(cancellationToken);

            // Sync each data type
            var sleepCount = await SyncDailySleepAsync(dataSource, metricTypes, startDate, endDate, cancellationToken);
            var readinessCount = await SyncReadinessAsync(dataSource, metricTypes, startDate, endDate, cancellationToken);
            var activityCount = await SyncActivityAsync(dataSource, metricTypes, startDate, endDate, cancellationToken);
            
            // Sync advanced metrics
            var stressCount = await SyncDailyStressAsync(dataSource, metricTypes, startDate, endDate, cancellationToken);
            var resilienceCount = await SyncDailyResilienceAsync(dataSource, metricTypes, startDate, endDate, cancellationToken);
            var vo2MaxCount = await SyncVo2MaxAsync(dataSource, metricTypes, startDate, endDate, cancellationToken);
            var cardioAgeCount = await SyncCardiovascularAgeAsync(dataSource, metricTypes, startDate, endDate, cancellationToken);
            var spo2Count = await SyncDailySpO2Async(dataSource, metricTypes, startDate, endDate, cancellationToken);
            var sleepTimeCount = await SyncSleepTimeAsync(dataSource, metricTypes, startDate, endDate, cancellationToken);
            var workoutCount = await SyncWorkoutsAsync(dataSource, metricTypes, startDate, endDate, cancellationToken);

            var totalCount = sleepCount + readinessCount + activityCount + stressCount + 
                           resilienceCount + vo2MaxCount + cardioAgeCount + spo2Count + 
                           sleepTimeCount + workoutCount;

            _logger.LogInformation(
                "Oura sync completed: {Count} records in {Duration}ms",
                totalCount,
                (DateTime.UtcNow - startTime).TotalMilliseconds);

            return SyncResult.Success(totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Oura data");
            return SyncResult.Failure(ex.Message);
        }
    }

    private async Task<Dictionary<string, MetricType>> LoadMetricTypesAsync(CancellationToken cancellationToken)
    {
        var allMetricTypes = await _metricTypesRepository.GetAllAsync(cancellationToken);
        return allMetricTypes.ToDictionary(m => m.Name, m => m);
    }

    private async Task<int> SyncDailySleepAsync(
        Source dataSource,
        Dictionary<string, MetricType> metricTypes,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var sleepData = await _ouraClient.GetDailySleepAsync(startDate, endDate, cancellationToken);
        var count = 0;

        foreach (var sleep in sleepData)
        {
            if (!DateTime.TryParse(sleep.Day, out var recordDate))
            {
                _logger.LogWarning("Invalid date format in sleep data: {Day}", sleep.Day);
                continue;
            }

            var timestamp = recordDate.Date;

            // Sleep Score
            if (sleep.Score.HasValue && metricTypes.TryGetValue(SleepScoreMetric, out var sleepScoreType))
            {
                if (!await _idempotencyService.IsDuplicateAsync(sleepScoreType.Id, dataSource.Id, timestamp, cancellationToken))
                {
                    await SaveMeasurementAsync(sleepScoreType, dataSource, timestamp, sleep.Score.Value, cancellationToken);
                    count++;
                }
            }

            // Total Sleep (seconds to hours)
            if (sleep.Contributors?.TotalSleep.HasValue == true && metricTypes.TryGetValue(TotalSleepMetric, out var totalSleepType))
            {
                if (!await _idempotencyService.IsDuplicateAsync(totalSleepType.Id, dataSource.Id, timestamp, cancellationToken))
                {
                    var hours = sleep.Contributors.TotalSleep.Value / 3600.0;
                    await SaveMeasurementAsync(totalSleepType, dataSource, timestamp, (decimal)hours, cancellationToken);
                    count++;
                }
            }

            // Deep Sleep (seconds to hours)
            if (sleep.Contributors?.DeepSleep.HasValue == true && metricTypes.TryGetValue(DeepSleepMetric, out var deepSleepType))
            {
                if (!await _idempotencyService.IsDuplicateAsync(deepSleepType.Id, dataSource.Id, timestamp, cancellationToken))
                {
                    var hours = sleep.Contributors.DeepSleep.Value / 3600.0;
                    await SaveMeasurementAsync(deepSleepType, dataSource, timestamp, (decimal)hours, cancellationToken);
                    count++;
                }
            }

            // REM Sleep (seconds to hours)
            if (sleep.Contributors?.RemSleep.HasValue == true && metricTypes.TryGetValue(RemSleepMetric, out var remSleepType))
            {
                if (!await _idempotencyService.IsDuplicateAsync(remSleepType.Id, dataSource.Id, timestamp, cancellationToken))
                {
                    var hours = sleep.Contributors.RemSleep.Value / 3600.0;
                    await SaveMeasurementAsync(remSleepType, dataSource, timestamp, (decimal)hours, cancellationToken);
                    count++;
                }
            }

            // Sleep Efficiency
            if (sleep.Contributors?.Efficiency.HasValue == true && metricTypes.TryGetValue(SleepEfficiencyMetric, out var efficiencyType))
            {
                if (!await _idempotencyService.IsDuplicateAsync(efficiencyType.Id, dataSource.Id, timestamp, cancellationToken))
                {
                    await SaveMeasurementAsync(efficiencyType, dataSource, timestamp, sleep.Contributors.Efficiency.Value, cancellationToken);
                    count++;
                }
            }
        }

        _logger.LogDebug("Synced {Count} sleep measurements", count);
        return count;
    }

    private async Task<int> SyncReadinessAsync(
        Source dataSource,
        Dictionary<string, MetricType> metricTypes,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var readinessData = await _ouraClient.GetDailyReadinessAsync(startDate, endDate, cancellationToken);
        var count = 0;

        foreach (var readiness in readinessData)
        {
            if (!DateTime.TryParse(readiness.Day, out var recordDate))
            {
                _logger.LogWarning("Invalid date format in readiness data: {Day}", readiness.Day);
                continue;
            }

            var timestamp = recordDate.Date;

            // Readiness Score
            if (readiness.Score.HasValue && metricTypes.TryGetValue(ReadinessScoreMetric, out var readinessType))
            {
                if (!await _idempotencyService.IsDuplicateAsync(readinessType.Id, dataSource.Id, timestamp, cancellationToken))
                {
                    await SaveMeasurementAsync(readinessType, dataSource, timestamp, readiness.Score.Value, cancellationToken);
                    count++;
                }
            }

            // HRV Balance (use as HRV proxy)
            if (readiness.Contributors?.HrvBalance.HasValue == true && metricTypes.TryGetValue(HrvMetric, out var hrvType))
            {
                if (!await _idempotencyService.IsDuplicateAsync(hrvType.Id, dataSource.Id, timestamp, cancellationToken))
                {
                    await SaveMeasurementAsync(hrvType, dataSource, timestamp, readiness.Contributors.HrvBalance.Value, cancellationToken);
                    count++;
                }
            }

            // Resting Heart Rate
            if (readiness.Contributors?.RestingHeartRate.HasValue == true && metricTypes.TryGetValue(RestingHeartRateMetric, out var rhrType))
            {
                if (!await _idempotencyService.IsDuplicateAsync(rhrType.Id, dataSource.Id, timestamp, cancellationToken))
                {
                    await SaveMeasurementAsync(rhrType, dataSource, timestamp, readiness.Contributors.RestingHeartRate.Value, cancellationToken);
                    count++;
                }
            }
        }

        _logger.LogDebug("Synced {Count} readiness measurements", count);
        return count;
    }

    private async Task<int> SyncActivityAsync(
        Source dataSource,
        Dictionary<string, MetricType> metricTypes,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var activityData = await _ouraClient.GetDailyActivityAsync(startDate, endDate, cancellationToken);
        var count = 0;

        foreach (var activity in activityData)
        {
            if (!DateTime.TryParse(activity.Day, out var recordDate))
            {
                _logger.LogWarning("Invalid date format in activity data: {Day}", activity.Day);
                continue;
            }

            var timestamp = recordDate.Date;

            // Activity Score
            if (activity.Score.HasValue && metricTypes.TryGetValue(ActivityScoreMetric, out var activityType))
            {
                if (!await _idempotencyService.IsDuplicateAsync(activityType.Id, dataSource.Id, timestamp, cancellationToken))
                {
                    await SaveMeasurementAsync(activityType, dataSource, timestamp, activity.Score.Value, cancellationToken);
                    count++;
                }
            }

            // Steps
            if (activity.Steps.HasValue && metricTypes.TryGetValue(StepsMetric, out var stepsType))
            {
                if (!await _idempotencyService.IsDuplicateAsync(stepsType.Id, dataSource.Id, timestamp, cancellationToken))
                {
                    await SaveMeasurementAsync(stepsType, dataSource, timestamp, activity.Steps.Value, cancellationToken);
                    count++;
                }
            }

            // Calories Burned (total calories)
            if (activity.TotalCalories.HasValue && metricTypes.TryGetValue(CaloriesBurnedMetric, out var caloriesType))
            {
                if (!await _idempotencyService.IsDuplicateAsync(caloriesType.Id, dataSource.Id, timestamp, cancellationToken))
                {
                    await SaveMeasurementAsync(caloriesType, dataSource, timestamp, activity.TotalCalories.Value, cancellationToken);
                    count++;
                }
            }
        }

        _logger.LogDebug("Synced {Count} activity measurements", count);
        return count;
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

    private async Task<int> SyncDailyStressAsync(
        Source dataSource,
        Dictionary<string, MetricType> metricTypes,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var stressData = await _ouraClient.GetDailyStressAsync(startDate, endDate, cancellationToken);
        var count = 0;

        foreach (var stress in stressData)
        {
            if (!DateTime.TryParse(stress.Day, out var recordDate))
            {
                continue;
            }

            var timestamp = recordDate.Date;

            // Store DaySummary as text metric if available
            if (!string.IsNullOrEmpty(stress.DaySummary) && metricTypes.TryGetValue(DailyStressMetric, out var stressType))
            {
                if (!await _idempotencyService.IsDuplicateAsync(stressType.Id, dataSource.Id, timestamp, cancellationToken))
                {
                    // Store as numeric value: 0=restored, 1=normal, 2=stressful
                    decimal value = stress.DaySummary.ToLower() switch
                    {
                        "restored" => 0,
                        "normal" => 1,
                        "stressful" => 2,
                        _ => 1
                    };
                    await SaveMeasurementAsync(stressType, dataSource, timestamp, value, cancellationToken);
                    count++;
                }
            }
        }

        _logger.LogDebug("Synced {Count} stress measurements", count);
        return count;
    }

    private async Task<int> SyncDailyResilienceAsync(
        Source dataSource,
        Dictionary<string, MetricType> metricTypes,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var resilienceData = await _ouraClient.GetDailyResilienceAsync(startDate, endDate, cancellationToken);
        var count = 0;

        foreach (var resilience in resilienceData)
        {
            if (!DateTime.TryParse(resilience.Day, out var recordDate))
            {
                continue;
            }

            var timestamp = recordDate.Date;

            if (!string.IsNullOrEmpty(resilience.Level) && metricTypes.TryGetValue(ResilienceLevelMetric, out var resilienceType))
            {
                if (!await _idempotencyService.IsDuplicateAsync(resilienceType.Id, dataSource.Id, timestamp, cancellationToken))
                {
                    // Store as numeric: 0=limited, 1=adequate, 2=solid, 3=strong, 4=exceptional
                    decimal value = resilience.Level.ToLower() switch
                    {
                        "limited" => 0,
                        "adequate" => 1,
                        "solid" => 2,
                        "strong" => 3,
                        "exceptional" => 4,
                        _ => 1
                    };
                    await SaveMeasurementAsync(resilienceType, dataSource, timestamp, value, cancellationToken);
                    count++;
                }
            }
        }

        _logger.LogDebug("Synced {Count} resilience measurements", count);
        return count;
    }

    private async Task<int> SyncVo2MaxAsync(
        Source dataSource,
        Dictionary<string, MetricType> metricTypes,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var vo2MaxData = await _ouraClient.GetVo2MaxAsync(startDate, endDate, cancellationToken);
        var count = 0;

        foreach (var vo2Max in vo2MaxData)
        {
            if (!DateTime.TryParse(vo2Max.Day, out var recordDate))
            {
                continue;
            }

            var timestamp = recordDate.Date;

            if (vo2Max.Vo2Max.HasValue && metricTypes.TryGetValue(Vo2MaxMetric, out var vo2MaxType))
            {
                if (!await _idempotencyService.IsDuplicateAsync(vo2MaxType.Id, dataSource.Id, timestamp, cancellationToken))
                {
                    await SaveMeasurementAsync(vo2MaxType, dataSource, timestamp, (decimal)vo2Max.Vo2Max.Value, cancellationToken);
                    count++;
                }
            }
        }

        _logger.LogDebug("Synced {Count} VO2 Max measurements", count);
        return count;
    }

    private async Task<int> SyncCardiovascularAgeAsync(
        Source dataSource,
        Dictionary<string, MetricType> metricTypes,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var cardioAgeData = await _ouraClient.GetCardiovascularAgeAsync(startDate, endDate, cancellationToken);
        var count = 0;

        foreach (var cardioAge in cardioAgeData)
        {
            if (!DateTime.TryParse(cardioAge.Day, out var recordDate))
            {
                continue;
            }

            var timestamp = recordDate.Date;

            if (cardioAge.VascularAge.HasValue && metricTypes.TryGetValue(CardiovascularAgeMetric, out var cardioAgeType))
            {
                if (!await _idempotencyService.IsDuplicateAsync(cardioAgeType.Id, dataSource.Id, timestamp, cancellationToken))
                {
                    await SaveMeasurementAsync(cardioAgeType, dataSource, timestamp, cardioAge.VascularAge.Value, cancellationToken);
                    count++;
                }
            }
        }

        _logger.LogDebug("Synced {Count} cardiovascular age measurements", count);
        return count;
    }

    private async Task<int> SyncDailySpO2Async(
        Source dataSource,
        Dictionary<string, MetricType> metricTypes,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var spo2Data = await _ouraClient.GetDailySpO2Async(startDate, endDate, cancellationToken);
        var count = 0;

        foreach (var spo2 in spo2Data)
        {
            if (!DateTime.TryParse(spo2.Day, out var recordDate))
            {
                continue;
            }

            var timestamp = recordDate.Date;

            if (spo2.Spo2Percentage?.Average.HasValue == true && metricTypes.TryGetValue(SpO2AverageMetric, out var spo2Type))
            {
                if (!await _idempotencyService.IsDuplicateAsync(spo2Type.Id, dataSource.Id, timestamp, cancellationToken))
                {
                    await SaveMeasurementAsync(spo2Type, dataSource, timestamp, (decimal)spo2.Spo2Percentage.Average.Value, cancellationToken);
                    count++;
                }
            }
        }

        _logger.LogDebug("Synced {Count} SpO2 measurements", count);
        return count;
    }

    private async Task<int> SyncSleepTimeAsync(
        Source dataSource,
        Dictionary<string, MetricType> metricTypes,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var sleepTimeData = await _ouraClient.GetSleepTimeAsync(startDate, endDate, cancellationToken);
        var count = 0;

        foreach (var sleepTime in sleepTimeData)
        {
            if (!DateTime.TryParse(sleepTime.Day, out var recordDate))
            {
                continue;
            }

            var timestamp = recordDate.Date;

            // Store bedtime start offset
            if (sleepTime.OptimalBedtime?.StartOffset.HasValue == true && 
                metricTypes.TryGetValue(OptimalBedtimeStartMetric, out var startType))
            {
                if (!await _idempotencyService.IsDuplicateAsync(startType.Id, dataSource.Id, timestamp, cancellationToken))
                {
                    await SaveMeasurementAsync(startType, dataSource, timestamp, sleepTime.OptimalBedtime.StartOffset.Value, cancellationToken);
                    count++;
                }
            }

            // Store bedtime end offset
            if (sleepTime.OptimalBedtime?.EndOffset.HasValue == true && 
                metricTypes.TryGetValue(OptimalBedtimeEndMetric, out var endType))
            {
                if (!await _idempotencyService.IsDuplicateAsync(endType.Id, dataSource.Id, timestamp, cancellationToken))
                {
                    await SaveMeasurementAsync(endType, dataSource, timestamp, sleepTime.OptimalBedtime.EndOffset.Value, cancellationToken);
                    count++;
                }
            }
        }

        _logger.LogDebug("Synced {Count} sleep time measurements", count);
        return count;
    }

    private async Task<int> SyncWorkoutsAsync(
        Source dataSource,
        Dictionary<string, MetricType> metricTypes,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var workoutData = await _ouraClient.GetWorkoutsAsync(startDate, endDate, cancellationToken);
        var count = 0;

        // Group workouts by day and count them
        var workoutsByDay = workoutData
            .Where(w => !string.IsNullOrEmpty(w.Day))
            .GroupBy(w => w.Day!)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var kvp in workoutsByDay)
        {
            if (!DateTime.TryParse(kvp.Key, out var recordDate))
            {
                continue;
            }

            var timestamp = recordDate.Date;

            if (metricTypes.TryGetValue(WorkoutCountMetric, out var workoutType))
            {
                if (!await _idempotencyService.IsDuplicateAsync(workoutType.Id, dataSource.Id, timestamp, cancellationToken))
                {
                    await SaveMeasurementAsync(workoutType, dataSource, timestamp, kvp.Value, cancellationToken);
                    count++;
                }
            }
        }

        _logger.LogDebug("Synced {Count} workout count measurements", count);
        return count;
    }
}
