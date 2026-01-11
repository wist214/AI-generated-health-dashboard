using HealthAggregatorV2.Application.Interfaces.Repositories;
using HealthAggregatorV2.Domain.Entities;
using HealthAggregatorV2.Functions.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace HealthAggregatorV2.Functions.Application.Services;

/// <summary>
/// Service that aggregates measurements into daily summaries.
/// This creates DailySummary records from individual Measurement records.
/// </summary>
public class DailySummaryAggregationService : IDailySummaryAggregationService
{
    private readonly IMeasurementsRepository _measurementsRepository;
    private readonly IDailySummaryRepository _dailySummaryRepository;
    private readonly IMetricTypesRepository _metricTypesRepository;
    private readonly ILogger<DailySummaryAggregationService> _logger;

    // Metric type names (must match seeded data)
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
    private const string WeightMetric = "weight";
    private const string BodyFatMetric = "body_fat_percentage";
    private const string CaloriesConsumedMetric = "energy";
    private const string ProteinMetric = "protein";
    private const string CarbsMetric = "carbs";
    private const string FatMetric = "fat";
    
    // Advanced Oura metrics
    private const string DailyStressMetric = "daily_stress";
    private const string ResilienceLevelMetric = "resilience_level";
    private const string Vo2MaxMetric = "vo2_max";
    private const string CardiovascularAgeMetric = "cardiovascular_age";
    private const string SpO2AverageMetric = "spo2_average";
    private const string OptimalBedtimeStartMetric = "optimal_bedtime_start";
    private const string OptimalBedtimeEndMetric = "optimal_bedtime_end";
    private const string WorkoutCountMetric = "workout_count";

    public DailySummaryAggregationService(
        IMeasurementsRepository measurementsRepository,
        IDailySummaryRepository dailySummaryRepository,
        IMetricTypesRepository metricTypesRepository,
        ILogger<DailySummaryAggregationService> logger)
    {
        _measurementsRepository = measurementsRepository;
        _dailySummaryRepository = dailySummaryRepository;
        _metricTypesRepository = metricTypesRepository;
        _logger = logger;
    }

