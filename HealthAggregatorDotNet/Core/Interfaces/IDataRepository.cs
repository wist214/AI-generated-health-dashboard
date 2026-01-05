namespace HealthAggregator.Core.Interfaces;

/// <summary>
/// Generic repository interface for data persistence operations.
/// Single Responsibility: Only handles loading and saving data.
/// </summary>
/// <typeparam name="T">The type of data to persist</typeparam>
public interface IDataRepository<T> where T : class, new()
{
    /// <summary>
    /// Loads data from the persistent storage asynchronously.
    /// </summary>
    /// <returns>The loaded data or null if none exists</returns>
    Task<T?> GetAsync();

    /// <summary>
    /// Saves data to the persistent storage asynchronously.
    /// </summary>
    /// <param name="data">The data to save</param>
    Task SaveAsync(T data);

    /// <summary>
    /// Checks if data exists in persistent storage.
    /// </summary>
    Task<bool> ExistsAsync();

    /// <summary>
    /// Deletes data from persistent storage.
    /// </summary>
    Task DeleteAsync();
}
