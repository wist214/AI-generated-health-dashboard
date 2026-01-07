namespace HealthAggregatorApi.Core.Models.Settings;

/// <summary>
/// User settings for health targets and body measurements.
/// Stored in Azure Blob Storage.
/// </summary>
public class UserSettings
{
    /// <summary>
    /// Current weight in kilograms.
    /// </summary>
    public double? Weight { get; set; }
    
    /// <summary>
    /// Height in centimeters.
    /// </summary>
    public int? Height { get; set; }
    
    /// <summary>
    /// Daily calorie target in kcal.
    /// </summary>
    public int Calories { get; set; } = 2000;
    
    /// <summary>
    /// Daily protein target in grams.
    /// </summary>
    public int Protein { get; set; } = 150;
    
    /// <summary>
    /// Daily carbohydrates target in grams.
    /// </summary>
    public int Carbs { get; set; } = 250;
    
    /// <summary>
    /// Daily fat target in grams.
    /// </summary>
    public int Fat { get; set; } = 65;
    
    /// <summary>
    /// Last updated timestamp.
    /// </summary>
    public DateTime? LastUpdated { get; set; }
}
