namespace HealthAggregator.Core.Models.Oura;

/// <summary>
/// Personal information from Oura.
/// </summary>
public class OuraPersonalInfo
{
    public string? Id { get; set; }
    public int? Age { get; set; }
    public double? Weight { get; set; }
    public double? Height { get; set; }
    public string? BiologicalSex { get; set; }
    public string? Email { get; set; }
}

/// <summary>
/// Combined Oura data container for caching.
/// </summary>
public class OuraData
{
    public List<OuraSleepRecord> SleepRecords { get; set; } = new();
    public List<OuraDailySleepRecord> DailySleep { get; set; } = new();
    public List<OuraReadinessRecord> Readiness { get; set; } = new();
    public List<OuraActivityRecord> Activity { get; set; } = new();
    public OuraPersonalInfo? PersonalInfo { get; set; }
    public DateTime LastSync { get; set; }
    
    /// <summary>
    /// Gets long sleep records only (excludes naps).
    /// </summary>
    public IEnumerable<OuraSleepRecord> LongSleepRecords => 
        SleepRecords.Where(s => s.IsLongSleep);
}
