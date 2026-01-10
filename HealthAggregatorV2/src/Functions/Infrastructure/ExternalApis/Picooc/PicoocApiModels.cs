using System.Text.Json.Serialization;

namespace HealthAggregatorV2.Functions.Infrastructure.ExternalApis.Picooc;

/// <summary>
/// Response from SmartScaleConnect tool.
/// </summary>
public class PicoocSyncResponse
{
    [JsonPropertyName("measurements")]
    public List<PicoocMeasurement> Measurements { get; set; } = new();

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>
/// A single measurement from Picooc scale.
/// </summary>
public class PicoocMeasurement
{
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("weight")]
    public decimal? Weight { get; set; }

    [JsonPropertyName("bmi")]
    public decimal? Bmi { get; set; }

    [JsonPropertyName("body_fat")]
    public decimal? BodyFat { get; set; }

    [JsonPropertyName("muscle_mass")]
    public decimal? MuscleMass { get; set; }

    [JsonPropertyName("bone_mass")]
    public decimal? BoneMass { get; set; }

    [JsonPropertyName("water")]
    public decimal? Water { get; set; }

    [JsonPropertyName("visceral_fat")]
    public decimal? VisceralFat { get; set; }

    [JsonPropertyName("bmr")]
    public decimal? Bmr { get; set; }

    [JsonPropertyName("protein")]
    public decimal? Protein { get; set; }

    [JsonPropertyName("body_age")]
    public int? BodyAge { get; set; }
}
