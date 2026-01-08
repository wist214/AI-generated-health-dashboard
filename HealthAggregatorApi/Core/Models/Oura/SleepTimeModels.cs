namespace HealthAggregatorApi.Core.Models.Oura;

/// <summary>
/// Sleep time recommendation from Oura Ring.
/// Contains optimal bedtime window calculated by Oura.
/// </summary>
public class OuraSleepTimeRecord
{
    public string Id { get; set; } = "";
    public string Day { get; set; } = "";
    
    /// <summary>
    /// Optimal bedtime window.
    /// </summary>
    public OuraOptimalBedtime? OptimalBedtime { get; set; }
    
    /// <summary>
    /// Sleep recommendation: "improve_efficiency", "earlier_bedtime", 
    /// "later_bedtime", "earlier_wake", "later_wake".
    /// </summary>
    public string? Recommendation { get; set; }
    
    /// <summary>
    /// Status: "not_enough_nights", "low_sleep_scores", "good_sleep".
    /// </summary>
    public string? Status { get; set; }
}

/// <summary>
/// Optimal bedtime window with offsets from midnight.
/// </summary>
public class OuraOptimalBedtime
{
    /// <summary>
    /// Timezone offset in seconds.
    /// </summary>
    public int? DayTz { get; set; }
    
    /// <summary>
    /// End of optimal bedtime window (seconds from midnight).
    /// </summary>
    public int? EndOffset { get; set; }
    
    /// <summary>
    /// Start of optimal bedtime window (seconds from midnight).
    /// Negative values indicate time before midnight.
    /// </summary>
    public int? StartOffset { get; set; }
}
