namespace HealthAggregatorApi.Core.Models.Oura;

/// <summary>
/// Daily activity record from Oura Ring.
/// </summary>
public class OuraActivityRecord
{
    public string Id { get; set; } = "";
    public string Day { get; set; } = "";
    public int? Score { get; set; }
    public int? ActiveCalories { get; set; }
    public int? Steps { get; set; }
    public int? TotalCalories { get; set; }
    public int? EquivalentWalkingDistance { get; set; }
    public int? HighActivityTime { get; set; }
    public int? MediumActivityTime { get; set; }
    public int? LowActivityTime { get; set; }
    public int? SedentaryTime { get; set; }
    public int? RestingTime { get; set; }
    public int? InactivityAlerts { get; set; }
    public string? Timestamp { get; set; }
}
