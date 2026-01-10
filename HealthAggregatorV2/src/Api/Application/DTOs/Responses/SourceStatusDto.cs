namespace HealthAggregatorV2.Api.Application.DTOs.Responses;

/// <summary>
/// DTO for returning data source status information.
/// </summary>
public record SourceStatusDto(
    long Id,
    string ProviderName,
    bool IsEnabled,
    DateTime? LastSyncedAt,
    int MeasurementCount
);
