namespace HealthAggregator.Core.Models.Oura;

/// <summary>
/// Daily readiness record from Oura Ring.
/// </summary>
public class OuraReadinessRecord
{
    public string Id { get; set; } = "";
    public string Day { get; set; } = "";
    public int? Score { get; set; }
    public double? TemperatureDeviation { get; set; }
    public double? TemperatureTrendDeviation { get; set; }
    public OuraReadinessContributors? Contributors { get; set; }
    public string? Timestamp { get; set; }
}

/// <summary>
/// Contributing factors to readiness score.
/// </summary>
public class OuraReadinessContributors
{
    public int? ActivityBalance { get; set; }
    public int? BodyTemperature { get; set; }
    public int? HrvBalance { get; set; }
    public int? PreviousDayActivity { get; set; }
    public int? PreviousNight { get; set; }
    public int? RecoveryIndex { get; set; }
    public int? RestingHeartRate { get; set; }
    public int? SleepBalance { get; set; }
}
