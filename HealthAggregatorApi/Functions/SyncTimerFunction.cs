using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using HealthAggregatorApi.Core.Interfaces;

namespace HealthAggregatorApi.Functions;

/// <summary>
/// Timer-triggered function for automatic hourly sync.
/// </summary>
public class SyncTimerFunction
{
    private readonly IOuraDataService _ouraService;
    private readonly IPicoocDataService _picoocService;
    private readonly ICronometerDataService _cronometerService;
    private readonly ILogger<SyncTimerFunction> _logger;

    public SyncTimerFunction(
        IOuraDataService ouraService,
        IPicoocDataService picoocService,
        ICronometerDataService cronometerService,
        ILogger<SyncTimerFunction> logger)
    {
        _ouraService = ouraService;
        _picoocService = picoocService;
        _cronometerService = cronometerService;
        _logger = logger;
    }

    /// <summary>
    /// Runs every hour at minute 0 (e.g., 1:00, 2:00, 3:00).
    /// CRON: second minute hour day month day-of-week
    /// "0 0 * * * *" = At second 0, minute 0, every hour
    /// </summary>
    [Function("SyncTimer")]
    public async Task Run([TimerTrigger("0 0 * * * *")] TimerInfo timer)
    {
        _logger.LogInformation("=== Hourly sync started at {Time} ===", DateTime.UtcNow);

        // Sync Oura (last 7 days to catch any delayed data)
        try
        {
            var endDate = DateTime.UtcNow.AddDays(1);
            var startDate = DateTime.UtcNow.AddDays(-7);
            
            _logger.LogInformation("Syncing Oura data from {Start} to {End}", startDate, endDate);
            var ouraData = await _ouraService.SyncDataAsync(startDate, endDate);
            _logger.LogInformation("Oura sync completed: {Sleep} sleep, {Readiness} readiness, {Activity} activity records",
                ouraData.DailySleep.Count, ouraData.Readiness.Count, ouraData.Activity.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Oura sync failed");
        }

        // Sync Picooc
        try
        {
            if (_picoocService.IsConfigured())
            {
                _logger.LogInformation("Syncing Picooc data...");
                var measurements = await _picoocService.SyncDataAsync();
                _logger.LogInformation("Picooc sync completed: {Count} measurements", measurements.Count);
            }
            else
            {
                _logger.LogWarning("Picooc not configured, skipping sync");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Picooc sync failed");
        }

        // Sync Cronometer (last 7 days)
        try
        {
            var endDate = DateTime.UtcNow.AddDays(1);
            var startDate = DateTime.UtcNow.AddDays(-7);
            
            _logger.LogInformation("Syncing Cronometer data from {Start} to {End}", startDate, endDate);
            var cronometerData = await _cronometerService.SyncDataAsync(startDate, endDate);
            _logger.LogInformation("Cronometer sync completed: {Nutrition} nutrition, {Servings} serving records",
                cronometerData.DailyNutrition.Count, cronometerData.Servings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cronometer sync failed");
        }

        _logger.LogInformation("=== Hourly sync completed at {Time} ===", DateTime.UtcNow);

        if (timer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next sync scheduled for: {Next}", timer.ScheduleStatus.Next);
        }
    }
}
