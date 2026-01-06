using HealthAggregatorApi.Core.Models.Cronometer;

namespace HealthAggregatorApi.Core.Interfaces;

/// <summary>
/// Interface for Cronometer API communication.
/// </summary>
public interface ICronometerApiClient
{
    /// <summary>
    /// Logs in to Cronometer and establishes session.
    /// </summary>
    Task<bool> LoginAsync(string username, string password);
    
    /// <summary>
    /// Exports daily nutrition summaries for a date range.
    /// </summary>
    Task<List<DailyNutrition>> ExportDailyNutritionAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Exports individual food servings for a date range.
    /// </summary>
    Task<List<FoodServing>> ExportServingsAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Exports exercise entries for a date range.
    /// </summary>
    Task<List<ExerciseEntry>> ExportExercisesAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Exports biometric data for a date range.
    /// </summary>
    Task<List<BiometricEntry>> ExportBiometricsAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Exports notes for a date range.
    /// </summary>
    Task<List<NoteEntry>> ExportNotesAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Logs out and clears session.
    /// </summary>
    Task LogoutAsync();
}
