namespace HealthAggregatorV2.Api.Application.DTOs.Responses;

/// <summary>
/// DTO for returning a daily summary record.
/// </summary>
public record DailySummaryDto
{
    public DateTime Date { get; init; }

    // Sleep
    public int? SleepScore { get; init; }
    public int? ReadinessScore { get; init; }
    public double? TotalSleepHours { get; init; }
    public double? DeepSleepHours { get; init; }
    public double? RemSleepHours { get; init; }
    public int? SleepEfficiency { get; init; }

    // Heart
    public int? HrvAverage { get; init; }
    public int? RestingHeartRate { get; init; }

    // Activity
    public int? ActivityScore { get; init; }
    public int? Steps { get; init; }
    public int? ActiveCalories { get; init; }

    // Body
    public double? Weight { get; init; }
    public double? BodyFat { get; init; }

    // Nutrition
    public int? CaloriesConsumed { get; init; }
    public double? Protein { get; init; }
    public double? Carbs { get; init; }
    public double? Fat { get; init; }
}
