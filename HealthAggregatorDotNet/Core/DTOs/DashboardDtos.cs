namespace HealthAggregator.Core.DTOs;

/// <summary>
/// Complete dashboard data returned to frontend.
/// These DTOs belong in Core because they represent domain concepts
/// that are used by domain services (IDashboardService).
/// </summary>
public class DashboardDto
{
    public List<MetricCardDto> Metrics { get; set; } = new();
    public ProgressComparisonDto Progress { get; set; } = new();
    public WeeklyTrendsDto WeeklyTrends { get; set; } = new();
    public List<InsightDto> Insights { get; set; } = new();
    public QuickStatsDto QuickStats { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Individual metric card with value and metadata.
/// </summary>
public class MetricCardDto
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public double Value { get; set; }
    public string Unit { get; set; } = "";
    public string Icon { get; set; } = "";
    public DateTime Date { get; set; }
    public string? Category { get; set; }
}

/// <summary>
/// Progress comparison between current and historical values.
/// </summary>
public class ProgressComparisonDto
{
    // Weight progress
    public double WeightChange { get; set; }
    public double WeightChangePercent { get; set; }
    public string WeightTrend { get; set; } = "stable";
    
    // Sleep score progress
    public int SleepScoreChange { get; set; }
    public string SleepScoreTrend { get; set; } = "stable";
    
    // Readiness score progress
    public int ReadinessScoreChange { get; set; }
    public string ReadinessScoreTrend { get; set; } = "stable";
}

/// <summary>
/// Quick stats displayed in dashboard.
/// </summary>
public class QuickStatsDto
{
    public int TotalMeasurements { get; set; }
    public double WeightMin { get; set; }
    public double WeightMax { get; set; }
    public double WeightAverage { get; set; }
    
    public double AverageSleepScore { get; set; }
    public double AverageReadinessScore { get; set; }
    public double AverageActivityScore { get; set; }
    
    public DateTime? DataStartDate { get; set; }
    public DateTime? DataEndDate { get; set; }
    public int DaysTracked { get; set; }
}

/// <summary>
/// Weekly trends data for chart rendering.
/// </summary>
public class WeeklyTrendsDto
{
    public List<string> Labels { get; set; } = new();
    public List<string> Dates { get; set; } = new();
    public List<double?> SleepScores { get; set; } = new();
    public List<double?> ReadinessScores { get; set; } = new();
    public List<double?> Weights { get; set; } = new();
}

/// <summary>
/// Individual insight/recommendation.
/// </summary>
public class InsightDto
{
    public string Type { get; set; } = "info";
    public string Icon { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}
