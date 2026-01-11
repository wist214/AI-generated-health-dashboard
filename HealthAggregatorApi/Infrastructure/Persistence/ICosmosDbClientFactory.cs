using Microsoft.Azure.Cosmos;

namespace HealthAggregatorApi.Infrastructure.Persistence;

/// <summary>
/// Factory interface for creating and managing Cosmos DB client instances.
/// Follows Dependency Inversion Principle (SOLID).
/// </summary>
public interface ICosmosDbClientFactory
{
    /// <summary>
    /// Gets a singleton instance of the CosmosClient.
    /// </summary>
    /// <returns>Configured CosmosClient instance</returns>
    CosmosClient GetClient();

    /// <summary>
    /// Gets a Cosmos DB container reference.
    /// </summary>
    /// <param name="containerName">Name of the container</param>
    /// <returns>Container instance</returns>
    Container GetContainer(string containerName);

    /// <summary>
    /// Ensures the database and specified containers exist.
    /// Creates them if they don't exist.
    /// </summary>
    /// <param name="containerNames">Names of containers to create</param>
    /// <param name="partitionKeyPath">Partition key path (default: /userId)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EnsureDatabaseAndContainersExistAsync(
        IEnumerable<string> containerNames,
        string partitionKeyPath = "/userId",
        CancellationToken cancellationToken = default);
}
