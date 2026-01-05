using HealthAggregator.Core.Interfaces;
using HealthAggregator.Core.Models.Picooc;

namespace HealthAggregator.Core.Services;

/// <summary>
/// Picooc data service handling business logic for Picooc scale data.
/// </summary>
public class PicoocDataService : IPicoocDataService
{
    private readonly IPicoocSyncClient _syncClient;
    private readonly IDataRepository<PicoocDataCache> _repository;
    private PicoocDataCache? _cachedData;

    public PicoocDataService(IPicoocSyncClient syncClient, IDataRepository<PicoocDataCache> repository)
    {
        _syncClient = syncClient;
        _repository = repository;
    }

    public async Task<List<HealthMeasurement>> GetAllMeasurementsAsync()
    {
        var cache = await GetCacheAsync();
        return cache.Measurements;
    }

    public async Task<List<HealthMeasurement>> SyncDataAsync()
    {
        var newMeasurements = await _syncClient.SyncFromCloudAsync();
        var cache = await GetCacheAsync();

        // Merge with existing data (convert IEnumerable to List)
        MergeMeasurements(cache.Measurements, newMeasurements.ToList());
        cache.LastSync = DateTime.UtcNow;

        await _repository.SaveAsync(cache);
        _cachedData = cache;

        return cache.Measurements;
    }

    public async Task<HealthMeasurement?> GetLatestMeasurementAsync()
    {
        var measurements = await GetAllMeasurementsAsync();
        return measurements
            .Where(m => m.HasValidWeight)
            .OrderByDescending(m => m.Date)
            .FirstOrDefault();
    }

    public async Task<MeasurementStats> GetStatsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var measurements = await GetAllMeasurementsAsync();
        
        var filtered = measurements
            .Where(m => m.HasValidWeight)
            .Where(m => !startDate.HasValue || m.Date >= startDate.Value)
            .Where(m => !endDate.HasValue || m.Date <= endDate.Value)
            .OrderBy(m => m.Date)
            .ToList();

        if (!filtered.Any())
        {
            return new MeasurementStats();
        }

        var weights = filtered.Select(m => m.Weight).ToList();
        var bodyFats = filtered.Where(m => m.BodyFat > 0).Select(m => m.BodyFat).ToList();
        var bmis = filtered.Where(m => m.BMI > 0).Select(m => m.BMI).ToList();
        var muscles = filtered.Where(m => m.SkeletalMuscleMass > 0).Select(m => m.SkeletalMuscleMass).ToList();

        return new MeasurementStats
        {
            Count = filtered.Count,
            Weight = new MetricStats
            {
                Min = weights.Min(),
                Max = weights.Max(),
                Average = Math.Round(weights.Average(), 2),
                Latest = weights.Last(),
                First = weights.First()
            },
            BodyFat = bodyFats.Any() ? new MetricStats
            {
                Min = bodyFats.Min(),
                Max = bodyFats.Max(),
                Average = Math.Round(bodyFats.Average(), 2),
                Latest = bodyFats.Last(),
                First = bodyFats.First()
            } : null,
            BMI = bmis.Any() ? new MetricStats
            {
                Min = Math.Round(bmis.Min(), 1),
                Max = Math.Round(bmis.Max(), 1),
                Average = Math.Round(bmis.Average(), 1),
                Latest = Math.Round(bmis.Last(), 1),
                First = Math.Round(bmis.First(), 1)
            } : null,
            Muscle = muscles.Any() ? new MetricStats
            {
                Min = Math.Round(muscles.Min(), 1),
                Max = Math.Round(muscles.Max(), 1),
                Average = Math.Round(muscles.Average(), 1),
                Latest = Math.Round(muscles.Last(), 1),
                First = Math.Round(muscles.First(), 1)
            } : null,
            FirstDate = filtered.First().Date,
            LastDate = filtered.Last().Date
        };
    }

    public async Task<DateTime?> GetLastSyncTimeAsync()
    {
        var cache = await GetCacheAsync();
        return cache.LastSync;
    }

    public async Task<bool> IsServiceAvailableAsync()
    {
        return await _syncClient.CheckDockerAvailableAsync();
    }

    private async Task<PicoocDataCache> GetCacheAsync()
    {
        if (_cachedData != null)
            return _cachedData;

        _cachedData = await _repository.GetAsync() ?? new PicoocDataCache();
        return _cachedData;
    }

    private static void MergeMeasurements(List<HealthMeasurement> existing, List<HealthMeasurement> newData)
    {
        // Use full DateTime for deduplication, not just the date portion.
        // This allows multiple measurements per day (e.g., morning and evening weigh-ins).
        var existingTimestamps = existing.Select(m => m.Date).ToHashSet();
        
        foreach (var measurement in newData)
        {
            if (!existingTimestamps.Contains(measurement.Date))
            {
                existing.Add(measurement);
                existingTimestamps.Add(measurement.Date);
            }
        }

        existing.Sort((a, b) => b.Date.CompareTo(a.Date));
    }
}

/// <summary>
/// Cache container for Picooc data.
/// </summary>
public class PicoocDataCache
{
    public List<HealthMeasurement> Measurements { get; set; } = new();
    public DateTime? LastSync { get; set; }
}

/// <summary>
/// Statistics for Picooc measurements.
/// </summary>
public class MeasurementStats
{
    public int Count { get; set; }
    public MetricStats? Weight { get; set; }
    public MetricStats? BodyFat { get; set; }
    public MetricStats? BMI { get; set; }
    public MetricStats? Muscle { get; set; }
    public DateTime? FirstDate { get; set; }
    public DateTime? LastDate { get; set; }
}

/// <summary>
/// Statistics for a single metric.
/// </summary>
public class MetricStats
{
    public double Min { get; set; }
    public double Max { get; set; }
    public double Average { get; set; }
    public double Latest { get; set; }
    public double First { get; set; }
    
    /// <summary>
    /// Change from first to latest measurement.
    /// </summary>
    public double Change => Math.Round(Latest - First, 2);
    
    /// <summary>
    /// Percentage change from first to latest.
    /// </summary>
    public double ChangePercent => First != 0 ? Math.Round((Latest - First) / First * 100, 1) : 0;
}
