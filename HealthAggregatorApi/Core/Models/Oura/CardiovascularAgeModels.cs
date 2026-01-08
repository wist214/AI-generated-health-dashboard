namespace HealthAggregatorApi.Core.Models.Oura;

/// <summary>
/// Daily cardiovascular age record from Oura Ring.
/// Estimates the health of cardiovascular system relative to actual age.
/// </summary>
public class OuraCardiovascularAgeRecord
{
    public string Day { get; set; } = "";
    
    /// <summary>
    /// Estimated vascular/cardiovascular age in years.
    /// </summary>
    public int? VascularAge { get; set; }
}
