using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using HealthAggregatorApi.Core.Interfaces;
using System.Net;

namespace HealthAggregatorApi.Functions;

/// <summary>
/// HTTP-triggered functions for Picooc data endpoints.
/// </summary>
public class PicoocFunctions
{
    private readonly IPicoocDataService _service;
    private readonly ILogger<PicoocFunctions> _logger;

    public PicoocFunctions(IPicoocDataService service, ILogger<PicoocFunctions> logger)
    {
        _service = service;
        _logger = logger;
    }

    [Function("GetPicoocData")]
    public async Task<HttpResponseData> GetData(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "picooc/data")] HttpRequestData req)
    {
        _logger.LogInformation("GetPicoocData called");
        
        var measurements = await _service.GetAllMeasurementsAsync();
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            success = true,
            data = measurements,
            count = measurements.Count
        });
        return response;
    }

    [Function("SyncPicoocData")]
    public async Task<HttpResponseData> Sync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "picooc/sync")] HttpRequestData req)
    {
        _logger.LogInformation("SyncPicoocData called");
        
        try
        {
            var measurements = await _service.SyncDataAsync();
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                data = measurements,
                count = measurements.Count,
                message = "Sync completed successfully"
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Picooc sync failed");
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = false,
                message = ex.Message
            });
            return response;
        }
    }

    [Function("GetPicoocLatest")]
    public async Task<HttpResponseData> GetLatest(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "picooc/latest")] HttpRequestData req)
    {
        var latest = await _service.GetLatestMeasurementAsync();
        
        if (latest == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { message = "No measurements found" });
            return notFound;
        }
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(latest);
        return response;
    }

    [Function("GetPicoocStatus")]
    public async Task<HttpResponseData> GetStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "picooc/status")] HttpRequestData req)
    {
        var isConfigured = _service.IsConfigured();
        var lastSync = await _service.GetLastSyncTimeAsync();
        var measurements = await _service.GetAllMeasurementsAsync();
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            configured = isConfigured,
            hasCredentials = isConfigured,
            dataCount = measurements.Count,
            syncMethod = isConfigured ? "native" : "none",
            lastSync = lastSync,
            status = isConfigured ? "ready" : "not_configured"
        });
        return response;
    }

    [Function("GetPicoocStats")]
    public async Task<HttpResponseData> GetStats(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "picooc/stats")] HttpRequestData req)
    {
        var measurements = await _service.GetAllMeasurementsAsync();
        
        if (measurements.Count == 0)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { });
            return response;
        }
        
        // Calculate stats
        var weights = measurements.Select(m => m.Weight).Where(w => w > 0).ToList();
        var bodyFats = measurements.Select(m => m.BodyFat).Where(b => b > 0).ToList();
        var bmis = measurements.Select(m => m.BMI).Where(b => b > 0).ToList();
        var muscles = measurements.Select(m => m.SkeletalMuscleMass).Where(m => m > 0).ToList();
        
        var stats = req.CreateResponse(HttpStatusCode.OK);
        await stats.WriteAsJsonAsync(new
        {
            weight = weights.Count > 0 ? new {
                current = Math.Round(weights.First(), 1),
                min = Math.Round(weights.Min(), 1),
                max = Math.Round(weights.Max(), 1),
                avg = Math.Round(weights.Average(), 1)
            } : null,
            bodyFat = bodyFats.Count > 0 ? new {
                current = Math.Round(bodyFats.First(), 1),
                min = Math.Round(bodyFats.Min(), 1),
                max = Math.Round(bodyFats.Max(), 1),
                avg = Math.Round(bodyFats.Average(), 1)
            } : null,
            bmi = bmis.Count > 0 ? new {
                current = Math.Round(bmis.First(), 1),
                min = Math.Round(bmis.Min(), 1),
                max = Math.Round(bmis.Max(), 1),
                avg = Math.Round(bmis.Average(), 1)
            } : null,
            muscle = muscles.Count > 0 ? new {
                current = Math.Round(muscles.First(), 1),
                min = Math.Round(muscles.Min(), 1),
                max = Math.Round(muscles.Max(), 1),
                avg = Math.Round(muscles.Average(), 1)
            } : null
        });
        return stats;
    }
}
