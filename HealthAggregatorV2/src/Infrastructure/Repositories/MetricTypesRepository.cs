using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HealthAggregatorV2.Application.Interfaces.Repositories;
using HealthAggregatorV2.Domain.Entities;
using HealthAggregatorV2.Domain.Enums;
using HealthAggregatorV2.Infrastructure.Data;

namespace HealthAggregatorV2.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for MetricType entity.
/// </summary>
public class MetricTypesRepository : IMetricTypesRepository
{
    private readonly HealthDbContext _context;
    private readonly ILogger<MetricTypesRepository> _logger;

    public MetricTypesRepository(HealthDbContext context, ILogger<MetricTypesRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<MetricType?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.MetricTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(mt => mt.Id == id, cancellationToken);
    }

    public async Task<MetricType?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.MetricTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(mt => mt.Name == name, cancellationToken);
    }

    public async Task<IEnumerable<MetricType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.MetricTypes
            .AsNoTracking()
            .OrderBy(mt => mt.Category)
            .ThenBy(mt => mt.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MetricType>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        // Try to parse as enum, if fails return empty
        if (Enum.TryParse<MetricCategory>(category, true, out var categoryEnum))
        {
            return await GetByCategoryAsync(categoryEnum, cancellationToken);
        }

        return [];
    }

    public async Task<IEnumerable<MetricType>> GetByCategoryAsync(MetricCategory category, CancellationToken cancellationToken = default)
    {
        return await _context.MetricTypes
            .AsNoTracking()
            .Where(mt => mt.Category == category)
            .OrderBy(mt => mt.Name)
            .ToListAsync(cancellationToken);
    }
}
