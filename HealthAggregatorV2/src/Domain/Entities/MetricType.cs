using HealthAggregatorV2.Domain.Common;
using HealthAggregatorV2.Domain.Enums;

namespace HealthAggregatorV2.Domain.Entities;

/// <summary>
/// Represents a type of metric that can be measured (e.g., sleep_score, weight, steps).
/// This is reference data that defines valid metric types.
/// </summary>
public class MetricType : BaseEntity
{
    /// <summary>
    /// Unique identifier for the metric type.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique name of the metric (e.g., "sleep_score", "weight", "steps").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unit of measurement (e.g., "score", "kg", "steps", "bpm").
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Category grouping for organizing metrics.
    /// </summary>
    public MetricCategory Category { get; set; }

    /// <summary>
    /// Optional description of what this metric represents.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Minimum valid value for this metric (for validation).
    /// </summary>
    public double? MinValue { get; set; }

    /// <summary>
    /// Maximum valid value for this metric (for validation).
    /// </summary>
    public double? MaxValue { get; set; }

    // Navigation properties

    /// <summary>
    /// Collection of measurements of this type.
    /// </summary>
    public virtual ICollection<Measurement> Measurements { get; set; } = new List<Measurement>();
}
