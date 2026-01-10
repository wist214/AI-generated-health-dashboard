namespace HealthAggregatorV2.Api.Application.DTOs.Responses;

/// <summary>
/// DTO for returning the latest value of a metric.
/// </summary>
public record MetricLatestDto(
    string MetricType,
    double Value,
    string Unit,
    DateTime Timestamp,
    string SourceName,
    string Category
);
