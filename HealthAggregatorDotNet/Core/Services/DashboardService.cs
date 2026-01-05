using HealthAggregator.Core.DTOs;
using HealthAggregator.Core.Interfaces;

namespace HealthAggregator.Core.Services;

/// <summary>
/// Dashboard service that aggregates data from all sources
/// and performs calculations for the UI.
/// This moves calculation logic from JavaScript to .NET.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly IPicoocDataService _picoocService;
    private readonly IOuraDataService _ouraService;

    public DashboardService(IPicoocDataService picoocService, IOuraDataService ouraService)
    {
        _picoocService = picoocService;
        _ouraService = ouraService;
    }

    public async Task<DashboardDto> GetDashboardDataAsync()
    {
        var metrics = await GetMetricCardsAsync();
        var progress = await GetProgressComparisonAsync();
        var weeklyTrends = await GetWeeklyTrendsAsync();
        var insights = await GenerateInsightsAsync();
        var quickStats = await GetQuickStatsAsync();

        return new DashboardDto
        {
            Metrics = metrics,
            Progress = progress,
            WeeklyTrends = weeklyTrends,
            Insights = insights,
            QuickStats = quickStats,
            LastUpdated = DateTime.UtcNow
        };
    }

    public async Task<List<MetricCardDto>> GetMetricCardsAsync()
    {
        var cards = new List<MetricCardDto>();

        // Weight card
        var latestWeight = await _picoocService.GetLatestMeasurementAsync();
        if (latestWeight != null)
        {
            cards.Add(new MetricCardDto
            {
                Id = "weight",
                Title = "Weight",
                Value = latestWeight.Weight,
                Unit = "kg",
                Icon = "⚖️",
                Date = latestWeight.Date
            });
        }

        // Sleep Score card
        var latestSleep = await _ouraService.GetLatestSleepAsync();
        if (latestSleep != null)
        {
            cards.Add(new MetricCardDto
            {
                Id = "sleep",
                Title = "Sleep Score",
                Value = latestSleep.Score ?? 0,
                Unit = "",
                Icon = "😴",
                Date = DateTime.Parse(latestSleep.Day),
                Category = GetScoreCategory(latestSleep.Score ?? 0)
            });
        }

        // Readiness Score card
        var latestReadiness = await _ouraService.GetLatestReadinessAsync();
        if (latestReadiness != null)
        {
            cards.Add(new MetricCardDto
            {
                Id = "readiness",
                Title = "Readiness Score",
                Value = latestReadiness.Score ?? 0,
                Unit = "",
                Icon = "💪",
                Date = DateTime.Parse(latestReadiness.Day),
                Category = GetScoreCategory(latestReadiness.Score ?? 0)
            });
        }

        // Activity Score card
        var latestActivity = await _ouraService.GetLatestActivityAsync();
        if (latestActivity != null)
        {
            cards.Add(new MetricCardDto
            {
                Id = "activity",
                Title = "Activity Score",
                Value = latestActivity.Score ?? 0,
                Unit = "",
                Icon = "🏃",
                Date = DateTime.Parse(latestActivity.Day),
                Category = GetScoreCategory(latestActivity.Score ?? 0)
            });
        }

        return cards;
    }

    public async Task<ProgressComparisonDto> GetProgressComparisonAsync()
    {
        var progress = new ProgressComparisonDto();

        // Weight progress (compared to 30 days ago from latest measurement)
        var allMeasurements = await _picoocService.GetAllMeasurementsAsync();
        var orderedMeasurements = allMeasurements
            .Where(m => m.HasValidWeight)
            .OrderByDescending(m => m.Date)
            .ToList();

        if (orderedMeasurements.Count >= 2)
        {
            var latest = orderedMeasurements.First();
            var latestDate = latest.Date;
            var compareDate = latestDate.AddDays(-30);
            
            var comparison = orderedMeasurements
                .Where(m => m.Date <= compareDate)
                .OrderByDescending(m => m.Date)
                .FirstOrDefault() ?? orderedMeasurements.Last();

            var change = latest.Weight - comparison.Weight;
            var percentChange = comparison.Weight != 0 
                ? (change / comparison.Weight) * 100 
                : 0;

            progress.WeightChange = Math.Round(change, 2);
            progress.WeightChangePercent = Math.Round(percentChange, 1);
            progress.WeightTrend = change < 0 ? "down" : change > 0 ? "up" : "stable";
        }

        // Sleep score progress
        var ouraData = await _ouraService.GetAllDataAsync();
        var sleepRecords = ouraData.DailySleep
            .Where(s => s.Score.HasValue)
            .OrderByDescending(s => s.Day)
            .ToList();

        if (sleepRecords.Count >= 2)
        {
            var latest = sleepRecords.First();
            var latestDate = DateTime.Parse(latest.Day);
            var compareDate = latestDate.AddDays(-7).ToString("yyyy-MM-dd");
            
            var comparison = sleepRecords
                .Where(s => s.Day.CompareTo(compareDate) <= 0)
                .OrderByDescending(s => s.Day)
                .FirstOrDefault() ?? sleepRecords.Last();

            var change = (latest.Score ?? 0) - (comparison.Score ?? 0);
            progress.SleepScoreChange = change;
            progress.SleepScoreTrend = change > 0 ? "up" : change < 0 ? "down" : "stable";
        }

        // Readiness score progress
        var readinessRecords = ouraData.Readiness
            .Where(r => r.Score.HasValue)
            .OrderByDescending(r => r.Day)
            .ToList();

        if (readinessRecords.Count >= 2)
        {
            var latest = readinessRecords.First();
            var latestDate = DateTime.Parse(latest.Day);
            var compareDate = latestDate.AddDays(-7).ToString("yyyy-MM-dd");
            
            var comparison = readinessRecords
                .Where(r => r.Day.CompareTo(compareDate) <= 0)
                .OrderByDescending(r => r.Day)
                .FirstOrDefault() ?? readinessRecords.Last();

            var change = (latest.Score ?? 0) - (comparison.Score ?? 0);
            progress.ReadinessScoreChange = change;
            progress.ReadinessScoreTrend = change > 0 ? "up" : change < 0 ? "down" : "stable";
        }

        return progress;
    }

    public async Task<WeeklyTrendsDto> GetWeeklyTrendsAsync()
    {
        var trends = new WeeklyTrendsDto();
        var last7Days = Enumerable.Range(0, 7)
            .Select(i => DateTime.UtcNow.AddDays(-6 + i))
            .ToList();

        trends.Labels = last7Days.Select(d => d.ToString("ddd")).ToList();
        trends.Dates = last7Days.Select(d => d.ToString("yyyy-MM-dd")).ToList();

        // Get Oura data
        var ouraData = await _ouraService.GetAllDataAsync();
        var sleepByDay = ouraData.DailySleep.ToDictionary(s => s.Day, s => (double?)s.Score);
        var readinessByDay = ouraData.Readiness.ToDictionary(r => r.Day, r => (double?)r.Score);

        trends.SleepScores = trends.Dates.Select(d => sleepByDay.GetValueOrDefault(d)).ToList();
        trends.ReadinessScores = trends.Dates.Select(d => readinessByDay.GetValueOrDefault(d)).ToList();

        // Get weight data
        var measurements = await _picoocService.GetAllMeasurementsAsync();
        var weightByDay = measurements.ToDictionary(
            m => m.Date.ToString("yyyy-MM-dd"), 
            m => (double?)m.Weight
        );

        trends.Weights = trends.Dates.Select(d => weightByDay.GetValueOrDefault(d)).ToList();

        return trends;
    }

    public async Task<List<InsightDto>> GenerateInsightsAsync()
    {
        var insights = new List<InsightDto>();

        // Weight insights
        var measurements = await _picoocService.GetAllMeasurementsAsync();
        var orderedMeasurements = measurements
            .Where(m => m.HasValidWeight)
            .OrderByDescending(m => m.Date)
            .ToList();

        if (orderedMeasurements.Count >= 7)
        {
            var latest = orderedMeasurements.First();
            var latestDate = latest.Date;
            var weekAgoDate = latestDate.AddDays(-7);
            
            var weekAgo = orderedMeasurements
                .Where(m => m.Date <= weekAgoDate)
                .OrderByDescending(m => m.Date)
                .FirstOrDefault();

            if (weekAgo != null)
            {
                var weeklyChange = latest.Weight - weekAgo.Weight;
                if (Math.Abs(weeklyChange) >= 0.5)
                {
                    insights.Add(new InsightDto
                    {
                        Type = weeklyChange < 0 ? "positive" : "info",
                        Icon = weeklyChange < 0 ? "📉" : "📈",
                        Title = $"Weight {(weeklyChange < 0 ? "decreased" : "increased")} by {Math.Abs(weeklyChange):F1} kg",
                        Description = $"Compared to {weekAgo.Date:MMM dd}"
                    });
                }
            }
        }

        // Sleep insights
        var ouraData = await _ouraService.GetAllDataAsync();
        var sleepRecords = ouraData.DailySleep
            .Where(s => s.Score.HasValue)
            .OrderByDescending(s => s.Day)
            .ToList();

        if (sleepRecords.Any())
        {
            var latestSleep = sleepRecords.First();
            if (latestSleep.Score >= 85)
            {
                insights.Add(new InsightDto
                {
                    Type = "positive",
                    Icon = "🌟",
                    Title = "Excellent sleep quality!",
                    Description = $"Sleep score of {latestSleep.Score} on {DateTime.Parse(latestSleep.Day):MMM dd}"
                });
            }
            else if (latestSleep.Score < 70)
            {
                insights.Add(new InsightDto
                {
                    Type = "warning",
                    Icon = "😴",
                    Title = "Sleep needs attention",
                    Description = $"Sleep score of {latestSleep.Score} - try to get more rest"
                });
            }

            // Sleep trend
            if (sleepRecords.Count >= 7)
            {
                var latestDate = DateTime.Parse(sleepRecords.First().Day);
                var weekAgoDate = latestDate.AddDays(-7).ToString("yyyy-MM-dd");
                
                var recentWeek = sleepRecords.Where(s => s.Day.CompareTo(weekAgoDate) >= 0).ToList();
                var previousWeek = sleepRecords
                    .Where(s => s.Day.CompareTo(weekAgoDate) < 0)
                    .Take(7)
                    .ToList();

                if (recentWeek.Any() && previousWeek.Any())
                {
                    var recentAvg = recentWeek.Average(s => s.Score ?? 0);
                    var previousAvg = previousWeek.Average(s => s.Score ?? 0);
                    var improvement = recentAvg - previousAvg;

                    if (Math.Abs(improvement) >= 5)
                    {
                        insights.Add(new InsightDto
                        {
                            Type = improvement > 0 ? "positive" : "info",
                            Icon = improvement > 0 ? "📈" : "📊",
                            Title = $"Sleep trend {(improvement > 0 ? "improving" : "declining")}",
                            Description = $"Average changed by {improvement:+0;-0} points this week"
                        });
                    }
                }
            }
        }

        // Readiness insights
        var readinessRecords = ouraData.Readiness
            .Where(r => r.Score.HasValue)
            .OrderByDescending(r => r.Day)
            .ToList();

        if (readinessRecords.Any())
        {
            var latestReadiness = readinessRecords.First();
            if (latestReadiness.Score >= 85)
            {
                insights.Add(new InsightDto
                {
                    Type = "positive",
                    Icon = "💪",
                    Title = "High readiness!",
                    Description = "Great day for challenging activities"
                });
            }
            else if (latestReadiness.Score < 65)
            {
                insights.Add(new InsightDto
                {
                    Type = "warning",
                    Icon = "🔋",
                    Title = "Low readiness",
                    Description = "Consider taking it easy today"
                });
            }
        }

        return insights.Take(5).ToList();
    }

    public async Task<QuickStatsDto> GetQuickStatsAsync()
    {
        var stats = new QuickStatsDto();

        // Get measurement stats
        var measurementStats = await _picoocService.GetStatsAsync();
        if (measurementStats.Weight != null)
        {
            stats.TotalMeasurements = measurementStats.Count;
            stats.WeightMin = measurementStats.Weight.Min;
            stats.WeightMax = measurementStats.Weight.Max;
            stats.WeightAverage = measurementStats.Weight.Average;
        }

        // Get Oura averages
        var ouraAverages = await _ouraService.GetAverageScoresAsync(30);
        stats.AverageSleepScore = ouraAverages.GetValueOrDefault("sleep");
        stats.AverageReadinessScore = ouraAverages.GetValueOrDefault("readiness");
        stats.AverageActivityScore = ouraAverages.GetValueOrDefault("activity");

        // Data range
        if (measurementStats.FirstDate.HasValue && measurementStats.LastDate.HasValue)
        {
            stats.DataStartDate = measurementStats.FirstDate.Value;
            stats.DataEndDate = measurementStats.LastDate.Value;
            stats.DaysTracked = (int)(measurementStats.LastDate.Value - measurementStats.FirstDate.Value).TotalDays;
        }

        return stats;
    }

    private static string GetScoreCategory(int score)
    {
        return score switch
        {
            >= 85 => "optimal",
            >= 70 => "good",
            >= 60 => "fair",
            _ => "needs_attention"
        };
    }
}
