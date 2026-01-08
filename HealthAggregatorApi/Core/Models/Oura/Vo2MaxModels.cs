namespace HealthAggregatorApi.Core.Models.Oura;

/// <summary>
/// VO2 Max record from Oura Ring.
/// Measures maximum oxygen uptake capacity during intense exercise.
/// </summary>
public class OuraVo2MaxRecord
{
    public string Id { get; set; } = "";
    public string Day { get; set; } = "";
    public string? Timestamp { get; set; }
    
    /// <summary>
    /// VO2 Max value in ml/kg/min.
    /// </summary>
    public double? Vo2Max { get; set; }
}
