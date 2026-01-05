using HealthAggregator.Api.DTOs;
using HealthAggregator.Core.DTOs;
using HealthAggregator.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HealthAggregator.Api.Endpoints;

/// <summary>
/// Extension methods to map API endpoints.
/// Uses minimal API approach with clean separation.
/// </summary>
public static class EndpointExtensions
{
    public static WebApplication MapHealthAggregatorEndpoints(this WebApplication app)
    {
        app.MapDashboardEndpoints();
        app.MapPicoocEndpoints();
        app.MapOuraEndpoints();
        
        return app;
    }

    private static void MapDashboardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/dashboard").WithTags("Dashboard");

        group.MapGet("/", async ([FromServices] IDashboardService service) =>
        {
            var dashboard = await service.GetDashboardDataAsync();
            return Results.Ok(dashboard);
        }).WithName("GetDashboard");

        group.MapGet("/metrics", async ([FromServices] IDashboardService service) =>
        {
            var metrics = await service.GetMetricCardsAsync();
            return Results.Ok(metrics);
        }).WithName("GetMetrics");

        group.MapGet("/progress", async ([FromServices] IDashboardService service) =>
        {
            var progress = await service.GetProgressComparisonAsync();
            return Results.Ok(progress);
        }).WithName("GetProgress");

        group.MapGet("/trends", async ([FromServices] IDashboardService service) =>
        {
            var trends = await service.GetWeeklyTrendsAsync();
            return Results.Ok(trends);
        }).WithName("GetWeeklyTrends");

        group.MapGet("/insights", async ([FromServices] IDashboardService service) =>
        {
            var insights = await service.GenerateInsightsAsync();
            return Results.Ok(insights);
        }).WithName("GetInsights");

