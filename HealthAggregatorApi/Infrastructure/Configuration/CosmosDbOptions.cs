using System.ComponentModel.DataAnnotations;

namespace HealthAggregatorApi.Infrastructure.Configuration;

/// <summary>
/// Configuration options for Azure Cosmos DB connection.
/// Follows Options Pattern with validation.
/// </summary>
public class CosmosDbOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "CosmosDb";

    /// <summary>
    /// Cosmos DB connection string containing endpoint and key.
    /// Format: AccountEndpoint=https://...;AccountKey=...
    /// Not required when UseEmulator is true.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Name of the Cosmos DB database.
    /// Default: HealthAggregator
    /// </summary>
    [Required(ErrorMessage = "Database name is required")]
    public string DatabaseName { get; set; } = "HealthAggregator";

    /// <summary>
    /// Container name for Oura Ring data.
    /// Default: OuraData
    /// </summary>
    [Required]
    public string OuraContainerName { get; set; } = "OuraData";

    /// <summary>
    /// Container name for Picooc scale data.
    /// Default: PicoocData
    /// </summary>
    [Required]
    public string PicoocContainerName { get; set; } = "PicoocData";

    /// <summary>
    /// Container name for Cronometer nutrition data.
    /// Default: CronometerData
    /// </summary>
    [Required]
    public string CronometerContainerName { get; set; } = "CronometerData";

    /// <summary>
    /// Container name for user settings.
    /// Default: UserSettings
    /// </summary>
    [Required]
    public string UserSettingsContainerName { get; set; } = "UserSettings";

    /// <summary>
    /// Maximum retry attempts for transient failures.
    /// Default: 3
    /// </summary>
    [Range(0, 10, ErrorMessage = "Max retry attempts must be between 0 and 10")]
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Maximum retry wait time in seconds.
    /// Default: 30 seconds
    /// </summary>
    [Range(1, 300, ErrorMessage = "Max retry wait time must be between 1 and 300 seconds")]
    public int MaxRetryWaitTimeSeconds { get; set; } = 30;

    /// <summary>
    /// Default partition key value for single-user scenario.
    /// Default: default
    /// </summary>
    public string DefaultPartitionKey { get; set; } = "default";

    /// <summary>
    /// Whether to use the local Cosmos DB emulator.
    /// Default: false
    /// </summary>
    public bool UseEmulator { get; set; } = false;

    /// <summary>
    /// Gets the emulator connection string.
    /// This is the well-known connection string for the Cosmos DB emulator.
    /// Using 127.0.0.1 instead of localhost for better Docker compatibility.
    /// </summary>
    public static string EmulatorConnectionString =>
        "AccountEndpoint=https://127.0.0.1:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    /// <summary>
    /// Gets the effective connection string (emulator or configured).
    /// </summary>
    public string EffectiveConnectionString =>
        UseEmulator ? EmulatorConnectionString : ConnectionString;

    /// <summary>
    /// Validates the configuration options.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString) && !UseEmulator)
        {
            throw new InvalidOperationException(
                "CosmosDb:ConnectionString must be configured when not using the emulator.");
        }

        if (string.IsNullOrWhiteSpace(DatabaseName))
        {
            throw new InvalidOperationException("CosmosDb:DatabaseName must be configured.");
        }
    }
}
