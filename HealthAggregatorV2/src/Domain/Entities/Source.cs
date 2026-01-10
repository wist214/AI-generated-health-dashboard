using HealthAggregatorV2.Domain.Common;

namespace HealthAggregatorV2.Domain.Entities;

/// <summary>
/// Represents a data source provider (e.g., Oura, Picooc, Cronometer).
/// </summary>
public class Source : BaseEntity
{
    /// <summary>
    /// Unique identifier for the source.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Name of the provider (e.g., "Oura", "Picooc", "Cronometer").
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this source is enabled for syncing.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// UTC timestamp of the last successful sync.
    /// </summary>
    public DateTime? LastSyncedAt { get; set; }

    /// <summary>
    /// Encrypted API key or token for authentication.
    /// </summary>
    public string? ApiKeyEncrypted { get; set; }

    /// <summary>
    /// JSON configuration for provider-specific settings.
    /// </summary>
    public string? ConfigurationJson { get; set; }

    // Navigation properties

    /// <summary>
    /// Collection of measurements from this source.
    /// </summary>
    public virtual ICollection<Measurement> Measurements { get; set; } = new List<Measurement>();
}
