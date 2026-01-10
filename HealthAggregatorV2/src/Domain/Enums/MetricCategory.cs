namespace HealthAggregatorV2.Domain.Enums;

/// <summary>
/// Categories for organizing metric types.
/// </summary>
public enum MetricCategory
{
    /// <summary>
    /// Sleep-related metrics (sleep score, duration, efficiency, etc.).
    /// </summary>
    Sleep,

    /// <summary>
    /// Activity-related metrics (steps, calories burned, activity score, etc.).
    /// </summary>
    Activity,

    /// <summary>
    /// Body composition metrics (weight, body fat, BMI, etc.).
    /// </summary>
    Body,

    /// <summary>
    /// Nutrition metrics (calories consumed, protein, carbs, fat, etc.).
    /// </summary>
    Nutrition,

    /// <summary>
    /// Heart-related metrics (heart rate, HRV, etc.).
    /// </summary>
    Heart
}
