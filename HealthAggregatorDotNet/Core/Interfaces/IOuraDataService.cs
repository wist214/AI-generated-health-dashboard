using HealthAggregator.Core.Models.Oura;

namespace HealthAggregator.Core.Interfaces;

/// <summary>
/// Interface for Oura data business logic.
/// Single Responsibility: Business logic for Oura data operations.
/// </summary>
public interface IOuraDataService
{
    /// <summary>
    /// Gets all cached Oura data.
    /// </summary>
    Task<OuraData> GetAllDataAsync();

    /// <summary>
    /// Synchronizes data from Oura API for the specified date range.
    /// </summary>
    Task<OuraData> SyncDataAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets the latest sleep record with score.
    /// </summary>
    Task<OuraDailySleepRecord?> GetLatestSleepAsync();

    /// <summary>
    /// Gets the latest readiness record with score.
    /// </summary>
    Task<OuraReadinessRecord?> GetLatestReadinessAsync();

    /// <summary>
    /// Gets the latest activity record with score.
    /// </summary>
    Task<OuraActivityRecord?> GetLatestActivityAsync();

    /// <summary>
    /// Gets average scores for the specified number of days.
    /// </summary>
    Task<Dictionary<string, double>> GetAverageScoresAsync(int days);

    /// <summary>
    /// Gets weekly trend data for sleep and readiness scores.
    /// </summary>
    Task<List<(string Day, int? SleepScore, int? ReadinessScore)>> GetWeeklyTrendsAsync();

    /// <summary>
    /// Gets a specific sleep record by ID.
    /// </summary>
    Task<OuraSleepRecord?> GetSleepRecordByIdAsync(string id);

    /// <summary>
    /// Gets daily sleep record by day.
    /// </summary>
    Task<OuraDailySleepRecord?> GetDailySleepByDayAsync(string day);
}
