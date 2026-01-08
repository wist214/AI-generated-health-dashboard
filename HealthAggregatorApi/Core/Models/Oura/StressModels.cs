namespace HealthAggregatorApi.Core.Models.Oura;

/// <summary>
/// Daily stress record from Oura Ring.
/// Shows minutes in high stress vs high recovery states.
/// </summary>
public class OuraStressRecord
{
    public string Id { get; set; } = "";
    public string Day { get; set; } = "";
    
    /// <summary>
    /// Minutes spent in high stress state.
    /// </summary>
    public int? StressHigh { get; set; }
    
    /// <summary>
    /// Minutes spent in high recovery state.
    /// </summary>
    public int? RecoveryHigh { get; set; }
    
    /// <summary>
    /// Day summary: "restored", "normal", or "stressful".
    /// </summary>
    public string? DaySummary { get; set; }
}
