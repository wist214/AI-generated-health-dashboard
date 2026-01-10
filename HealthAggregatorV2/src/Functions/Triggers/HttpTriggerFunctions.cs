using HealthAggregatorV2.Functions.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HealthAggregatorV2.Functions.Triggers;

/// <summary>
/// HTTP-triggered functions for manual sync operations and health checks.
/// </summary>
public class HttpTriggerFunctions
{
    private readonly ISyncOrchestrator _syncOrchestrator;
    private readonly ILogger<HttpTriggerFunctions> _logger;

    public HttpTriggerFunctions(
        ISyncOrchestrator syncOrchestrator,
        ILogger<HttpTriggerFunctions> logger)
    {
        _syncOrchestrator = syncOrchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint.
    /// </summary>
    [Function("HealthCheck")]
    public IActionResult HealthCheck(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req)
    {
        return new OkObjectResult(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "HealthAggregatorV2.Functions"
        });
    }

    /// <summary>
    /// Manually trigger sync for all sources.
    /// </summary>
    [Function("SyncAll")]
    public async Task<IActionResult> SyncAll(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "sync")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Manual sync triggered for all sources");

        try
        {
            var result = await _syncOrchestrator.SyncAllSourcesAsync(cancellationToken);

            return new OkObjectResult(new
            {
                success = result.FailedCount == 0,
                successCount = result.SuccessCount,
                failedCount = result.FailedCount,
                totalRecordsSynced = result.TotalRecordsSynced,
                completedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual sync failed");
            return new ObjectResult(new
            {
                success = false,
                error = ex.Message
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    /// <summary>
    /// Manually trigger sync for a specific source.
    /// </summary>
    [Function("SyncSource")]
    public async Task<IActionResult> SyncSource(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "sync/{sourceName}")] HttpRequest req,
        string sourceName,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Manual sync triggered for source: {SourceName}", sourceName);

        try
        {
            var result = await _syncOrchestrator.SyncSourceAsync(sourceName, cancellationToken);

            return new OkObjectResult(new
            {
                success = result.IsSuccess,
                sourceName = sourceName,
                recordsSynced = result.RecordsSynced,
                errorMessage = result.ErrorMessage,
                startedAt = result.StartedAt,
                completedAt = result.CompletedAt,
                metadata = result.Metadata
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Source not found: {SourceName}", sourceName);
            return new NotFoundObjectResult(new
            {
                success = false,
                error = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed for source: {SourceName}", sourceName);
            return new ObjectResult(new
            {
                success = false,
                error = ex.Message
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
