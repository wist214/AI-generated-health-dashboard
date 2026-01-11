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

    /// <summary>
    /// Get daily stress data.
    /// </summary>
    Task<IEnumerable<OuraStressData>> GetDailyStressAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get daily resilience data.
    /// </summary>
    Task<IEnumerable<OuraResilienceData>> GetDailyResilienceAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get VO2 Max data.
    /// </summary>
    Task<IEnumerable<OuraVo2MaxData>> GetVo2MaxAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get cardiovascular age data.
    /// </summary>
    Task<IEnumerable<OuraCardiovascularAgeData>> GetCardiovascularAgeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get daily SpO2 data (Gen 3 only).
    /// </summary>
    Task<IEnumerable<OuraSpO2Data>> GetDailySpO2Async(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get sleep time recommendations.
    /// </summary>
    Task<IEnumerable<OuraSleepTimeData>> GetSleepTimeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get workout data.
    /// </summary>
    Task<IEnumerable<OuraWorkoutData>> GetWorkoutsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}
