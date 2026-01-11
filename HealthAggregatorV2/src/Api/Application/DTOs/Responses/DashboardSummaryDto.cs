namespace HealthAggregatorV2.Api.Application.DTOs.Responses;

/// <summary>
/// DTO for returning aggregated dashboard summary.
/// </summary>
public record DashboardSummaryDto
{
    // Sleep metrics
    public int? SleepScore { get; init; }
    public int? ReadinessScore { get; init; }
    public double? TotalSleepHours { get; init; }
    public double? DeepSleepHours { get; init; }
    public double? RemSleepHours { get; init; }
    public int? SleepEfficiency { get; init; }

    // Activity metrics
    public int? ActivityScore { get; init; }
    public int? Steps { get; init; }
    public int? ActiveCalories { get; init; }

    // Body metrics
    public double? Weight { get; init; }
    public double? BodyFat { get; init; }
    public double? Bmi { get; init; }

    // Heart metrics
    public int? HeartRateAvg { get; init; }
    public int? HeartRateMin { get; init; }
    public int? HrvAverage { get; init; }

    // Nutrition metrics
    public int? CaloriesConsumed { get; init; }
    public double? Protein { get; init; }
    public double? Carbs { get; init; }
    public double? Fat { get; init; }

    // Advanced Oura metrics
    public string? DailyStress { get; init; }
    public string? ResilienceLevel { get; init; }
    public double? Vo2Max { get; init; }
    public int? CardiovascularAge { get; init; }
    public double? SpO2Average { get; init; }
    public int? OptimalBedtimeStart { get; init; }
    public int? OptimalBedtimeEnd { get; init; }
    public int? WorkoutCount { get; init; }

    // Metadata
    public DateTime LastUpdated { get; init; }
    public IEnumerable<SourceSyncInfoDto> SourceSyncInfo { get; init; } = [];
}

/// <summary>
/// Brief sync info for each source.
/// </summary>
public record SourceSyncInfoDto(
    string SourceName,
    DateTime? LastSyncedAt
);
