using HealthAggregatorApi.Core.Models.Oura;

namespace HealthAggregatorApi.Core.Interfaces;

/// <summary>
/// Interface for Oura Ring API communication.
/// </summary>
public interface IOuraApiClient
{
    Task<List<OuraSleepRecord>> GetSleepRecordsAsync(DateTime startDate, DateTime endDate);
    Task<List<OuraDailySleepRecord>> GetDailySleepAsync(DateTime startDate, DateTime endDate);
    Task<List<OuraReadinessRecord>> GetReadinessAsync(DateTime startDate, DateTime endDate);
    Task<List<OuraActivityRecord>> GetActivityAsync(DateTime startDate, DateTime endDate);
    Task<OuraPersonalInfo?> GetPersonalInfoAsync();
}
