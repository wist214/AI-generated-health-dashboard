using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using HealthAggregatorApi.Core.Interfaces;
using System.Net;

namespace HealthAggregatorApi.Functions;

/// <summary>
/// HTTP-triggered functions for Oura data endpoints.
/// </summary>
public class OuraFunctions
{
    private readonly IOuraDataService _service;
    private readonly ILogger<OuraFunctions> _logger;

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

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
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
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            configured = true,
            lastSync = data.LastSync,
            recordCounts = new Dictionary<string, int>
            {
                ["sleep"] = data.DailySleep.Count,
                ["readiness"] = data.Readiness.Count,
                ["activity"] = data.Activity.Count
            }
        });
        return response;
    }
}
