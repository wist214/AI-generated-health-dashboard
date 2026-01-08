namespace HealthAggregatorApi.Core.Models.Oura;

/// <summary>
/// Daily resilience record from Oura Ring.
/// Measures ability to withstand and recover from physiological stress.
/// </summary>
public class OuraResilienceRecord
{
    public string Id { get; set; } = "";
    public string Day { get; set; } = "";
    
    /// <summary>
    /// Resilience level: "limited", "adequate", "solid", "strong", or "exceptional".
    /// </summary>
    public string? Level { get; set; }
    
    /// <summary>
    /// Contributing factors to resilience.
    /// </summary>
    public OuraResilienceContributors? Contributors { get; set; }
}

/// <summary>
/// Contributing factors to resilience score.
/// </summary>
public class OuraResilienceContributors
{
    /// <summary>
    /// Sleep recovery contribution (0-100).
    /// </summary>
    public int? SleepRecovery { get; set; }
    
    /// <summary>
    /// Daytime recovery contribution (0-100).
    /// </summary>
    public int? DaytimeRecovery { get; set; }
    
    /// <summary>
    /// Stress contribution (0-100).
    /// </summary>
    public int? Stress { get; set; }
}
