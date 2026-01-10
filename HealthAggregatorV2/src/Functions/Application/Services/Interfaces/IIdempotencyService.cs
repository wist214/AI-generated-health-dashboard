namespace HealthAggregatorV2.Functions.Application.Services.Interfaces;

/// <summary>
/// Service for checking duplicate measurements to ensure idempotent sync.
/// </summary>
public interface IIdempotencyService
{
    /// <summary>
    /// Check if a measurement already exists in the database.
    /// </summary>
    Task<bool> IsDuplicateAsync(
        int metricTypeId,
        long sourceId,
        DateTime timestamp,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark a measurement as processed (cache the key).
    /// </summary>
    void MarkAsProcessed(int metricTypeId, long sourceId, DateTime timestamp);
}
