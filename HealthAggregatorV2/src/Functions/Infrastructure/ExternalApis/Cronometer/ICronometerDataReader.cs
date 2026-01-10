namespace HealthAggregatorV2.Functions.Infrastructure.ExternalApis.Cronometer;

/// <summary>
/// Interface for reading Cronometer CSV exports.
/// </summary>
public interface ICronometerDataReader
{
    /// <summary>
    /// Read daily nutrition data from CSV exports.
    /// </summary>
    /// <param name="startDate">Start date for data retrieval.</param>
    /// <param name="endDate">End date for data retrieval.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of daily nutrition records.</returns>
    Task<IEnumerable<CronometerNutritionData>> GetNutritionDataAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}
