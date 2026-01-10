using HealthAggregatorV2.Domain.Entities;

namespace HealthAggregatorV2.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for MetricType entity operations.
/// </summary>
public interface IMetricTypesRepository
{
    /// <summary>
    /// Gets a metric type by its identifier.
    /// </summary>
    Task<MetricType?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a metric type by its name.
    /// </summary>
    Task<MetricType?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all metric types.
    /// </summary>
    Task<IEnumerable<MetricType>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metric types by category.
    /// </summary>
    Task<IEnumerable<MetricType>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
}
