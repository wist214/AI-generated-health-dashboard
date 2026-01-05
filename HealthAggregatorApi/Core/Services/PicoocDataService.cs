using HealthAggregatorApi.Core.Interfaces;
using HealthAggregatorApi.Core.Models.Picooc;

namespace HealthAggregatorApi.Core.Services;

/// <summary>
/// Picooc data service handling business logic for Picooc scale data.
/// </summary>
public class PicoocDataService : IPicoocDataService
{
    private readonly IPicoocSyncClient _syncClient;
    private readonly IDataRepository<PicoocDataCache> _repository;

    public PicoocDataService(IPicoocSyncClient syncClient, IDataRepository<PicoocDataCache> repository)
    {
        _syncClient = syncClient;
        _repository = repository;
    }

    public async Task<List<HealthMeasurement>> GetAllMeasurementsAsync()
    {
        var cache = await _repository.GetAsync() ?? new PicoocDataCache();
        return cache.Measurements;
    }

    public async Task<List<HealthMeasurement>> SyncDataAsync()
    {
        var newMeasurements = await _syncClient.SyncFromCloudAsync();
        var cache = await _repository.GetAsync() ?? new PicoocDataCache();

        MergeMeasurements(cache.Measurements, newMeasurements.ToList());
        cache.LastSync = DateTime.UtcNow;

        await _repository.SaveAsync(cache);

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

    public async Task<DateTime?> GetLastSyncTimeAsync()
    {
        var cache = await _repository.GetAsync();
        return cache?.LastSync;
    }

    public bool IsConfigured()
    {
        return _syncClient.IsConfigured();
    }

    private static void MergeMeasurements(List<HealthMeasurement> existing, List<HealthMeasurement> newData)
    {
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