        group.MapGet("/stats", async ([FromServices] IDashboardService service) =>
        {
            var stats = await service.GetQuickStatsAsync();
            return Results.Ok(stats);
        }).WithName("GetQuickStats");
    }

    private static void MapPicoocEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/picooc").WithTags("Picooc");

        group.MapGet("/data", async (
            [FromServices] IPicoocDataService service,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate) =>
        {
            var measurements = await service.GetAllMeasurementsAsync();
            
            var filtered = measurements
                .Where(m => !startDate.HasValue || m.Date >= startDate.Value)
                .Where(m => !endDate.HasValue || m.Date <= endDate.Value)
                .OrderByDescending(m => m.Date)
                .ToList();

            return Results.Ok(new PicoocSyncResponseDto
            {
                Success = true,
                Data = filtered,
                Count = filtered.Count
            });
        }).WithName("GetPicoocData");

        group.MapPost("/sync", async ([FromServices] IPicoocDataService service) =>
        {
            try
            {
                var measurements = await service.SyncDataAsync();
                return Results.Ok(new PicoocSyncResponseDto
                {
                    Success = true,
                    Data = measurements,
                    Count = measurements.Count,
                    Message = "Sync completed successfully"
                });
            }
            catch (Exception ex)
            {
                return Results.Ok(new PicoocSyncResponseDto
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }).WithName("SyncPicoocData");

        group.MapGet("/latest", async ([FromServices] IPicoocDataService service) =>
        {
            var latest = await service.GetLatestMeasurementAsync();
            if (latest == null)
                return Results.NotFound(new { message = "No measurements found" });
            
            return Results.Ok(latest);
        }).WithName("GetLatestPicoocMeasurement");

        group.MapGet("/stats", async (
            [FromServices] IPicoocDataService service,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate) =>
        {
            var stats = await service.GetStatsAsync(startDate, endDate);
            return Results.Ok(new PicoocStatsDto
            {
                Success = true,
                Stats = stats
            });
        }).WithName("GetPicoocStats");

        group.MapGet("/status", async ([FromServices] IPicoocDataService service, [FromServices] IPicoocSyncClient syncClient) =>
        {
            var isConfigured = syncClient.IsConfigured();
            var lastSync = await service.GetLastSyncTimeAsync();
            var measurements = await service.GetAllMeasurementsAsync();
            
            return Results.Ok(new PicoocStatusDto
            {
                Configured = isConfigured,
                HasCredentials = isConfigured,
                DockerAvailable = false, // No longer using Docker
                DataCount = measurements.Count,
                SyncMethod = isConfigured ? "native" : "none",
                LastSync = lastSync,
                Status = isConfigured ? "ready" : "not_configured"
            });
        }).WithName("GetPicoocStatus");
    }

    private static void MapOuraEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/oura").WithTags("Oura");

        group.MapGet("/data", async ([FromServices] IOuraDataService service) =>
        {
            var data = await service.GetAllDataAsync();
            // Return data directly for easier frontend consumption
            return Results.Ok(new
            {
                sleepRecords = data.SleepRecords,
                dailySleep = data.DailySleep,
                readiness = data.Readiness,
                activity = data.Activity,
                personalInfo = data.PersonalInfo,
                lastSync = data.LastSync
            });
        }).WithName("GetOuraData");

        group.MapPost("/sync", async (
            [FromServices] IOuraDataService service,
            [FromQuery] int? days) =>
        {
            try
            {
                // Oura API end_date is exclusive, so add 1 day to include today's data
                var endDate = DateTime.UtcNow.AddDays(1);
                var startDate = DateTime.UtcNow.AddDays(-(days ?? 30));
                
                var data = await service.SyncDataAsync(startDate, endDate);
                // Return data in a format consistent with loadOuraData expectations
                return Results.Ok(new
                {
                    success = true,
                    data = new
                    {
                        sleepRecords = data.SleepRecords,
                        dailySleep = data.DailySleep,
                        readiness = data.Readiness,
                        activity = data.Activity,
                        personalInfo = data.PersonalInfo,
                        lastSync = data.LastSync
                    },
                    lastSync = data.LastSync,
                    message = "Sync completed successfully",
                    sleepCount = data.DailySleep.Count,
                    readinessCount = data.Readiness.Count,
                    activityCount = data.Activity.Count
                });
            }
            catch (Exception ex)
            {
                return Results.Ok(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }).WithName("SyncOuraData");

        group.MapGet("/sleep", async ([FromServices] IOuraDataService service) =>
        {
            var data = await service.GetAllDataAsync();
            return Results.Ok(new
            {
                dailySleep = data.DailySleep.OrderByDescending(s => s.Day),
                sleepRecords = data.LongSleepRecords.OrderByDescending(s => s.Day)
            });
        }).WithName("GetOuraSleepData");

        group.MapGet("/readiness", async ([FromServices] IOuraDataService service) =>
        {
            var data = await service.GetAllDataAsync();
            return Results.Ok(data.Readiness.OrderByDescending(r => r.Day));
        }).WithName("GetOuraReadinessData");

        group.MapGet("/activity", async ([FromServices] IOuraDataService service) =>
        {
            var data = await service.GetAllDataAsync();
            return Results.Ok(data.Activity.OrderByDescending(a => a.Day));
        }).WithName("GetOuraActivityData");

        group.MapGet("/latest", async ([FromServices] IOuraDataService service) =>
        {
            var sleep = await service.GetLatestSleepAsync();
            var readiness = await service.GetLatestReadinessAsync();
            var activity = await service.GetLatestActivityAsync();
            
            return Results.Ok(new
            {
                sleep,
                readiness,
                activity
            });
        }).WithName("GetLatestOuraData");

        group.MapGet("/averages", async (
            [FromServices] IOuraDataService service,
            [FromQuery] int? days) =>
        {
            var averages = await service.GetAverageScoresAsync(days ?? 30);
            return Results.Ok(averages);
        }).WithName("GetOuraAverages");

        group.MapGet("/status", async ([FromServices] IOuraDataService service) =>
        {
            var data = await service.GetAllDataAsync();
            return Results.Ok(new OuraStatusDto
            {
                Configured = true,
                LastSync = data.LastSync,
                RecordCounts = new Dictionary<string, int>
                {
                    ["sleep"] = data.DailySleep.Count,
                    ["readiness"] = data.Readiness.Count,
                    ["activity"] = data.Activity.Count
                }
            });
        }).WithName("GetOuraStatus");

        // Sleep detail endpoint - returns detailed sleep record with contributors
        group.MapGet("/sleep/{id}", async (
            [FromServices] IOuraDataService service,
            string id) =>
        {
            var sleepRecord = await service.GetSleepRecordByIdAsync(id);
            if (sleepRecord == null)
                return Results.NotFound(new { message = "Sleep record not found" });

            // Get the daily sleep record for this day to get score and contributors
            var dailySleep = await service.GetDailySleepByDayAsync(sleepRecord.Day);

            return Results.Ok(new
            {
                sleepRecord,
                score = dailySleep?.Score,
                contributors = dailySleep?.Contributors
            });
        }).WithName("GetSleepDetail");

        // Stats endpoint for dashboard compatibility
        group.MapGet("/stats", async ([FromServices] IOuraDataService service) =>
        {
            var data = await service.GetAllDataAsync();
            var longSleepRecords = data.SleepRecords.Where(s => s.IsLongSleep).ToList();
            
            // Calculate averages from last 30 days
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

            return Results.Ok(new
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
                firstDate = data.DailySleep.OrderBy(s => s.Day).FirstOrDefault()?.Day,
                lastDate = data.DailySleep.OrderByDescending(s => s.Day).FirstOrDefault()?.Day,
                dataCount = data.DailySleep.Count + data.Readiness.Count + data.Activity.Count,
                lastSync = data.LastSync
            });
        }).WithName("GetOuraStats");
    }
}
