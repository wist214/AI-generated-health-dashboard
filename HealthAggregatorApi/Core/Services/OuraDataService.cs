using HealthAggregatorApi.Core.Interfaces;
using HealthAggregatorApi.Core.Models.Oura;

namespace HealthAggregatorApi.Core.Services;

/// <summary>
/// Oura data service handling business logic for Oura Ring data.
/// </summary>
public class OuraDataService : IOuraDataService
{
    private readonly IOuraApiClient _apiClient;
    private readonly IDataRepository<OuraData> _repository;

    public OuraDataService(IOuraApiClient apiClient, IDataRepository<OuraData> repository)
    {
        _apiClient = apiClient;
        _repository = repository;
    }

    public async Task<OuraData> GetAllDataAsync()
    {
        return await _repository.GetAsync() ?? new OuraData();
    }

    public async Task<OuraData> SyncDataAsync(DateTime startDate, DateTime endDate)
    {
        // Fetch all data in parallel
        var sleepRecordsTask = _apiClient.GetSleepRecordsAsync(startDate, endDate);
        var dailySleepTask = _apiClient.GetDailySleepAsync(startDate, endDate);
        var readinessTask = _apiClient.GetReadinessAsync(startDate, endDate);
        var activityTask = _apiClient.GetActivityAsync(startDate, endDate);
        var personalInfoTask = _apiClient.GetPersonalInfoAsync();
        
        // New endpoints
        var dailyStressTask = _apiClient.GetDailyStressAsync(startDate, endDate);
        var dailyResilienceTask = _apiClient.GetDailyResilienceAsync(startDate, endDate);
        var vo2MaxTask = _apiClient.GetVo2MaxAsync(startDate, endDate);
        var cardiovascularAgeTask = _apiClient.GetCardiovascularAgeAsync(startDate, endDate);
        var workoutsTask = _apiClient.GetWorkoutsAsync(startDate, endDate);
        var sleepTimeTask = _apiClient.GetSleepTimeAsync(startDate, endDate);
        var spo2Task = _apiClient.GetSpO2Async(startDate, endDate);

        await Task.WhenAll(
            sleepRecordsTask, dailySleepTask, readinessTask, activityTask, personalInfoTask,
            dailyStressTask, dailyResilienceTask, vo2MaxTask, cardiovascularAgeTask,
            workoutsTask, sleepTimeTask, spo2Task);

        var sleepRecords = await sleepRecordsTask;
        var dailySleep = await dailySleepTask;
        var readiness = await readinessTask;
        var activity = await activityTask;
        var personalInfo = await personalInfoTask;
        var dailyStress = await dailyStressTask;
        var dailyResilience = await dailyResilienceTask;
        var vo2Max = await vo2MaxTask;
        var cardiovascularAge = await cardiovascularAgeTask;
        var workouts = await workoutsTask;
        var sleepTime = await sleepTimeTask;
        var spo2 = await spo2Task;

        var existingData = await _repository.GetAsync() ?? new OuraData();

        // Merge new data with existing (avoid duplicates)
        MergeData(existingData.SleepRecords, sleepRecords, s => s.Id);
        MergeData(existingData.DailySleep, dailySleep, s => s.Day);
        MergeData(existingData.Readiness, readiness, r => r.Day);
        MergeData(existingData.Activity, activity, a => a.Day);
        
        // Merge new data types
        MergeData(existingData.DailyStress, dailyStress, s => s.Day);
        MergeData(existingData.DailyResilience, dailyResilience, r => r.Day);
        MergeData(existingData.Vo2Max, vo2Max, v => v.Day);
        MergeData(existingData.CardiovascularAge, cardiovascularAge, c => c.Day);
        MergeData(existingData.Workouts, workouts, w => w.Id);
        MergeData(existingData.SleepTime, sleepTime, s => s.Day);
        MergeData(existingData.SpO2, spo2, s => s.Day);
        
        existingData.PersonalInfo = personalInfo ?? existingData.PersonalInfo;
        existingData.LastSync = DateTime.UtcNow;

        await _repository.SaveAsync(existingData);

        return existingData;
    }

    public async Task<OuraDailySleepRecord?> GetLatestSleepAsync()
    {
        var data = await GetAllDataAsync();
        return data.DailySleep
            .Where(s => s.Score.HasValue)
            .OrderByDescending(s => s.Day)
            .FirstOrDefault();
    }

    public async Task<OuraReadinessRecord?> GetLatestReadinessAsync()
    {
        var data = await GetAllDataAsync();
        return data.Readiness
            .Where(r => r.Score.HasValue)
            .OrderByDescending(r => r.Day)
            .FirstOrDefault();
    }

    public async Task<OuraActivityRecord?> GetLatestActivityAsync()
    {
        var data = await GetAllDataAsync();
        return data.Activity
            .Where(a => a.Score.HasValue)
            .OrderByDescending(a => a.Day)
            .FirstOrDefault();
    }

    public async Task<Dictionary<string, double>> GetAverageScoresAsync(int days)
    {
        var data = await GetAllDataAsync();
        var cutoffDate = DateTime.UtcNow.AddDays(-days).ToString("yyyy-MM-dd");

        var sleepScores = data.DailySleep
            .Where(s => s.Day.CompareTo(cutoffDate) >= 0 && s.Score.HasValue)
            .Select(s => (double)s.Score!.Value)
            .ToList();

        var readinessScores = data.Readiness
            .Where(r => r.Day.CompareTo(cutoffDate) >= 0 && r.Score.HasValue)
            .Select(r => (double)r.Score!.Value)
            .ToList();

        var activityScores = data.Activity
            .Where(a => a.Day.CompareTo(cutoffDate) >= 0 && a.Score.HasValue)
            .Select(a => (double)a.Score!.Value)
            .ToList();

        return new Dictionary<string, double>
        {
            ["sleep"] = sleepScores.Count > 0 ? Math.Round(sleepScores.Average(), 1) : 0,
            ["readiness"] = readinessScores.Count > 0 ? Math.Round(readinessScores.Average(), 1) : 0,
            ["activity"] = activityScores.Count > 0 ? Math.Round(activityScores.Average(), 1) : 0
        };
    }

    public async Task<OuraSleepRecord?> GetSleepRecordByIdAsync(string id)
    {
        var data = await GetAllDataAsync();
        return data.SleepRecords.FirstOrDefault(s => s.Id == id);
    }

    public async Task<OuraDailySleepRecord?> GetDailySleepByDayAsync(string day)
    {
        var data = await GetAllDataAsync();
        return data.DailySleep.FirstOrDefault(s => s.Day == day);
    }

    private static void MergeData<T>(List<T> existing, List<T> newData, Func<T, string> keySelector)
    {
        var existingKeys = existing.Select(keySelector).ToHashSet();
        foreach (var item in newData)
        {
            if (!existingKeys.Contains(keySelector(item)))
            {
                existing.Add(item);
            }
            else
            {
                var index = existing.FindIndex(e => keySelector(e) == keySelector(item));
                if (index >= 0)
                    existing[index] = item;
            }
        }
    }
}