    public async Task<int> AggregateDailySummariesAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Aggregating daily summaries from {StartDate} to {EndDate}",
            startDate.Date,
            endDate.Date);

        // Load metric types
        var metricTypes = await LoadMetricTypesAsync(cancellationToken);

        var count = 0;
        var currentDate = startDate.Date;

        while (currentDate <= endDate.Date)
        {
            var updated = await AggregateDayAsync(currentDate, metricTypes, cancellationToken);
            if (updated)
            {
                count++;
            }

            currentDate = currentDate.AddDays(1);
        }

        _logger.LogInformation("Aggregated {Count} daily summaries", count);
        return count;
    }

    private async Task<Dictionary<string, MetricType>> LoadMetricTypesAsync(CancellationToken cancellationToken)
    {
        var allMetricTypes = await _metricTypesRepository.GetAllAsync(cancellationToken);
        return allMetricTypes.ToDictionary(m => m.Name, m => m);
    }

    private async Task<bool> AggregateDayAsync(
        DateTime date,
        Dictionary<string, MetricType> metricTypes,
        CancellationToken cancellationToken)
    {
        var dateOnly = DateOnly.FromDateTime(date);
        var nextDay = date.AddDays(1);

        // Get all measurements for this day
        var measurements = await _measurementsRepository.GetByDateRangeAsync(date, nextDay, null, cancellationToken);

        // If no measurements for this day, skip
        if (!measurements.Any())
        {
            return false;
        }

        // Get or create daily summary
        var summary = await _dailySummaryRepository.GetByDateAsync(dateOnly, cancellationToken);
        if (summary == null)
        {
            summary = new DailySummary
            {
                Date = dateOnly,
                CreatedAt = DateTime.UtcNow
            };
        }

        // Aggregate metrics
        summary.SleepScore = GetLatestIntValue(measurements, metricTypes, SleepScoreMetric);
        summary.TotalSleepDuration = ConvertHoursToSeconds(GetLatestValue(measurements, metricTypes, TotalSleepMetric));
        summary.DeepSleepDuration = ConvertHoursToSeconds(GetLatestValue(measurements, metricTypes, DeepSleepMetric));
        summary.RemSleepDuration = ConvertHoursToSeconds(GetLatestValue(measurements, metricTypes, RemSleepMetric));
        summary.SleepEfficiency = GetLatestIntValue(measurements, metricTypes, SleepEfficiencyMetric);

        summary.ReadinessScore = GetLatestIntValue(measurements, metricTypes, ReadinessScoreMetric);
        summary.HrvAverage = GetLatestIntValue(measurements, metricTypes, HrvMetric);
        summary.HeartRateMin = GetLatestIntValue(measurements, metricTypes, RestingHeartRateMetric);

        summary.ActivityScore = GetLatestIntValue(measurements, metricTypes, ActivityScoreMetric);
        summary.Steps = GetLatestIntValue(measurements, metricTypes, StepsMetric);
        summary.CaloriesBurned = GetLatestIntValue(measurements, metricTypes, CaloriesBurnedMetric);

        summary.Weight = GetLatestValue(measurements, metricTypes, WeightMetric);
        summary.BodyFatPercentage = GetLatestValue(measurements, metricTypes, BodyFatMetric);

        summary.CaloriesConsumed = GetLatestIntValue(measurements, metricTypes, CaloriesConsumedMetric);
        summary.ProteinGrams = GetLatestValue(measurements, metricTypes, ProteinMetric);
        summary.CarbsGrams = GetLatestValue(measurements, metricTypes, CarbsMetric);
        summary.FatGrams = GetLatestValue(measurements, metricTypes, FatMetric);

        // Advanced Oura metrics
        summary.DailyStress = ConvertNumericToStressLevel(GetLatestValue(measurements, metricTypes, DailyStressMetric));
        summary.ResilienceLevel = ConvertNumericToResilienceLevel(GetLatestValue(measurements, metricTypes, ResilienceLevelMetric));
        summary.Vo2Max = GetLatestValue(measurements, metricTypes, Vo2MaxMetric);
        summary.CardiovascularAge = GetLatestIntValue(measurements, metricTypes, CardiovascularAgeMetric);
        summary.SpO2Average = GetLatestValue(measurements, metricTypes, SpO2AverageMetric);
        summary.OptimalBedtimeStart = GetLatestIntValue(measurements, metricTypes, OptimalBedtimeStartMetric);
        summary.OptimalBedtimeEnd = GetLatestIntValue(measurements, metricTypes, OptimalBedtimeEndMetric);
        summary.WorkoutCount = GetLatestIntValue(measurements, metricTypes, WorkoutCountMetric);

        summary.UpdatedAt = DateTime.UtcNow;

        // Save or update
        if (summary.Id == 0)
        {
            await _dailySummaryRepository.AddAsync(summary, cancellationToken);
            _logger.LogDebug("Created daily summary for {Date}", dateOnly);
        }
        else
        {
            await _dailySummaryRepository.UpdateAsync(summary, cancellationToken);
            _logger.LogDebug("Updated daily summary for {Date}", dateOnly);
        }

        return true;
    }

    private static double? GetLatestValue(
        IEnumerable<Measurement> measurements,
        Dictionary<string, MetricType> metricTypes,
        string metricName)
    {
        if (!metricTypes.TryGetValue(metricName, out var metricType))
        {
            return null;
        }

        var measurement = measurements
            .Where(m => m.MetricTypeId == metricType.Id)
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefault();

        return measurement?.Value;
    }

    private static int? GetLatestIntValue(
        IEnumerable<Measurement> measurements,
        Dictionary<string, MetricType> metricTypes,
        string metricName)
    {
        var value = GetLatestValue(measurements, metricTypes, metricName);
        return value.HasValue ? (int)Math.Round(value.Value) : null;
    }

    private static int? ConvertHoursToSeconds(double? hours)
    {
        return hours.HasValue ? (int)Math.Round(hours.Value * 3600.0) : null;
    }

    private static string? ConvertNumericToStressLevel(double? value)
    {
        if (!value.HasValue) return null;
        
        return ((int)value) switch
        {
            0 => "restored",
            1 => "normal",
            2 => "stressful",
            _ => "normal"
        };
    }

    private static string? ConvertNumericToResilienceLevel(double? value)
    {
        if (!value.HasValue) return null;
        
        return ((int)value) switch
        {
            0 => "limited",
            1 => "adequate",
            2 => "solid",
            3 => "strong",
            4 => "exceptional",
            _ => "adequate"
        };
    }
}
