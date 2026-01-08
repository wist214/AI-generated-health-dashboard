namespace HealthAggregatorApi.Core.Models.Oura;

/// <summary>
/// Daily SpO2 (blood oxygen) record from Oura Ring.
/// Only available for Gen 3 Oura Ring users.
/// </summary>
public class OuraSpO2Record
{
    public string Id { get; set; } = "";
    public string Day { get; set; } = "";
    
    /// <summary>
    /// SpO2 percentage data.
    /// </summary>
    public OuraSpO2Percentage? Spo2Percentage { get; set; }
    
    /// <summary>
    /// Breathing disturbance index - indicates potential sleep apnea.
    /// </summary>
    public double? BreathingDisturbanceIndex { get; set; }
}

/// <summary>
/// SpO2 percentage data.
/// </summary>
public class OuraSpO2Percentage
{
    /// <summary>
    /// Average SpO2 percentage during sleep (typically 95-100%).
    /// </summary>
    public double? Average { get; set; }
}
