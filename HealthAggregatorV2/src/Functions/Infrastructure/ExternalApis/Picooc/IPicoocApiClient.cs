namespace HealthAggregatorV2.Functions.Infrastructure.ExternalApis.Picooc;

/// <summary>
/// Interface for Picooc/SmartScaleConnect data client.
/// </summary>
public interface IPicoocApiClient
{
    /// <summary>
    /// Fetch weight measurements from Picooc via SmartScaleConnect Docker container.
    /// </summary>
    /// <param name="startDate">Start date for data retrieval.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of weight measurements.</returns>
    Task<IEnumerable<PicoocMeasurement>> GetMeasurementsAsync(
        DateTime? startDate = null,
        CancellationToken cancellationToken = default);
}
