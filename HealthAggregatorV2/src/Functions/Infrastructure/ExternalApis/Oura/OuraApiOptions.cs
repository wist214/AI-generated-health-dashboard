namespace HealthAggregatorV2.Functions.Infrastructure.ExternalApis.Oura;

/// <summary>
/// Configuration options for Oura API.
/// </summary>
public class OuraApiOptions
{
    public const string SectionName = "Oura";

    /// <summary>
    /// Personal Access Token for Oura API v2.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for Oura API (default: https://api.ouraring.com/v2/).
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.ouraring.com/v2/";

    /// <summary>
    /// Number of days to backfill on first sync (default: 30).
    /// </summary>
    public int InitialBackfillDays { get; set; } = 30;
}
