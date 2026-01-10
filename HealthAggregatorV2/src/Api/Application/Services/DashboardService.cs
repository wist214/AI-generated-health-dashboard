using HealthAggregatorV2.Api.Application.DTOs.Responses;
using HealthAggregatorV2.Api.Application.Services.Interfaces;
using HealthAggregatorV2.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace HealthAggregatorV2.Api.Application.Services;

/// <summary>
/// Service for aggregating dashboard data.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly IDailySummaryRepository _dailySummaryRepository;
    private readonly ISourcesRepository _sourcesRepository;
    private readonly IMeasurementsRepository _measurementsRepository;
    private readonly IMetricTypesRepository _metricTypesRepository;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        IDailySummaryRepository dailySummaryRepository,
        ISourcesRepository sourcesRepository,
        IMeasurementsRepository measurementsRepository,
        IMetricTypesRepository metricTypesRepository,
        ILogger<DashboardService> logger)
    {
        _dailySummaryRepository = dailySummaryRepository;
        _sourcesRepository = sourcesRepository;
        _measurementsRepository = measurementsRepository;
        _metricTypesRepository = metricTypesRepository;
        _logger = logger;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting dashboard summary");

        // Get today's or most recent daily summary
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var summary = await _dailySummaryRepository.GetByDateAsync(today, cancellationToken);

        // If no summary for today, get the most recent one
        if (summary == null)
        {
            var recentSummaries = await _dailySummaryRepository.GetByDateRangeAsync(
                today.AddDays(-7),
                today,
                cancellationToken);
            summary = recentSummaries.OrderByDescending(s => s.Date).FirstOrDefault();
        }

        // Get source sync info
        var sources = await _sourcesRepository.GetAllAsync(cancellationToken);
        var sourceSyncInfo = sources.Select(s => new SourceSyncInfoDto(
            SourceName: s.ProviderName,
            LastSyncedAt: s.LastSyncedAt
        ));

        if (summary == null)
        {
            // Return empty summary with source info
            return new DashboardSummaryDto
            {
                LastUpdated = DateTime.UtcNow,
                SourceSyncInfo = sourceSyncInfo
            };
        }

        return new DashboardSummaryDto
        {
            // Sleep
            SleepScore = summary.SleepScore,
            ReadinessScore = summary.ReadinessScore,
            TotalSleepHours = summary.TotalSleepDuration.HasValue
                ? Math.Round(summary.TotalSleepDuration.Value / 3600.0, 1)
                : null,
            DeepSleepHours = summary.DeepSleepDuration.HasValue
                ? Math.Round(summary.DeepSleepDuration.Value / 3600.0, 1)
                : null,
            RemSleepHours = summary.RemSleepDuration.HasValue
                ? Math.Round(summary.RemSleepDuration.Value / 3600.0, 1)
                : null,
            SleepEfficiency = summary.SleepEfficiency,

            // Activity
            ActivityScore = summary.ActivityScore,
            Steps = summary.Steps,
            ActiveCalories = summary.CaloriesBurned,

            // Body
            Weight = summary.Weight,
            BodyFat = summary.BodyFatPercentage,
            Bmi = null, // Not stored in DailySummary, can be calculated from weight/height if needed

            // Heart
            HeartRateAvg = summary.HeartRateAvg,
            HeartRateMin = summary.HeartRateMin,
            HrvAverage = summary.HrvAverage,

            // Nutrition
            CaloriesConsumed = summary.CaloriesConsumed,
            Protein = summary.ProteinGrams,
            Carbs = summary.CarbsGrams,
            Fat = summary.FatGrams,

            // Metadata
            LastUpdated = summary.UpdatedAt,
            SourceSyncInfo = sourceSyncInfo
        };
    }

    public async Task<IEnumerable<DailySummaryDto>> GetDailySummariesAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting daily summaries from {From} to {To}", from, to);

        var fromDate = DateOnly.FromDateTime(from);
        var toDate = DateOnly.FromDateTime(to);
        var summaries = await _dailySummaryRepository.GetByDateRangeAsync(fromDate, toDate, cancellationToken);

        return summaries.Select(s => new DailySummaryDto
        {
            Date = s.Date.ToDateTime(TimeOnly.MinValue),

            // Sleep
            SleepScore = s.SleepScore,
            ReadinessScore = s.ReadinessScore,
            TotalSleepHours = s.TotalSleepDuration.HasValue
                ? Math.Round(s.TotalSleepDuration.Value / 3600.0, 1)
                : null,
            DeepSleepHours = s.DeepSleepDuration.HasValue
                ? Math.Round(s.DeepSleepDuration.Value / 3600.0, 1)
                : null,
            RemSleepHours = s.RemSleepDuration.HasValue
                ? Math.Round(s.RemSleepDuration.Value / 3600.0, 1)
                : null,
            SleepEfficiency = s.SleepEfficiency,

            // Heart
            HrvAverage = s.HrvAverage,
            RestingHeartRate = s.HeartRateMin,

            // Activity
            ActivityScore = s.ActivityScore,
            Steps = s.Steps,
            ActiveCalories = s.CaloriesBurned,

            // Body
            Weight = s.Weight,
            BodyFat = s.BodyFatPercentage,

            // Nutrition
            CaloriesConsumed = s.CaloriesConsumed,
            Protein = s.ProteinGrams,
            Carbs = s.CarbsGrams,
            Fat = s.FatGrams
        }).OrderByDescending(s => s.Date);
    }
}
