using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using HealthAggregatorApi.Core.Interfaces;
using System.Net;

namespace HealthAggregatorApi.Functions;

/// <summary>
/// HTTP-triggered functions for Dashboard aggregation endpoints.
/// </summary>
public class DashboardFunctions
{
    private readonly IOuraDataService _ouraService;
    private readonly IPicoocDataService _picoocService;

    public DashboardFunctions(IOuraDataService ouraService, IPicoocDataService picoocService)
    {
        _ouraService = ouraService;
        _picoocService = picoocService;
    }

    [Function("GetDashboard")]
    public async Task<HttpResponseData> GetDashboard(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dashboard")] HttpRequestData req)
    {
        var ouraData = await _ouraService.GetAllDataAsync();
        var picoocData = await _picoocService.GetAllMeasurementsAsync();
        var latestPicooc = await _picoocService.GetLatestMeasurementAsync();
        var latestSleep = await _ouraService.GetLatestSleepAsync();
        var latestReadiness = await _ouraService.GetLatestReadinessAsync();
        var latestActivity = await _ouraService.GetLatestActivityAsync();

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            oura = new
            {
                dailySleep = ouraData.DailySleep,
                readiness = ouraData.Readiness,
                activity = ouraData.Activity,
                lastSync = ouraData.LastSync
            },
            picooc = new
            {
                measurements = picoocData,
                lastSync = await _picoocService.GetLastSyncTimeAsync()
            },
            latest = new
            {
                weight = latestPicooc?.Weight,
                bodyFat = latestPicooc?.BodyFat,
                weightDate = latestPicooc?.Date,
                sleepScore = latestSleep?.Score,
                sleepDate = latestSleep?.Day,
                readinessScore = latestReadiness?.Score,
                readinessDate = latestReadiness?.Day,
                activityScore = latestActivity?.Score,
                steps = latestActivity?.Steps,
                activityDate = latestActivity?.Day
            }
        });
        return response;
    }

    [Function("GetDashboardMetrics")]
    public async Task<HttpResponseData> GetMetrics(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dashboard/metrics")] HttpRequestData req)
    {
        var latestPicooc = await _picoocService.GetLatestMeasurementAsync();
        var latestSleep = await _ouraService.GetLatestSleepAsync();
        var latestReadiness = await _ouraService.GetLatestReadinessAsync();
        var latestActivity = await _ouraService.GetLatestActivityAsync();
        var averages = await _ouraService.GetAverageScoresAsync(30);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            weight = new
            {
                value = latestPicooc?.Weight,
                date = latestPicooc?.Date
            },
            bodyFat = new
            {
                value = latestPicooc?.BodyFat,
                date = latestPicooc?.Date
            },
            sleep = new
            {
                score = latestSleep?.Score,
                date = latestSleep?.Day,
                average = averages.GetValueOrDefault("sleep")
            },
            readiness = new
            {
                score = latestReadiness?.Score,
                date = latestReadiness?.Day,
                average = averages.GetValueOrDefault("readiness")
            },
            activity = new
            {
                score = latestActivity?.Score,
                steps = latestActivity?.Steps,
                date = latestActivity?.Day,
                average = averages.GetValueOrDefault("activity")
            }
        });
        return response;
    }
}
