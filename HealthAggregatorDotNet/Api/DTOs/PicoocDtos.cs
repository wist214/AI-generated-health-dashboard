using HealthAggregator.Core.Models.Picooc;

namespace HealthAggregator.Api.DTOs;

/// <summary>
/// Response for Picooc sync operation.
/// </summary>
public class PicoocSyncResponseDto
{
    public bool Success { get; set; }
    public int Count { get; set; }
    public string? Error { get; set; }
    public string? Message { get; set; }
    public List<HealthMeasurement>? Data { get; set; }
}

/// <summary>
/// Statistics for Picooc data.
/// </summary>
public class PicoocStatsDto
{
    public bool Success { get; set; }
    public object? Stats { get; set; }
    public int TotalMeasurements { get; set; }
    public MetricStatsDto? Weight { get; set; }
    public MetricStatsDto? BodyFat { get; set; }
    public MetricStatsDto? Bmi { get; set; }
    public DateTime? FirstMeasurement { get; set; }
    public DateTime? LastMeasurement { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Generic metric statistics.
/// </summary>
public class MetricStatsDto
{
    public double Current { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public double Average { get; set; }
}

/// <summary>
/// Status of Picooc configuration.
/// </summary>
public class PicoocStatusDto
{
    public bool Configured { get; set; }
    public bool HasCredentials { get; set; }
    public bool DockerAvailable { get; set; } // Kept for backward compatibility
    public int DataCount { get; set; }
    public string SyncMethod { get; set; } = "native";
    public string Status { get; set; } = "";
    public DateTime? LastSync { get; set; }
}
