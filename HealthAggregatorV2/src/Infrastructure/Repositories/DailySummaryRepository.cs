using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HealthAggregatorV2.Application.Interfaces.Repositories;
using HealthAggregatorV2.Domain.Entities;
using HealthAggregatorV2.Infrastructure.Data;

namespace HealthAggregatorV2.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for DailySummary entity.
/// </summary>
public class DailySummaryRepository : IDailySummaryRepository
{
    private readonly HealthDbContext _context;
    private readonly ILogger<DailySummaryRepository> _logger;

    public DailySummaryRepository(HealthDbContext context, ILogger<DailySummaryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DailySummary?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.DailySummaries
            .AsNoTracking()
            .FirstOrDefaultAsync(ds => ds.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<DailySummary>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DailySummaries
            .AsNoTracking()
            .OrderByDescending(ds => ds.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<DailySummary> AddAsync(DailySummary entity, CancellationToken cancellationToken = default)
    {
        await _context.DailySummaries.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<DailySummary> entities, CancellationToken cancellationToken = default)
    {
        await _context.DailySummaries.AddRangeAsync(entities, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(DailySummary entity, CancellationToken cancellationToken = default)
    {
        _context.DailySummaries.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(DailySummary entity, CancellationToken cancellationToken = default)
    {
        _context.DailySummaries.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<DailySummary?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _context.DailySummaries
            .AsNoTracking()
            .FirstOrDefaultAsync(ds => ds.Date == date, cancellationToken);
    }

    public async Task<IEnumerable<DailySummary>> GetByDateRangeAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        return await _context.DailySummaries
            .AsNoTracking()
            .Where(ds => ds.Date >= from && ds.Date <= to)
            .OrderByDescending(ds => ds.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<DailySummary>> GetLatestAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        return await _context.DailySummaries
            .AsNoTracking()
            .OrderByDescending(ds => ds.Date)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<DailySummary> UpsertAsync(DailySummary summary, CancellationToken cancellationToken = default)
    {
        var existing = await _context.DailySummaries
            .FirstOrDefaultAsync(ds => ds.Date == summary.Date, cancellationToken);

        if (existing == null)
        {
            await _context.DailySummaries.AddAsync(summary, cancellationToken);
        }
        else
        {
            // Update existing summary
            existing.SleepScore = summary.SleepScore ?? existing.SleepScore;
            existing.ReadinessScore = summary.ReadinessScore ?? existing.ReadinessScore;
            existing.ActivityScore = summary.ActivityScore ?? existing.ActivityScore;
            existing.Steps = summary.Steps ?? existing.Steps;
            existing.CaloriesBurned = summary.CaloriesBurned ?? existing.CaloriesBurned;
            existing.Weight = summary.Weight ?? existing.Weight;
            existing.BodyFatPercentage = summary.BodyFatPercentage ?? existing.BodyFatPercentage;
            existing.HeartRateAvg = summary.HeartRateAvg ?? existing.HeartRateAvg;
            existing.HeartRateMin = summary.HeartRateMin ?? existing.HeartRateMin;
            existing.HeartRateMax = summary.HeartRateMax ?? existing.HeartRateMax;
            existing.TotalSleepDuration = summary.TotalSleepDuration ?? existing.TotalSleepDuration;
            existing.DeepSleepDuration = summary.DeepSleepDuration ?? existing.DeepSleepDuration;
            existing.RemSleepDuration = summary.RemSleepDuration ?? existing.RemSleepDuration;
            existing.SleepEfficiency = summary.SleepEfficiency ?? existing.SleepEfficiency;
            existing.HrvAverage = summary.HrvAverage ?? existing.HrvAverage;
            existing.CaloriesConsumed = summary.CaloriesConsumed ?? existing.CaloriesConsumed;
            existing.ProteinGrams = summary.ProteinGrams ?? existing.ProteinGrams;
            existing.CarbsGrams = summary.CarbsGrams ?? existing.CarbsGrams;
            existing.FatGrams = summary.FatGrams ?? existing.FatGrams;

            summary = existing;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return summary;
    }
}
