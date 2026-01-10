namespace HealthAggregatorV2.Api.Application.DTOs.Responses;

/// <summary>
/// DTO for returning metric values within a date range.
/// </summary>
public record MetricRangeDto(
    string MetricType,
    string Unit,
    string Category,
    IEnumerable<MetricDataPointDto> DataPoints
);

/// <summary>
/// Individual data point in a metric range.
/// </summary>
public record MetricDataPointDto(
    double Value,
    DateTime Timestamp,
    string SourceName
);
