namespace HealthAggregatorV2.Functions.Infrastructure.ExternalApis.Oura;

/// <summary>
/// Client interface for Oura API v2.
/// </summary>
public interface IOuraApiClient
{
    /// <summary>
    /// Get daily sleep scores.
    /// </summary>
    Task<IEnumerable<OuraSleepData>> GetDailySleepAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get detailed sleep documents (sleep periods).
    /// </summary>
    Task<IEnumerable<OuraSleepDocument>> GetSleepDocumentsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get daily activity data.
    /// </summary>
    Task<IEnumerable<OuraActivityData>> GetDailyActivityAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get daily readiness data.
    /// </summary>
    Task<IEnumerable<OuraReadinessData>> GetDailyReadinessAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}
