namespace HealthAggregatorV2.Functions.Infrastructure.ExternalApis.Picooc;

/// <summary>
/// Configuration options for Picooc/SmartScaleConnect integration.
/// </summary>
public class PicoocApiOptions
{
    public const string SectionName = "Picooc";

    /// <summary>
    /// Picooc account email.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Picooc account password.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Docker container name for SmartScaleConnect.
    /// </summary>
    public string DockerContainerName { get; set; } = "smartscaleconnect";

    /// <summary>
    /// Number of days to backfill on initial sync.
    /// </summary>
    public int InitialBackfillDays { get; set; } = 30;
}
