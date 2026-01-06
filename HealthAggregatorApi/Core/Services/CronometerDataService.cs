using Microsoft.Extensions.Logging;
using HealthAggregatorApi.Core.Interfaces;
using HealthAggregatorApi.Core.Models.Cronometer;

namespace HealthAggregatorApi.Core.Services;

/// <summary>
/// Service for managing Cronometer nutrition data.
/// </summary>
public class CronometerDataService : ICronometerDataService
{
    private readonly ICronometerApiClient _apiClient;
    private readonly IDataRepository<CronometerData> _repository;
    private readonly ILogger<CronometerDataService> _logger;
    private readonly string _username;
    private readonly string _password;

    public CronometerDataService(
        ICronometerApiClient apiClient,
        IDataRepository<CronometerData> repository,
        string username,
        string password,
        ILogger<CronometerDataService> logger)
    {
        _apiClient = apiClient;
        _repository = repository;
        _username = username;
        _password = password;
        _logger = logger;
    }

    public async Task<CronometerData> GetAllDataAsync()
    {
        try
        {
            var data = await _repository.GetAsync();
            return data ?? new CronometerData();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Cronometer data from repository");
            return new CronometerData();
        }
    }

    public async Task<CronometerData> SyncDataAsync(DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("Starting Cronometer sync from {Start} to {End}", startDate, endDate);

        if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password))
        {
            throw new InvalidOperationException("Cronometer credentials not configured");
        }

        // Login
        var loginSuccess = await _apiClient.LoginAsync(_username, _password);
        if (!loginSuccess)
        {
            throw new Exception("Failed to login to Cronometer");
        }

        try
        {
            // Get existing data
            var existingData = await _repository.GetAsync() ?? new CronometerData();

            // Export all data types
            var dailyNutrition = await _apiClient.ExportDailyNutritionAsync(startDate, endDate);
            var servings = await _apiClient.ExportServingsAsync(startDate, endDate);
            var exercises = await _apiClient.ExportExercisesAsync(startDate, endDate);

            // Merge with existing data (update existing, add new)
            MergeData(existingData.DailyNutrition, dailyNutrition, d => d.Date);
            MergeData(existingData.FoodServings, servings, s => $"{s.Day}_{s.FoodName}_{s.Amount}");
            MergeData(existingData.Exercises, exercises, e => $"{e.Day}_{e.Exercise}");

            existingData.LastSync = DateTime.UtcNow;

            // Save updated data
            await _repository.SaveAsync(existingData);

            _logger.LogInformation("Cronometer sync complete: {Nutrition} nutrition, {Servings} servings, {Exercises} exercises",
                dailyNutrition.Count, servings.Count, exercises.Count);

            return existingData;
        }
        finally
        {
            await _apiClient.LogoutAsync();
        }
    }

    public async Task<DailyNutrition?> GetDailyNutritionAsync(string date)
    {
        var data = await GetAllDataAsync();
        return data.DailyNutrition.FirstOrDefault(d => d.Date == date);
    }

    public async Task<List<FoodServing>> GetServingsForDateAsync(string date)
    {
        var data = await GetAllDataAsync();
        return data.FoodServings.Where(s => s.Day == date).ToList();
    }

    public async Task<DailyNutrition?> GetLatestNutritionAsync()
    {
        var data = await GetAllDataAsync();
        return data.DailyNutrition
            .Where(d => d.Energy.HasValue && d.Energy > 0)
            .OrderByDescending(d => d.Date)
            .FirstOrDefault();
    }

    public async Task<NutritionStats> GetStatsAsync(int days = 30)
    {
        var data = await GetAllDataAsync();
        var cutoffDate = DateTime.UtcNow.AddDays(-days).ToString("yyyy-MM-dd");

        var recentNutrition = data.DailyNutrition
            .Where(d => d.Date.CompareTo(cutoffDate) >= 0 && d.Energy.HasValue && d.Energy > 0)
            .OrderBy(d => d.Date)
            .ToList();

        var recentServings = data.FoodServings
            .Where(s => s.Day.CompareTo(cutoffDate) >= 0)
            .ToList();

        var stats = new NutritionStats
        {
            DaysTracked = recentNutrition.Count,
            TotalServings = recentServings.Count,
            LastSync = data.LastSync
        };

        if (recentNutrition.Count > 0)
        {
            stats.AverageCalories = Math.Round(recentNutrition.Average(d => d.Energy ?? 0), 0);
            stats.AverageProtein = Math.Round(recentNutrition.Average(d => d.Protein ?? 0), 1);
            stats.AverageCarbs = Math.Round(recentNutrition.Average(d => d.Carbs ?? 0), 1);
            stats.AverageFat = Math.Round(recentNutrition.Average(d => d.Fat ?? 0), 1);
            stats.AverageFiber = Math.Round(recentNutrition.Average(d => d.Fiber ?? 0), 1);
            stats.TotalCalories = Math.Round(recentNutrition.Sum(d => d.Energy ?? 0), 0);
            stats.FirstDate = recentNutrition.First().Date;
            stats.LastDate = recentNutrition.Last().Date;

            // Calorie trend
            stats.CalorieTrend = recentNutrition.Select(d => new DailyCaloriePoint
            {
                Date = d.Date,
                Calories = d.Energy ?? 0,
                Protein = d.Protein ?? 0,
                Carbs = d.Carbs ?? 0,
                Fat = d.Fat ?? 0
            }).ToList();
        }

        // Top foods by frequency
        stats.TopFoods = recentServings
            .GroupBy(s => s.FoodName)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .ToDictionary(g => g.Key, g => g.Count());

        // Category breakdown
        stats.CategoryBreakdown = recentServings
            .Where(s => !string.IsNullOrEmpty(s.Category))
            .GroupBy(s => s.Category)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .ToDictionary(g => g.Key, g => g.Count());

        return stats;
    }

    private static void MergeData<T>(List<T> existing, List<T> newData, Func<T, string> keySelector)
    {
        var existingKeys = existing.Select(keySelector).ToHashSet();

        foreach (var item in newData)
        {
            var key = keySelector(item);
            var existingItem = existing.FirstOrDefault(e => keySelector(e) == key);

            if (existingItem != null)
            {
                // Update existing
                var index = existing.IndexOf(existingItem);
                existing[index] = item;
            }
            else
            {
                // Add new
                existing.Add(item);
            }
        }
    }
}
