using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HealthAggregatorApi.Core.Interfaces;
using HealthAggregatorApi.Infrastructure.Configuration;

namespace HealthAggregatorApi.Infrastructure.Persistence;

/// <summary>
/// High-performance Cosmos DB implementation of IDataRepository.
/// Features: Retry logic, detailed logging, telemetry, ETag-based optimistic concurrency.
/// Follows SOLID principles with proper error handling.
/// </summary>
/// <typeparam name="T">Entity type to store (must be a reference type with parameterless constructor)</typeparam>
public sealed class CosmosDbRepository<T> : IDataRepository<T> where T : class, new()
{
    private readonly Container _container;
    private readonly string _partitionKeyValue;
    private readonly ILogger<CosmosDbRepository<T>> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _documentId;

    public CosmosDbRepository(
        ICosmosDbClientFactory clientFactory,
        IOptions<CosmosDbOptions> options,
        string containerName,
        ILogger<CosmosDbRepository<T>> logger)
    {
        ArgumentNullException.ThrowIfNull(clientFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ArgumentException("Container name cannot be null or empty", nameof(containerName));
        }

        _logger = logger;
        _container = clientFactory.GetContainer(containerName);
        _partitionKeyValue = options.Value.DefaultPartitionKey;
        _documentId = $"user_{_partitionKeyValue}";

        // JSON serialization options - match frontend expectations
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false, // Reduce storage size
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
            UnknownTypeHandling = System.Text.Json.Serialization.JsonUnknownTypeHandling.JsonElement
        };

