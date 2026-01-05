namespace HealthAggregatorApi.Core.Interfaces;

/// <summary>
/// Generic repository interface for data persistence operations.
/// </summary>
public interface IDataRepository<T> where T : class, new()
{
    Task<T?> GetAsync();
    Task SaveAsync(T data);
    Task<bool> ExistsAsync();
    Task DeleteAsync();
}
