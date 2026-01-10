using HealthAggregatorV2.Functions.Application.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HealthAggregatorV2.Functions.Triggers;

/// <summary>
/// Timer-triggered function for syncing data from all sources.
/// Runs every 30 minutes by default.
/// </summary>
public class SyncTimerFunction
{
    private readonly ISyncOrchestrator _syncOrchestrator;
    private readonly ILogger<SyncTimerFunction> _logger;

    public SyncTimerFunction(
        ISyncOrchestrator syncOrchestrator,
        ILogger<SyncTimerFunction> logger)
    {
        _syncOrchestrator = syncOrchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Timer trigger that syncs all enabled data sources.
    /// CRON: "0 */30 * * * *" = every 30 minutes
    /// </summary>
    [Function("SyncTimerFunction")]
    public async Task Run(
        [TimerTrigger("0 */30 * * * *")] TimerInfo timerInfo,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Sync timer function started at {Time}. Next run: {NextRun}",
            DateTime.UtcNow,
            timerInfo.ScheduleStatus?.Next);

        try
        {
            var result = await _syncOrchestrator.SyncAllSourcesAsync(cancellationToken);

            _logger.LogInformation(
                "Sync completed. Success: {SuccessCount}, Failed: {FailedCount}, Records: {TotalRecords}",
                result.SuccessCount,
                result.FailedCount,
                result.TotalRecordsSynced);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync timer function failed");
            throw; // Re-throw to trigger retry policy
        }
    }
}
