using HealthAggregatorV2.Domain.Common;

namespace HealthAggregatorV2.Domain.Entities;

/// <summary>
/// Represents an individual health measurement from a data source.
/// This is the high-volume entity storing all metric data points.
/// </summary>
public class Measurement : BaseEntity
{
    /// <summary>
    /// Unique identifier for the measurement.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Foreign key to the metric type.
    /// </summary>
    public int MetricTypeId { get; set; }

    /// <summary>
    /// Foreign key to the data source.
    /// </summary>
    public long SourceId { get; set; }

    /// <summary>
    /// The measured value.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// UTC timestamp when this measurement was taken.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Optional raw JSON response from the API for debugging/audit.
    /// </summary>
    public string? RawDataJson { get; set; }

    // Navigation properties

    /// <summary>
    /// The type of metric being measured.
    /// </summary>
    public virtual MetricType MetricType { get; set; } = null!;

    /// <summary>
    /// The source that provided this measurement.
    /// </summary>
    public virtual Source Source { get; set; } = null!;
}
