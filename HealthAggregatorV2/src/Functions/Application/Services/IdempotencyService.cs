using HealthAggregatorV2.Application.Interfaces.Repositories;
using HealthAggregatorV2.Functions.Application.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HealthAggregatorV2.Functions.Application.Services;

/// <summary>
/// Service for ensuring idempotent sync operations by checking for duplicates.
/// Uses in-memory cache for fast lookups and database as source of truth.
/// </summary>
public class IdempotencyService : IIdempotencyService
{
    private readonly IMeasurementsRepository _measurementsRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<IdempotencyService> _logger;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public IdempotencyService(
        IMeasurementsRepository measurementsRepository,
        IMemoryCache cache,
        ILogger<IdempotencyService> logger)
    {
        _measurementsRepository = measurementsRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsDuplicateAsync(
        int metricTypeId,
        long sourceId,
        DateTime timestamp,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(metricTypeId, sourceId, timestamp);

        // Check cache first
        if (_cache.TryGetValue(cacheKey, out bool _))
        {
            _logger.LogTrace(
                "Cache hit for measurement: MetricTypeId={MetricTypeId}, SourceId={SourceId}, Timestamp={Timestamp}",
                metricTypeId, sourceId, timestamp);
            return true;
        }

        // Check database
        var exists = await _measurementsRepository.ExistsAsync(
            metricTypeId,
            sourceId,
            timestamp,
            cancellationToken);

        if (exists)
        {
            // Cache the result
            _cache.Set(cacheKey, true, CacheDuration);
            _logger.LogTrace(
                "Database hit for measurement: MetricTypeId={MetricTypeId}, SourceId={SourceId}, Timestamp={Timestamp}",
                metricTypeId, sourceId, timestamp);
        }

        return exists;
    }

    public void MarkAsProcessed(int metricTypeId, long sourceId, DateTime timestamp)
    {
        var cacheKey = BuildCacheKey(metricTypeId, sourceId, timestamp);
        _cache.Set(cacheKey, true, CacheDuration);
    }

    private static string BuildCacheKey(int metricTypeId, long sourceId, DateTime timestamp)
    {
        return $"measurement:{metricTypeId}:{sourceId}:{timestamp:yyyy-MM-dd-HH-mm-ss}";
    }
}
