namespace HealthAggregatorApi.Core.Models.Picooc;

/// <summary>
/// Health measurement from Picooc smart scale.
/// </summary>
public class HealthMeasurement
{
    public DateTime Date { get; set; }
    public double Weight { get; set; }
    public double BMI { get; set; }
    public double BodyFat { get; set; }
    public double BodyWater { get; set; }
    public double BoneMass { get; set; }
    public int MetabolicAge { get; set; }
    public int VisceralFat { get; set; }
    public int BasalMetabolism { get; set; }
    public double SkeletalMuscleMass { get; set; }
    public string? Source { get; set; }
    
    /// <summary>
    /// Checks if measurement has valid weight data.
    /// </summary>
    public bool HasValidWeight => Weight > 0;
    
    /// <summary>
    /// Checks if measurement has valid body composition data.
    /// </summary>
    public bool HasBodyComposition => BodyFat > 0 && BodyWater > 0;
}

/// <summary>
/// Cache container for Picooc data.
/// </summary>
public class PicoocDataCache
{
    public List<HealthMeasurement> Measurements { get; set; } = new();
    public DateTime? LastSync { get; set; }
}
