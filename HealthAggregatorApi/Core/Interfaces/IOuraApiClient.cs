using HealthAggregatorApi.Core.Models.Oura;

namespace HealthAggregatorApi.Core.Interfaces;

/// <summary>
/// Interface for Oura Ring API communication.
/// </summary>
public interface IOuraApiClient
{
    // Core data
    Task<List<OuraSleepRecord>> GetSleepRecordsAsync(DateTime startDate, DateTime endDate);
    Task<List<OuraDailySleepRecord>> GetDailySleepAsync(DateTime startDate, DateTime endDate);
    Task<List<OuraReadinessRecord>> GetReadinessAsync(DateTime startDate, DateTime endDate);
    Task<List<OuraActivityRecord>> GetActivityAsync(DateTime startDate, DateTime endDate);
    Task<OuraPersonalInfo?> GetPersonalInfoAsync();
    
    // Stress & Recovery
    Task<List<OuraStressRecord>> GetDailyStressAsync(DateTime startDate, DateTime endDate);
    Task<List<OuraResilienceRecord>> GetDailyResilienceAsync(DateTime startDate, DateTime endDate);
    
    // Cardio Fitness
    Task<List<OuraVo2MaxRecord>> GetVo2MaxAsync(DateTime startDate, DateTime endDate);
    Task<List<OuraCardiovascularAgeRecord>> GetCardiovascularAgeAsync(DateTime startDate, DateTime endDate);
    
    // Exercise & Sleep Guidance
    Task<List<OuraWorkoutRecord>> GetWorkoutsAsync(DateTime startDate, DateTime endDate);
    Task<List<OuraSleepTimeRecord>> GetSleepTimeAsync(DateTime startDate, DateTime endDate);
    
    // Blood Oxygen (Gen 3 only)
    Task<List<OuraSpO2Record>> GetSpO2Async(DateTime startDate, DateTime endDate);
}
