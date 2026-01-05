using HealthAggregator.Core.Models.Oura;

namespace HealthAggregator.Core.Interfaces;

/// <summary>
/// Interface for Oura Ring API communication.
/// Single Responsibility: Only handles API communication with Oura.
/// </summary>
public interface IOuraApiClient
{
    /// <summary>
    /// Fetches all sleep records from Oura API for the specified period.
    /// </summary>
    Task<List<OuraSleepRecord>> GetSleepRecordsAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Fetches daily sleep summaries from Oura API for the specified period.
    /// </summary>
    Task<List<OuraDailySleepRecord>> GetDailySleepAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Fetches readiness data from Oura API for the specified period.
    /// </summary>
    Task<List<OuraReadinessRecord>> GetReadinessAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Fetches activity data from Oura API for the specified period.
    /// </summary>
    Task<List<OuraActivityRecord>> GetActivityAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Fetches personal info from Oura API.
    /// </summary>
    Task<OuraPersonalInfo?> GetPersonalInfoAsync();
}
