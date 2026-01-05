using HealthAggregatorApi.Core.Models.Oura;

namespace HealthAggregatorApi.Core.Interfaces;

/// <summary>
/// Interface for Oura data business logic.
/// </summary>
public interface IOuraDataService
{
    Task<OuraData> GetAllDataAsync();
    Task<OuraData> SyncDataAsync(DateTime startDate, DateTime endDate);
    Task<OuraDailySleepRecord?> GetLatestSleepAsync();
    Task<OuraReadinessRecord?> GetLatestReadinessAsync();
    Task<OuraActivityRecord?> GetLatestActivityAsync();
    Task<Dictionary<string, double>> GetAverageScoresAsync(int days);
    Task<OuraSleepRecord?> GetSleepRecordByIdAsync(string id);
    Task<OuraDailySleepRecord?> GetDailySleepByDayAsync(string day);
}
