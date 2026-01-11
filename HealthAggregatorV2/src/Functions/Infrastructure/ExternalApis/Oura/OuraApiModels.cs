using System.Text.Json.Serialization;

namespace HealthAggregatorV2.Functions.Infrastructure.ExternalApis.Oura;

/// <summary>
/// Oura API response wrapper.
/// </summary>
public class OuraApiResponse<T>
{
    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = new();

    [JsonPropertyName("next_token")]
    public string? NextToken { get; set; }
}

/// <summary>
/// Oura sleep data from daily_sleep endpoint.
/// </summary>
public class OuraSleepData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("day")]
    public string Day { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public int? Score { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }

    // Sleep contributors
    [JsonPropertyName("contributors")]
    public OuraSleepContributors? Contributors { get; set; }
}

public class OuraSleepContributors
{
    [JsonPropertyName("deep_sleep")]
    public int? DeepSleep { get; set; }

    [JsonPropertyName("efficiency")]
    public int? Efficiency { get; set; }

    [JsonPropertyName("latency")]
    public int? Latency { get; set; }

    [JsonPropertyName("rem_sleep")]
    public int? RemSleep { get; set; }

    [JsonPropertyName("restfulness")]
    public int? Restfulness { get; set; }

    [JsonPropertyName("timing")]
    public int? Timing { get; set; }

    [JsonPropertyName("total_sleep")]
    public int? TotalSleep { get; set; }
}

/// <summary>
/// Oura sleep document from sleep endpoint (detailed sleep periods).
/// </summary>
public class OuraSleepDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("day")]
    public string Day { get; set; } = string.Empty;

    [JsonPropertyName("bedtime_start")]
    public DateTime? BedtimeStart { get; set; }

    [JsonPropertyName("bedtime_end")]
    public DateTime? BedtimeEnd { get; set; }

    [JsonPropertyName("total_sleep_duration")]
    public int? TotalSleepDuration { get; set; }

    [JsonPropertyName("deep_sleep_duration")]
    public int? DeepSleepDuration { get; set; }

    [JsonPropertyName("rem_sleep_duration")]
    public int? RemSleepDuration { get; set; }

    [JsonPropertyName("light_sleep_duration")]
    public int? LightSleepDuration { get; set; }

    [JsonPropertyName("awake_time")]
    public int? AwakeTime { get; set; }

    [JsonPropertyName("efficiency")]
    public int? Efficiency { get; set; }

    [JsonPropertyName("latency")]
    public int? Latency { get; set; }

    [JsonPropertyName("average_heart_rate")]
    public double? AverageHeartRate { get; set; }

    [JsonPropertyName("lowest_heart_rate")]
    public int? LowestHeartRate { get; set; }

    [JsonPropertyName("average_hrv")]
    public int? AverageHrv { get; set; }

    [JsonPropertyName("average_breath")]
    public double? AverageBreath { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // "long_sleep", "short_sleep", etc.
}

/// <summary>
/// Oura activity data from daily_activity endpoint.
/// </summary>
public class OuraActivityData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("day")]
    public string Day { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public int? Score { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }

    [JsonPropertyName("active_calories")]
    public int? ActiveCalories { get; set; }

    [JsonPropertyName("total_calories")]
    public int? TotalCalories { get; set; }

    [JsonPropertyName("steps")]
    public int? Steps { get; set; }

    [JsonPropertyName("equivalent_walking_distance")]
    public int? EquivalentWalkingDistance { get; set; }

    [JsonPropertyName("high_activity_time")]
    public int? HighActivityTime { get; set; }

    [JsonPropertyName("medium_activity_time")]
    public int? MediumActivityTime { get; set; }

    [JsonPropertyName("low_activity_time")]
    public int? LowActivityTime { get; set; }

    [JsonPropertyName("sedentary_time")]
    public int? SedentaryTime { get; set; }

    [JsonPropertyName("inactivity_alerts")]
    public int? InactivityAlerts { get; set; }
}

/// <summary>
/// Oura readiness data from daily_readiness endpoint.
/// </summary>
public class OuraReadinessData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("day")]
    public string Day { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public int? Score { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }

    [JsonPropertyName("temperature_deviation")]
    public double? TemperatureDeviation { get; set; }

    [JsonPropertyName("contributors")]
    public OuraReadinessContributors? Contributors { get; set; }
}

