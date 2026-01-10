using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HealthAggregatorV2.Application.Interfaces.Repositories;
using HealthAggregatorV2.Domain.Entities;
using HealthAggregatorV2.Infrastructure.Data;

namespace HealthAggregatorV2.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Source entity.
/// </summary>
public class SourcesRepository : ISourcesRepository
{
    private readonly HealthDbContext _context;
    private readonly ILogger<SourcesRepository> _logger;

    public SourcesRepository(HealthDbContext context, ILogger<SourcesRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Source?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Sources
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Source>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Sources
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Source> AddAsync(Source entity, CancellationToken cancellationToken = default)
    {
        await _context.Sources.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<Source> entities, CancellationToken cancellationToken = default)
    {
        await _context.Sources.AddRangeAsync(entities, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Source entity, CancellationToken cancellationToken = default)
    {
        _context.Sources.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Source entity, CancellationToken cancellationToken = default)
    {
        _context.Sources.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Source?> GetByProviderNameAsync(string providerName, CancellationToken cancellationToken = default)
    {
        return await _context.Sources
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ProviderName == providerName, cancellationToken);
    }

    public async Task<IEnumerable<Source>> GetEnabledSourcesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Sources
            .AsNoTracking()
            .Where(s => s.IsEnabled)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateLastSyncedAsync(long sourceId, DateTime syncedAt, CancellationToken cancellationToken = default)
    {
        var source = await _context.Sources.FindAsync([sourceId], cancellationToken);
        if (source != null)
        {
            source.LastSyncedAt = syncedAt;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
