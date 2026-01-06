using HealthAggregatorApi.Core.Models.Cronometer;

namespace HealthAggregatorApi.Core.Interfaces;

/// <summary>
/// Interface for Cronometer data operations.
/// </summary>
public interface ICronometerDataService
{
    /// <summary>
    /// Gets all cached Cronometer data.
    /// </summary>
    Task<CronometerData> GetAllDataAsync();
    
    /// <summary>
    /// Syncs data from Cronometer API for the specified date range.
    /// </summary>
    Task<CronometerData> SyncDataAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Gets daily nutrition for a specific date.
    /// </summary>
    Task<DailyNutrition?> GetDailyNutritionAsync(string date);
    
    /// <summary>
    /// Gets food servings for a specific date.
    /// </summary>
    Task<List<FoodServing>> GetServingsForDateAsync(string date);
    
    /// <summary>
    /// Gets the latest daily nutrition record.
    /// </summary>
    Task<DailyNutrition?> GetLatestNutritionAsync();
    
    /// <summary>
    /// Gets nutrition statistics for the last N days.
    /// </summary>
    Task<NutritionStats> GetStatsAsync(int days = 30);
}

/// <summary>
/// Nutrition statistics aggregation.
/// </summary>
public class NutritionStats
{
    public double? AverageCalories { get; set; }
    public double? AverageProtein { get; set; }
    public double? AverageCarbs { get; set; }
    public double? AverageFat { get; set; }
    public double? AverageFiber { get; set; }
    public double? TotalCalories { get; set; }
    public int DaysTracked { get; set; }
    public int TotalServings { get; set; }
    public string? FirstDate { get; set; }
    public string? LastDate { get; set; }
    public DateTime? LastSync { get; set; }
    
    // Trends
    public List<DailyCaloriePoint> CalorieTrend { get; set; } = [];
    public Dictionary<string, int> TopFoods { get; set; } = [];
    public Dictionary<string, int> CategoryBreakdown { get; set; } = [];
}

/// <summary>
/// Point for calorie trend chart.
/// </summary>
public class DailyCaloriePoint
{
    public string Date { get; set; } = string.Empty;
    public double Calories { get; set; }
    public double Protein { get; set; }
    public double Carbs { get; set; }
    public double Fat { get; set; }
}
