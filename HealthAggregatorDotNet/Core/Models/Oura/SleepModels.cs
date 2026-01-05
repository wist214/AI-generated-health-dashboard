namespace HealthAggregator.Core.Models.Oura;

/// <summary>
/// Detailed sleep record from Oura Ring.
/// </summary>
public class OuraSleepRecord
{
    public string Id { get; set; } = "";
    public string Day { get; set; } = "";
    public string? BedtimeStart { get; set; }
    public string? BedtimeEnd { get; set; }
    public double? AverageBreath { get; set; }
    public double? AverageHeartRate { get; set; }
    public double? AverageHrv { get; set; }
    public int? AwakeTime { get; set; }
    public int? DeepSleepDuration { get; set; }
    public int? Efficiency { get; set; }
    public int? Latency { get; set; }
    public int? LightSleepDuration { get; set; }
    public int? LowestHeartRate { get; set; }
    public int? RemSleepDuration { get; set; }
    public int? RestlessPeriods { get; set; }
    public int? TimeInBed { get; set; }
    public int? TotalSleepDuration { get; set; }
    public string? Type { get; set; }
    
    /// <summary>
    /// Checks if this is a long sleep (not a nap).
    /// </summary>
    public bool IsLongSleep => Type == "long_sleep";
}

/// <summary>
/// Daily sleep summary with score.
/// </summary>
public class OuraDailySleepRecord
{
    public string Id { get; set; } = "";
    public string Day { get; set; } = "";
    public int? Score { get; set; }
    public OuraSleepContributors? Contributors { get; set; }
    public string? Timestamp { get; set; }
}

/// <summary>
/// Contributing factors to sleep score.
/// </summary>
public class OuraSleepContributors
{
    public int? DeepSleep { get; set; }
    public int? Efficiency { get; set; }
    public int? Latency { get; set; }
    public int? RemSleep { get; set; }
    public int? Restfulness { get; set; }
    public int? Timing { get; set; }
    public int? TotalSleep { get; set; }
}
