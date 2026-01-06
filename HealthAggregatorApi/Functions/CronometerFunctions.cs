using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using HealthAggregatorApi.Core.Interfaces;
using System.Net;

namespace HealthAggregatorApi.Functions;

/// <summary>
/// HTTP-triggered functions for Cronometer nutrition data endpoints.
/// </summary>
public class CronometerFunctions
{
    private readonly ICronometerDataService _service;
    private readonly ILogger<CronometerFunctions> _logger;

    public CronometerFunctions(ICronometerDataService service, ILogger<CronometerFunctions> logger)
    {
        _service = service;
        _logger = logger;
    }

    [Function("GetCronometerData")]
    public async Task<HttpResponseData> GetData(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cronometer/data")] HttpRequestData req)
    {
        _logger.LogInformation("GetCronometerData called");
        
        var data = await _service.GetAllDataAsync();
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            dailyNutrition = data.DailyNutrition.OrderByDescending(d => d.Date),
            foodServings = data.FoodServings.OrderByDescending(s => s.Day),
            exercises = data.Exercises.OrderByDescending(e => e.Day),
            lastSync = data.LastSync
        });
        return response;
    }

    [Function("SyncCronometerData")]
    public async Task<HttpResponseData> Sync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cronometer/sync")] HttpRequestData req)
    {
        _logger.LogInformation("SyncCronometerData called");
        
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
                nutritionCount = data.DailyNutrition.Count,
                servingsCount = data.FoodServings.Count,
                exercisesCount = data.Exercises.Count,
                lastSync = data.LastSync
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cronometer sync failed");
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = false,
                error = ex.Message
            });
            return response;
        }
    }

    [Function("GetCronometerDaily")]
    public async Task<HttpResponseData> GetDaily(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cronometer/daily/{date}")] HttpRequestData req,
        string date)
    {
        _logger.LogInformation("GetCronometerDaily called for {Date}", date);
        
        var nutrition = await _service.GetDailyNutritionAsync(date);
        var servings = await _service.GetServingsForDateAsync(date);
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            date,
            nutrition,
            servings
        });
        return response;
    }

    [Function("GetCronometerLatest")]
    public async Task<HttpResponseData> GetLatest(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cronometer/latest")] HttpRequestData req)
    {
        _logger.LogInformation("GetCronometerLatest called");
        
        var nutrition = await _service.GetLatestNutritionAsync();
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(nutrition);
        return response;
    }

    [Function("GetCronometerStats")]
    public async Task<HttpResponseData> GetStats(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cronometer/stats")] HttpRequestData req)
    {
        _logger.LogInformation("GetCronometerStats called");
        
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var days = int.TryParse(query["days"], out int d) ? d : 30;
        
        var stats = await _service.GetStatsAsync(days);
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(stats);
        return response;
    }

    [Function("GetCronometerStatus")]
    public async Task<HttpResponseData> GetStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cronometer/status")] HttpRequestData req)
    {
        _logger.LogInformation("GetCronometerStatus called");
        
        var data = await _service.GetAllDataAsync();
        var hasCredentials = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("Cronometer__Email"));
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            configured = hasCredentials,
            hasCredentials,
            lastSync = data.LastSync,
            dataCount = data.DailyNutrition.Count + data.FoodServings.Count,
            recordCounts = new Dictionary<string, int>
            {
                ["nutrition"] = data.DailyNutrition.Count,
                ["servings"] = data.FoodServings.Count,
                ["exercises"] = data.Exercises.Count
            }
        });
        return response;
    }
}
