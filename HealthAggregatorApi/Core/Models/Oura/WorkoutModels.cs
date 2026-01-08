namespace HealthAggregatorApi.Core.Models.Oura;

/// <summary>
/// Workout record from Oura Ring.
/// Contains auto-detected and user-entered workout sessions.
/// </summary>
public class OuraWorkoutRecord
{
    public string Id { get; set; } = "";
    public string? Day { get; set; }
    
    /// <summary>
    /// Type of activity (e.g., "running", "cycling", "walking").
    /// </summary>
    public string? Activity { get; set; }
    
    /// <summary>
    /// Calories burned during workout.
    /// </summary>
    public int? Calories { get; set; }
    
    /// <summary>
    /// Distance in meters.
    /// </summary>
    public int? Distance { get; set; }
    
    /// <summary>
    /// Start time of workout in ISO 8601 format.
    /// </summary>
    public string? StartDatetime { get; set; }
    
    /// <summary>
    /// End time of workout in ISO 8601 format.
    /// </summary>
    public string? EndDatetime { get; set; }
    
    /// <summary>
    /// Workout intensity: "easy", "moderate", or "hard".
    /// </summary>
    public string? Intensity { get; set; }
    
    /// <summary>
    /// User-provided label for the workout.
    /// </summary>
    public string? Label { get; set; }
    
    /// <summary>
    /// Source of workout data: "manual" or "autodetected".
    /// </summary>
    public string? Source { get; set; }
}
