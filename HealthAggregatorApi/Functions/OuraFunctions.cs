using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using HealthAggregatorApi.Core.Interfaces;
using System.Net;
using System.Text.Json;

namespace HealthAggregatorApi.Functions;

/// <summary>
/// HTTP-triggered functions for Oura data endpoints.
/// </summary>
public class OuraFunctions
{
    private readonly IOuraDataService _service;
    private readonly ILogger<OuraFunctions> _logger;
    
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public OuraFunctions(IOuraDataService service, ILogger<OuraFunctions> logger)
    {
        _service = service;
        _logger = logger;
    }

    [Function("GetOuraData")]
    public async Task<HttpResponseData> GetData(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "oura/data")] HttpRequestData req)
    {
        _logger.LogInformation("GetOuraData called");
        
        var data = await _service.GetAllDataAsync();
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            sleepRecords = data.SleepRecords,
            dailySleep = data.DailySleep,
            readiness = data.Readiness,
            activity = data.Activity,
            personalInfo = data.PersonalInfo,
            dailyStress = data.DailyStress,
            dailyResilience = data.DailyResilience,
            vo2Max = data.Vo2Max,
            cardiovascularAge = data.CardiovascularAge,
            workouts = data.Workouts,
            sleepTime = data.SleepTime,
            spo2 = data.SpO2,
            lastSync = data.LastSync
        });
        return response;
    }

    [Function("SyncOuraData")]
    public async Task<HttpResponseData> Sync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "oura/sync")] HttpRequestData req)
    {
        _logger.LogInformation("SyncOuraData called");
        
        try
        {
            var endDate = DateTime.UtcNow.AddDays(1);
            var startDate = DateTime.UtcNow.AddDays(-30);
            
            var data = await _service.SyncDataAsync(startDate, endDate);
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                message = "Sync completed successfully",
                sleepCount = data.DailySleep.Count,
                readinessCount = data.Readiness.Count,
                activityCount = data.Activity.Count,
                stressCount = data.DailyStress.Count,
                resilienceCount = data.DailyResilience.Count,
                vo2MaxCount = data.Vo2Max.Count,
                cardiovascularAgeCount = data.CardiovascularAge.Count,
                workoutsCount = data.Workouts.Count,
                spo2Count = data.SpO2.Count,
                lastSync = data.LastSync
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Oura sync failed");
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = false,
                message = ex.Message
            });
            return response;
        }
    }

    [Function("GetOuraSleep")]
    public async Task<HttpResponseData> GetSleep(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "oura/sleep")] HttpRequestData req)
    {
        var data = await _service.GetAllDataAsync();
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            dailySleep = data.DailySleep.OrderByDescending(s => s.Day),
            sleepRecords = data.LongSleepRecords.OrderByDescending(s => s.Day)
        });
        return response;
    }

    [Function("GetOuraReadiness")]
    public async Task<HttpResponseData> GetReadiness(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "oura/readiness")] HttpRequestData req)
    {
        var data = await _service.GetAllDataAsync();
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(data.Readiness.OrderByDescending(r => r.Day));
        return response;
    }

    [Function("GetOuraActivity")]
    public async Task<HttpResponseData> GetActivity(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "oura/activity")] HttpRequestData req)
    {
        var data = await _service.GetAllDataAsync();
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(data.Activity.OrderByDescending(a => a.Day));
        return response;
    }

    [Function("GetOuraLatest")]
    public async Task<HttpResponseData> GetLatest(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "oura/latest")] HttpRequestData req)
    {
        var sleep = await _service.GetLatestSleepAsync();
        var readiness = await _service.GetLatestReadinessAsync();
        var activity = await _service.GetLatestActivityAsync();
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { sleep, readiness, activity });
        return response;
    }

    [Function("GetOuraStats")]
    public async Task<HttpResponseData> GetStats(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "oura/stats")] HttpRequestData req)
    {
        var data = await _service.GetAllDataAsync();
        var longSleepRecords = data.SleepRecords.Where(s => s.IsLongSleep).ToList();
        
        var cutoffDate = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
        
        var recentDailySleep = data.DailySleep
            .Where(s => s.Day.CompareTo(cutoffDate) >= 0 && s.Score.HasValue)
            .ToList();
        
        var recentReadiness = data.Readiness
            .Where(r => r.Day.CompareTo(cutoffDate) >= 0 && r.Score.HasValue)
            .ToList();
        
        var recentActivity = data.Activity
            .Where(a => a.Day.CompareTo(cutoffDate) >= 0 && a.Score.HasValue)
            .ToList();
        
        var recentSleepRecords = longSleepRecords
            .Where(s => s.Day.CompareTo(cutoffDate) >= 0 && s.TotalSleepDuration.HasValue)
            .ToList();
        
        // New data types
        var recentStress = data.DailyStress
            .Where(s => s.Day.CompareTo(cutoffDate) >= 0)
            .ToList();
        
        // Filter for stress records with complete data (has DaySummary)
        var completeStress = recentStress
            .Where(s => s.DaySummary != null)
            .ToList();
        
        var recentResilience = data.DailyResilience
            .Where(r => r.Day.CompareTo(cutoffDate) >= 0 && r.Level != null)
            .ToList();
        
        var recentVo2Max = data.Vo2Max
            .Where(v => v.Day.CompareTo(cutoffDate) >= 0 && v.Vo2Max.HasValue)
            .ToList();
        
        var recentCardioAge = data.CardiovascularAge
            .Where(c => c.Day.CompareTo(cutoffDate) >= 0 && c.VascularAge.HasValue)
            .ToList();
        
        var recentSpo2 = data.SpO2
            .Where(s => s.Day.CompareTo(cutoffDate) >= 0 && s.Spo2Percentage?.Average != null)
            .ToList();
        
        var recentWorkouts = data.Workouts
            .Where(w => w.Day?.CompareTo(cutoffDate) >= 0)
            .ToList();
        
        var recentSleepTime = data.SleepTime
            .Where(s => s.Day.CompareTo(cutoffDate) >= 0)
            .ToList();

        // Get latest values (most recent day)
        var latestSleep = recentDailySleep.OrderByDescending(s => s.Day).FirstOrDefault();
        var latestReadiness = recentReadiness.OrderByDescending(r => r.Day).FirstOrDefault();
        var latestActivity = recentActivity.OrderByDescending(a => a.Day).FirstOrDefault();
        var latestSleepRecord = recentSleepRecords.OrderByDescending(s => s.Day).FirstOrDefault();
        
        // Latest for new data types - use complete data records
        var latestStress = completeStress.OrderByDescending(s => s.Day).FirstOrDefault();
        var latestResilience = recentResilience.OrderByDescending(r => r.Day).FirstOrDefault();
        var latestVo2Max = recentVo2Max.OrderByDescending(v => v.Day).FirstOrDefault();
        var latestCardioAge = recentCardioAge.OrderByDescending(c => c.Day).FirstOrDefault();
        var latestSpo2 = recentSpo2.OrderByDescending(s => s.Day).FirstOrDefault();
        var latestSleepTime = recentSleepTime.OrderByDescending(s => s.Day).FirstOrDefault();

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            // Sleep Score stats
            sleepScore = new
            {
                current = latestSleep?.Score,
                min = recentDailySleep.Count > 0 ? recentDailySleep.Min(s => s.Score!.Value) : (int?)null,
                max = recentDailySleep.Count > 0 ? recentDailySleep.Max(s => s.Score!.Value) : (int?)null,
                avg = recentDailySleep.Count > 0 ? Math.Round(recentDailySleep.Average(s => s.Score!.Value), 0) : (double?)null
            },
            // Readiness Score stats
            readinessScore = new
            {
                current = latestReadiness?.Score,
                min = recentReadiness.Count > 0 ? recentReadiness.Min(r => r.Score!.Value) : (int?)null,
                max = recentReadiness.Count > 0 ? recentReadiness.Max(r => r.Score!.Value) : (int?)null,
                avg = recentReadiness.Count > 0 ? Math.Round(recentReadiness.Average(r => r.Score!.Value), 0) : (double?)null
            },
            // Activity Score stats
            activityScore = new
            {
                current = latestActivity?.Score,
                min = recentActivity.Count > 0 ? recentActivity.Min(a => a.Score!.Value) : (int?)null,
                max = recentActivity.Count > 0 ? recentActivity.Max(a => a.Score!.Value) : (int?)null,
                avg = recentActivity.Count > 0 ? Math.Round(recentActivity.Average(a => a.Score!.Value), 0) : (double?)null
            },
            // Sleep Duration stats (in hours)
            sleepDuration = new
            {
                current = latestSleepRecord?.TotalSleepDuration.HasValue == true 
                    ? Math.Round(latestSleepRecord.TotalSleepDuration!.Value / 3600.0, 1) : (double?)null,
                min = recentSleepRecords.Count > 0 
                    ? Math.Round(recentSleepRecords.Min(s => s.TotalSleepDuration!.Value) / 3600.0, 1) : (double?)null,
                max = recentSleepRecords.Count > 0 
                    ? Math.Round(recentSleepRecords.Max(s => s.TotalSleepDuration!.Value) / 3600.0, 1) : (double?)null,
                avg = recentSleepRecords.Count > 0 
                    ? Math.Round(recentSleepRecords.Average(s => s.TotalSleepDuration!.Value) / 3600.0, 1) : (double?)null
            },
            
            // --- NEW DATA TYPES ---
            
            // Daily Stress stats
            stress = new
            {
                currentStressHigh = latestStress?.StressHigh,
                currentRecoveryHigh = latestStress?.RecoveryHigh,
                daySummary = latestStress?.DaySummary,
                avgStressHigh = recentStress.Count > 0 && recentStress.Any(s => s.StressHigh.HasValue)
                    ? Math.Round(recentStress.Where(s => s.StressHigh.HasValue).Average(s => s.StressHigh!.Value), 0) : (double?)null,
                avgRecoveryHigh = recentStress.Count > 0 && recentStress.Any(s => s.RecoveryHigh.HasValue)
                    ? Math.Round(recentStress.Where(s => s.RecoveryHigh.HasValue).Average(s => s.RecoveryHigh!.Value), 0) : (double?)null
            },
            
            // Resilience stats
            resilience = new
            {
                currentLevel = latestResilience?.Level,
                contributors = latestResilience?.Contributors
            },
            
            // VO2 Max stats
            vo2Max = new
            {
                current = latestVo2Max?.Vo2Max,
                min = recentVo2Max.Count > 0 ? Math.Round(recentVo2Max.Min(v => v.Vo2Max!.Value), 1) : (double?)null,
                max = recentVo2Max.Count > 0 ? Math.Round(recentVo2Max.Max(v => v.Vo2Max!.Value), 1) : (double?)null,
                avg = recentVo2Max.Count > 0 ? Math.Round(recentVo2Max.Average(v => v.Vo2Max!.Value), 1) : (double?)null
            },
            
            // Cardiovascular Age stats
            cardiovascularAge = new
            {
                current = latestCardioAge?.VascularAge,
                actualAge = data.PersonalInfo?.Age,
                difference = latestCardioAge?.VascularAge != null && data.PersonalInfo?.Age != null
                    ? latestCardioAge.VascularAge - data.PersonalInfo.Age : (int?)null
            },
            
            // SpO2 stats
            spo2 = new
            {
                current = latestSpo2?.Spo2Percentage?.Average,
                breathingDisturbanceIndex = latestSpo2?.BreathingDisturbanceIndex,
                avg = recentSpo2.Count > 0 
                    ? Math.Round(recentSpo2.Where(s => s.Spo2Percentage?.Average != null).Average(s => s.Spo2Percentage!.Average!.Value), 1) 
                    : (double?)null
            },
            
            // Sleep Time Recommendations
            sleepTimeRecommendation = new
            {
                recommendation = latestSleepTime?.Recommendation,
                status = latestSleepTime?.Status,
                optimalBedtime = latestSleepTime?.OptimalBedtime != null ? new
                {
                    startTime = FormatBedtimeOffset(latestSleepTime.OptimalBedtime.StartOffset),
                    endTime = FormatBedtimeOffset(latestSleepTime.OptimalBedtime.EndOffset)
                } : null
            },
            
            // Workouts summary
            workouts = new
            {
                recentCount = recentWorkouts.Count,
                totalCalories = recentWorkouts.Where(w => w.Calories.HasValue).Sum(w => w.Calories!.Value),
                totalDistance = recentWorkouts.Where(w => w.Distance.HasValue).Sum(w => w.Distance!.Value),
                recentWorkouts = recentWorkouts
                    .OrderByDescending(w => w.StartDatetime)
                    .Take(5)
                    .Select(w => new
                    {
                        w.Activity,
                        w.Calories,
                        distance = w.Distance,
                        w.Intensity,
                        w.StartDatetime,
                        w.Source
                    })
            },
            
            // Legacy fields for backwards compatibility
            averageSleepScore = recentDailySleep.Count > 0 
                ? Math.Round(recentDailySleep.Average(s => s.Score!.Value), 1) : (double?)null,
            averageReadinessScore = recentReadiness.Count > 0 
                ? Math.Round(recentReadiness.Average(r => r.Score!.Value), 1) : (double?)null,
            averageActivityScore = recentActivity.Count > 0 
                ? Math.Round(recentActivity.Average(a => a.Score!.Value), 1) : (double?)null,
            averageSleepDuration = recentSleepRecords.Count > 0 
                ? Math.Round(recentSleepRecords.Average(s => s.TotalSleepDuration!.Value) / 3600.0, 1) : (double?)null,
            averageSteps = recentActivity.Count > 0 
                ? (int)Math.Round(recentActivity.Where(a => a.Steps.HasValue).DefaultIfEmpty().Average(a => a?.Steps ?? 0)) : 0,
            totalSleepRecords = data.DailySleep.Count,
            totalReadinessRecords = data.Readiness.Count,
            totalActivityRecords = data.Activity.Count,
            dataCount = data.DailySleep.Count + data.Readiness.Count + data.Activity.Count,
            lastSync = data.LastSync
        });
        return response;
    }
    
    /// <summary>
    /// Converts bedtime offset (seconds from midnight) to readable time string.
    /// </summary>
    private static string? FormatBedtimeOffset(int? offset)
    {
        if (offset == null) return null;
        
        var totalMinutes = offset.Value / 60;
        var hours = 24 + (totalMinutes / 60); // Handle negative offsets (before midnight)
        var minutes = Math.Abs(totalMinutes % 60);
        
        if (hours >= 24) hours -= 24;
        if (hours < 0) hours += 24;
        
        var period = hours >= 12 ? "PM" : "AM";
        var displayHours = hours > 12 ? hours - 12 : (hours == 0 ? 12 : hours);
        
        return $"{displayHours}:{minutes:D2} {period}";
    }

    [Function("GetSleepDetail")]
    public async Task<HttpResponseData> GetSleepDetail(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "oura/sleep/{id}")] HttpRequestData req,
        string id)
    {
        var sleepRecord = await _service.GetSleepRecordByIdAsync(id);
        
        if (sleepRecord == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { message = "Sleep record not found" });
            return notFound;
        }

        var dailySleep = await _service.GetDailySleepByDayAsync(sleepRecord.Day);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            sleepRecord,
            score = dailySleep?.Score,
            contributors = dailySleep?.Contributors
        });
        return response;
    }

    [Function("GetOuraStatus")]
    public async Task<HttpResponseData> GetStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "oura/status")] HttpRequestData req)
    {
        var data = await _service.GetAllDataAsync();
        var totalRecords = data.DailySleep.Count + data.Readiness.Count + data.Activity.Count +
                          data.DailyStress.Count + data.DailyResilience.Count + data.Vo2Max.Count +
                          data.CardiovascularAge.Count + data.Workouts.Count + data.SpO2.Count;
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            configured = true,
            lastSync = data.LastSync,
            dataCount = totalRecords,
            recordCounts = new Dictionary<string, int>
            {
                ["sleep"] = data.DailySleep.Count,
                ["readiness"] = data.Readiness.Count,
                ["activity"] = data.Activity.Count,
                ["stress"] = data.DailyStress.Count,
                ["resilience"] = data.DailyResilience.Count,
                ["vo2Max"] = data.Vo2Max.Count,
                ["cardiovascularAge"] = data.CardiovascularAge.Count,
                ["workouts"] = data.Workouts.Count,
                ["spo2"] = data.SpO2.Count
            }
        });
        return response;
    }
    
    [Function("GetOuraWorkouts")]
    public async Task<HttpResponseData> GetWorkouts(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "oura/workouts")] HttpRequestData req)
    {
        var data = await _service.GetAllDataAsync();
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        var json = JsonSerializer.Serialize(data.Workouts.OrderByDescending(w => w.StartDatetime), CamelCaseOptions);
        await response.WriteStringAsync(json);
        return response;
    }
    
    [Function("GetOuraStress")]
    public async Task<HttpResponseData> GetStress(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "oura/stress")] HttpRequestData req)
    {
        var data = await _service.GetAllDataAsync();
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        var json = JsonSerializer.Serialize(data.DailyStress.OrderByDescending(s => s.Day), CamelCaseOptions);
        await response.WriteStringAsync(json);
        return response;
    }
    
    [Function("GetOuraResilience")]
    public async Task<HttpResponseData> GetResilience(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "oura/resilience")] HttpRequestData req)
    {
        var data = await _service.GetAllDataAsync();
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        var json = JsonSerializer.Serialize(data.DailyResilience.OrderByDescending(r => r.Day), CamelCaseOptions);
        await response.WriteStringAsync(json);
        return response;
    }
}
