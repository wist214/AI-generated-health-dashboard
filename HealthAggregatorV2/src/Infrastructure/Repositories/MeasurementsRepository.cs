using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HealthAggregatorV2.Application.Interfaces.Repositories;
using HealthAggregatorV2.Domain.Entities;
using HealthAggregatorV2.Infrastructure.Data;

namespace HealthAggregatorV2.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Measurement entity.
/// Optimized for high-volume data access patterns.
/// </summary>
public class MeasurementsRepository : IMeasurementsRepository
{
    private readonly HealthDbContext _context;
    private readonly ILogger<MeasurementsRepository> _logger;

    public MeasurementsRepository(HealthDbContext context, ILogger<MeasurementsRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Measurement?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Measurements
            .AsNoTracking()
            .Include(m => m.MetricType)
            .Include(m => m.Source)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Measurement>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Measurements
            .AsNoTracking()
            .Include(m => m.MetricType)
            .Include(m => m.Source)
            .OrderByDescending(m => m.Timestamp)
            .Take(1000) // Safety limit
            .ToListAsync(cancellationToken);
    }

    public async Task<Measurement> AddAsync(Measurement entity, CancellationToken cancellationToken = default)
    {
        await _context.Measurements.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<Measurement> entities, CancellationToken cancellationToken = default)
    {
        await _context.Measurements.AddRangeAsync(entities, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Measurement entity, CancellationToken cancellationToken = default)
    {
        _context.Measurements.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Measurement entity, CancellationToken cancellationToken = default)
    {
        _context.Measurements.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Measurement?> GetLatestByMetricTypeAsync(
        string metricTypeName,
        CancellationToken cancellationToken = default)
    {
        return await _context.Measurements
            .AsNoTracking()
            .Include(m => m.MetricType)
            .Include(m => m.Source)
            .Where(m => m.MetricType.Name == metricTypeName)
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Measurement>> GetLatestByMetricTypeAsync(
        string metricTypeName,
        int count,
        CancellationToken cancellationToken = default)
    {
        return await _context.Measurements
            .AsNoTracking()
            .Include(m => m.MetricType)
            .Include(m => m.Source)
            .Where(m => m.MetricType.Name == metricTypeName)
            .OrderByDescending(m => m.Timestamp)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Measurement>> GetByDateRangeAsync(
        DateTime from,
        DateTime to,
        string? metricTypeName = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Measurements
            .AsNoTracking()
            .Include(m => m.MetricType)
            .Include(m => m.Source)
            .Where(m => m.Timestamp >= from && m.Timestamp <= to);

        if (!string.IsNullOrEmpty(metricTypeName))
        {
            query = query.Where(m => m.MetricType.Name == metricTypeName);
        }

        return await query
            .OrderBy(m => m.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<Measurement?> GetLatestByMetricTypeAndSourceAsync(
        string metricTypeName,
        long sourceId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Measurements
            .AsNoTracking()
            .Include(m => m.MetricType)
            .Where(m => m.MetricType.Name == metricTypeName && m.SourceId == sourceId)
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        int metricTypeId,
        long sourceId,
        DateTime timestamp,
        CancellationToken cancellationToken = default)
    {
        return await _context.Measurements
            .AsNoTracking()
            .AnyAsync(m =>
                m.MetricTypeId == metricTypeId &&
                m.SourceId == sourceId &&
                m.Timestamp == timestamp,
                cancellationToken);
    }

    public async Task<IEnumerable<Measurement>> GetBySourceAsync(
        long sourceId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Measurements
            .AsNoTracking()
            .Include(m => m.MetricType)
            .Where(m => m.SourceId == sourceId);

        if (from.HasValue)
        {
            query = query.Where(m => m.Timestamp >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(m => m.Timestamp <= to.Value);
        }

        return await query
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync(cancellationToken);
    }
}