public class OuraReadinessContributors
{
    [JsonPropertyName("activity_balance")]
    public int? ActivityBalance { get; set; }

    [JsonPropertyName("body_temperature")]
    public int? BodyTemperature { get; set; }

    [JsonPropertyName("hrv_balance")]
    public int? HrvBalance { get; set; }

    [JsonPropertyName("previous_day_activity")]
    public int? PreviousDayActivity { get; set; }

    [JsonPropertyName("previous_night")]
    public int? PreviousNight { get; set; }

    [JsonPropertyName("recovery_index")]
    public int? RecoveryIndex { get; set; }

    [JsonPropertyName("resting_heart_rate")]
    public int? RestingHeartRate { get; set; }

    [JsonPropertyName("sleep_balance")]
    public int? SleepBalance { get; set; }
}

/// <summary>
/// Daily stress data from daily_stress endpoint.
/// </summary>
public class OuraStressData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("day")]
    public string Day { get; set; } = string.Empty;

    [JsonPropertyName("stress_high")]
    public int? StressHigh { get; set; }

    [JsonPropertyName("recovery_high")]
    public int? RecoveryHigh { get; set; }

    [JsonPropertyName("day_summary")]
    public string? DaySummary { get; set; }
}

/// <summary>
/// Daily resilience data from daily_resilience endpoint.
/// </summary>
public class OuraResilienceData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("day")]
    public string Day { get; set; } = string.Empty;

    [JsonPropertyName("level")]
    public string? Level { get; set; }

    [JsonPropertyName("contributors")]
    public OuraResilienceContributors? Contributors { get; set; }
}

public class OuraResilienceContributors
{
    [JsonPropertyName("sleep_recovery")]
    public int? SleepRecovery { get; set; }

    [JsonPropertyName("daytime_recovery")]
    public int? DaytimeRecovery { get; set; }

    [JsonPropertyName("stress")]
    public int? Stress { get; set; }
}

/// <summary>
/// VO2 Max data from vO2_max endpoint.
/// </summary>
public class OuraVo2MaxData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("day")]
    public string Day { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("vo2_max")]
    public double? Vo2Max { get; set; }
}

/// <summary>
/// Cardiovascular age data from daily_cardiovascular_age endpoint.
/// </summary>
public class OuraCardiovascularAgeData
{
    [JsonPropertyName("day")]
    public string Day { get; set; } = string.Empty;

    [JsonPropertyName("vascular_age")]
    public int? VascularAge { get; set; }
}

/// <summary>
/// SpO2 data from daily_spo2 endpoint (Gen 3 only).
/// </summary>
public class OuraSpO2Data
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("day")]
    public string Day { get; set; } = string.Empty;

    [JsonPropertyName("spo2_percentage")]
    public OuraSpO2Percentage? Spo2Percentage { get; set; }

    [JsonPropertyName("breathing_disturbance_index")]
    public double? BreathingDisturbanceIndex { get; set; }
}

public class OuraSpO2Percentage
{
    [JsonPropertyName("average")]
    public double? Average { get; set; }
}

/// <summary>
/// Sleep time recommendation from sleep_time endpoint.
/// </summary>
public class OuraSleepTimeData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("day")]
    public string Day { get; set; } = string.Empty;

    [JsonPropertyName("optimal_bedtime")]
    public OuraOptimalBedtime? OptimalBedtime { get; set; }

    [JsonPropertyName("recommendation")]
    public string? Recommendation { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public class OuraOptimalBedtime
{
    [JsonPropertyName("day_tz")]
    public int? DayTz { get; set; }

    [JsonPropertyName("end_offset")]
    public int? EndOffset { get; set; }

    [JsonPropertyName("start_offset")]
    public int? StartOffset { get; set; }
}

/// <summary>
/// Workout data from workout endpoint.
/// </summary>
public class OuraWorkoutData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("day")]
    public string? Day { get; set; }

    [JsonPropertyName("activity")]
    public string? Activity { get; set; }

    [JsonPropertyName("calories")]
    public int? Calories { get; set; }

    [JsonPropertyName("distance")]
    public int? Distance { get; set; }

    [JsonPropertyName("start_datetime")]
    public string? StartDatetime { get; set; }

    [JsonPropertyName("end_datetime")]
    public string? EndDatetime { get; set; }

    [JsonPropertyName("intensity")]
    public string? Intensity { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }
}
