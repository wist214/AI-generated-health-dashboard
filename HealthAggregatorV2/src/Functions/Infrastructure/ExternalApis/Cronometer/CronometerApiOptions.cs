namespace HealthAggregatorV2.Functions.Infrastructure.ExternalApis.Cronometer;

/// <summary>
/// Configuration options for Cronometer integration.
/// </summary>
public class CronometerApiOptions
{
    public const string SectionName = "Cronometer";

    /// <summary>
    /// Path to the directory containing Cronometer CSV exports.
    /// </summary>
    public string ExportsPath { get; set; } = "./exports";

    /// <summary>
    /// Number of days to backfill on initial sync.
    /// </summary>
    public int InitialBackfillDays { get; set; } = 30;
}
