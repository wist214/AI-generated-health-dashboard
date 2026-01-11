using HealthAggregatorV2.Domain.Common;

namespace HealthAggregatorV2.Domain.Entities;

/// <summary>
/// Represents pre-aggregated daily health data for efficient dashboard queries.
/// This reduces the need to calculate aggregates from individual measurements.
/// </summary>
public class DailySummary : BaseEntity
{
    /// <summary>
    /// Unique identifier for the daily summary.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The date this summary represents (one summary per day).
    /// </summary>
    public DateOnly Date { get; set; }

    // Oura scores (0-100)

    /// <summary>
    /// Sleep score from Oura (0-100).
    /// </summary>
    public int? SleepScore { get; set; }

    /// <summary>
    /// Readiness score from Oura (0-100).
    /// </summary>
    public int? ReadinessScore { get; set; }

    /// <summary>
    /// Activity score from Oura (0-100).
    /// </summary>
    public int? ActivityScore { get; set; }

    // Activity metrics

    /// <summary>
    /// Total steps for the day.
    /// </summary>
    public int? Steps { get; set; }

    /// <summary>
    /// Total calories burned for the day.
    /// </summary>
    public int? CaloriesBurned { get; set; }

    // Body metrics

    /// <summary>
    /// Weight measurement in kilograms.
    /// </summary>
    public double? Weight { get; set; }

    /// <summary>
    /// Body fat percentage.
    /// </summary>
    public double? BodyFatPercentage { get; set; }

    // Heart rate metrics

    /// <summary>
    /// Average heart rate for the day in BPM.
    /// </summary>
    public int? HeartRateAvg { get; set; }

    /// <summary>
    /// Minimum heart rate for the day in BPM.
    /// </summary>
    public int? HeartRateMin { get; set; }

    /// <summary>
    /// Maximum heart rate for the day in BPM.
    /// </summary>
    public int? HeartRateMax { get; set; }

    // Sleep metrics

    /// <summary>
    /// Total sleep duration in seconds.
    /// </summary>
    public int? TotalSleepDuration { get; set; }

    /// <summary>
    /// Deep sleep duration in seconds.
    /// </summary>
    public int? DeepSleepDuration { get; set; }

    /// <summary>
    /// REM sleep duration in seconds.
    /// </summary>
    public int? RemSleepDuration { get; set; }

    /// <summary>
    /// Sleep efficiency percentage.
    /// </summary>
    public int? SleepEfficiency { get; set; }

    // HRV metrics

    /// <summary>
    /// Average Heart Rate Variability in milliseconds.
    /// </summary>
    public int? HrvAverage { get; set; }

    // Nutrition metrics

    /// <summary>
    /// Total calories consumed.
    /// </summary>
    public int? CaloriesConsumed { get; set; }

    /// <summary>
    /// Protein intake in grams.
    /// </summary>
    public double? ProteinGrams { get; set; }

    /// <summary>
    /// Carbohydrate intake in grams.
    /// </summary>
    public double? CarbsGrams { get; set; }

    /// <summary>
    /// Fat intake in grams.
    /// </summary>
    public double? FatGrams { get; set; }

    // Advanced Oura metrics

    /// <summary>
    /// Daily stress summary (restored/normal/stressful).
    /// </summary>
    public string? DailyStress { get; set; }

    /// <summary>
    /// Resilience level (limited/adequate/solid/strong/exceptional).
    /// </summary>
    public string? ResilienceLevel { get; set; }

    /// <summary>
    /// VO2 Max estimate in ml/kg/min.
    /// </summary>
    public double? Vo2Max { get; set; }

    /// <summary>
    /// Cardiovascular age in years.
    /// </summary>
    public int? CardiovascularAge { get; set; }

    /// <summary>
    /// Average SpO2 percentage for the day.
    /// </summary>
    public double? SpO2Average { get; set; }

    /// <summary>
    /// Optimal bedtime start offset from midnight in seconds (can be negative).
    /// </summary>
    public int? OptimalBedtimeStart { get; set; }

    /// <summary>
    /// Optimal bedtime end offset from midnight in seconds (can be negative).
    /// </summary>
    public int? OptimalBedtimeEnd { get; set; }

    /// <summary>
    /// Number of workouts recorded for the day.
    /// </summary>
    public int? WorkoutCount { get; set; }
}
