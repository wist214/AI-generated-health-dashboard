using HealthAggregator.Core.Models.Oura;

namespace HealthAggregator.Api.DTOs;

/// <summary>
/// Response for Oura sync operation.
/// </summary>
public class OuraSyncResponseDto
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Message { get; set; }
    public OuraData? Data { get; set; }
    public DateTime? LastSync { get; set; }
    public int SleepCount { get; set; }
    public int ReadinessCount { get; set; }
    public int ActivityCount { get; set; }
}

/// <summary>
/// Statistics for Oura data.
/// </summary>
public class OuraStatsDto
{
    public string? Message { get; set; }
    public int TotalSleepRecords { get; set; }
    public int TotalReadinessRecords { get; set; }
    public int TotalActivityRecords { get; set; }
    public double? AverageSleepScore { get; set; }
    public double? AverageReadinessScore { get; set; }
    public double? AverageActivityScore { get; set; }
    public double? AverageSleepDurationHours { get; set; }
    public int? AverageSteps { get; set; }
    public string? FirstDate { get; set; }
    public string? LastDate { get; set; }
}

/// <summary>
/// Status of Oura configuration.
/// </summary>
public class OuraStatusDto
{
    public bool Configured { get; set; }
    public bool HasToken { get; set; }
    public int DataCount { get; set; }
    public DateTime? LastSync { get; set; }
    public Dictionary<string, int> RecordCounts { get; set; } = new();
}