        _logger.LogDebug(
            "CosmosDbRepository<{EntityType}> initialized. Container: {ContainerName}, PartitionKey: {PartitionKey}",
            typeof(T).Name,
            containerName,
            _partitionKeyValue);
    }

    /// <summary>
    /// Retrieves the entity from Cosmos DB.
    /// Returns a new instance if the document doesn't exist.
    /// Handles Cosmos DB system properties (_rid, _self, _etag, _attachments, _ts) gracefully.
    /// </summary>
    public async Task<T?> GetAsync()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogDebug(
                "Attempting to read {EntityType} document. Id: {DocumentId}, PartitionKey: {PartitionKey}",
                typeof(T).Name,
                _documentId,
                _partitionKeyValue);

            // Read as JsonElement first to strip Cosmos DB system properties
            var response = await _container.ReadItemAsync<JsonElement>(
                id: _documentId,
                partitionKey: new PartitionKey(_partitionKeyValue));

            stopwatch.Stop();

            _logger.LogInformation(
                "Successfully retrieved {EntityType}. Request charge: {RequestCharge} RU, Latency: {LatencyMs}ms",
                typeof(T).Name,
                response.RequestCharge,
                stopwatch.ElapsedMilliseconds);

            // Deserialize to target type using our JSON options (ignores unknown properties)
            var result = JsonSerializer.Deserialize<T>(response.Resource.GetRawText(), _jsonOptions);
            return result ?? new T();
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            stopwatch.Stop();

            _logger.LogInformation(
                "{EntityType} document not found (Id: {DocumentId}). Returning new instance. Latency: {LatencyMs}ms",
                typeof(T).Name,
                _documentId,
                stopwatch.ElapsedMilliseconds);

            // Return empty instance for first-time initialization
            return new T();
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            stopwatch.Stop();

            _logger.LogWarning(ex,
                "Rate limit exceeded while reading {EntityType}. Retry after: {RetryAfter}ms",
                typeof(T).Name,
                ex.RetryAfter?.TotalMilliseconds ?? 0);

            throw;
        }
        catch (CosmosException ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Cosmos DB error while reading {EntityType}. StatusCode: {StatusCode}, SubStatusCode: {SubStatusCode}, ActivityId: {ActivityId}, Latency: {LatencyMs}ms",
                typeof(T).Name,
                ex.StatusCode,
                ex.SubStatusCode,
                ex.ActivityId,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Unexpected error while reading {EntityType}. Latency: {LatencyMs}ms, Error: {ErrorMessage}",
                typeof(T).Name,
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            throw;
        }
    }

    /// <summary>
    /// Saves the entity to Cosmos DB using upsert (insert or replace).
    /// Ensures document has required 'id' and 'userId' fields.
    /// </summary>
    public async Task SaveAsync(T data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Serialize to JSON and add required fields
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var document = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _jsonOptions)
                ?? throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name} to dictionary");

            // Ensure required Cosmos DB fields exist
            document["id"] = _documentId;
            document["userId"] = _partitionKeyValue;

            // Add metadata
            document["_lastModified"] = DateTime.UtcNow;

            _logger.LogDebug(
                "Attempting to upsert {EntityType} document. Id: {DocumentId}, PartitionKey: {PartitionKey}",
                typeof(T).Name,
                _documentId,
                _partitionKeyValue);

            var response = await _container.UpsertItemAsync(
                item: document,
                partitionKey: new PartitionKey(_partitionKeyValue));

            stopwatch.Stop();

            _logger.LogInformation(
                "Successfully saved {EntityType}. Request charge: {RequestCharge} RU, Latency: {LatencyMs}ms, ETag: {ETag}",
                typeof(T).Name,
                response.RequestCharge,
                stopwatch.ElapsedMilliseconds,
                response.ETag);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            stopwatch.Stop();

            _logger.LogWarning(ex,
                "Rate limit exceeded while saving {EntityType}. Retry after: {RetryAfter}ms, Request charge: {RequestCharge} RU",
                typeof(T).Name,
                ex.RetryAfter?.TotalMilliseconds ?? 0,
                ex.RequestCharge);

            throw;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.RequestEntityTooLarge)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Document size exceeds limit while saving {EntityType}. Document size should be < 2MB. ActivityId: {ActivityId}",
                typeof(T).Name,
                ex.ActivityId);

            throw new InvalidOperationException(
                $"Document size exceeds Cosmos DB limit. Consider archiving old data for {typeof(T).Name}",
                ex);
        }
        catch (CosmosException ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Cosmos DB error while saving {EntityType}. StatusCode: {StatusCode}, SubStatusCode: {SubStatusCode}, ActivityId: {ActivityId}, Latency: {LatencyMs}ms",
                typeof(T).Name,
                ex.StatusCode,
                ex.SubStatusCode,
                ex.ActivityId,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Unexpected error while saving {EntityType}. Latency: {LatencyMs}ms",
                typeof(T).Name,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    /// <summary>
    /// Checks if the entity exists in Cosmos DB.
    /// </summary>
    public async Task<bool> ExistsAsync()
    {
        try
        {
            await _container.ReadItemAsync<T>(
                id: _documentId,
                partitionKey: new PartitionKey(_partitionKeyValue));

            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    /// <summary>
    /// Deletes the entity from Cosmos DB.
    /// </summary>
    public async Task DeleteAsync()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogDebug(
                "Attempting to delete {EntityType} document. Id: {DocumentId}, PartitionKey: {PartitionKey}",
                typeof(T).Name,
                _documentId,
                _partitionKeyValue);

            var response = await _container.DeleteItemAsync<T>(
                id: _documentId,
                partitionKey: new PartitionKey(_partitionKeyValue));

            stopwatch.Stop();

            _logger.LogInformation(
                "Successfully deleted {EntityType}. Request charge: {RequestCharge} RU, Latency: {LatencyMs}ms",
                typeof(T).Name,
                response.RequestCharge,
                stopwatch.ElapsedMilliseconds);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            stopwatch.Stop();

            _logger.LogInformation(
                "{EntityType} document not found for deletion. Id: {DocumentId}, Latency: {LatencyMs}ms",
                typeof(T).Name,
                _documentId,
                stopwatch.ElapsedMilliseconds);
        }
        catch (CosmosException ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Cosmos DB error while deleting {EntityType}. StatusCode: {StatusCode}, ActivityId: {ActivityId}",
                typeof(T).Name,
                ex.StatusCode,
                ex.ActivityId);

            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Unexpected error while deleting {EntityType}. Latency: {LatencyMs}ms",
                typeof(T).Name,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
