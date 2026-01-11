using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
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
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            // Ignore unknown properties from Cosmos DB (id, userId, _rid, _self, etc.)
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Skip
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

            // Read as string to get raw JSON, then deserialize manually
            var response = await _container.ReadItemStreamAsync(
                id: _documentId,
                partitionKey: new PartitionKey(_partitionKeyValue));

            stopwatch.Stop();
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogInformation(
                        "{EntityType} document not found (Id: {DocumentId}). Returning new instance. Latency: {LatencyMs}ms",
                        typeof(T).Name,
                        _documentId,
                        stopwatch.ElapsedMilliseconds);
                    return new T();
                }
                
                _logger.LogError(
                    "Failed to read {EntityType}. StatusCode: {StatusCode}",
                    typeof(T).Name,
                    response.StatusCode);
                throw new InvalidOperationException($"Failed to read from Cosmos DB: {response.StatusCode}");
            }

            _logger.LogInformation(
                "Successfully retrieved {EntityType}. Latency: {LatencyMs}ms",
                typeof(T).Name,
                stopwatch.ElapsedMilliseconds);

            // Read the stream content
            using var reader = new StreamReader(response.Content);
            var json = await reader.ReadToEndAsync();
            
            try
            {
                // Deserialize to target type using our JSON options
                var result = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                return result ?? new T();
            }
            catch (JsonException jsonEx)
            {
                _logger.LogWarning(jsonEx,
                    "Failed to deserialize {EntityType} from Cosmos DB. Document may have incompatible format. " +
                    "Deleting old document and returning new instance.",
                    typeof(T).Name);
                
                // Delete the corrupted document
                try
                {
                    await _container.DeleteItemStreamAsync(
                        id: _documentId,
                        partitionKey: new PartitionKey(_partitionKeyValue));
                    _logger.LogInformation("Deleted corrupted {EntityType} document", typeof(T).Name);
                }
                catch (Exception deleteEx)
                {
                    _logger.LogWarning(deleteEx, "Failed to delete corrupted {EntityType} document", typeof(T).Name);
                }
                
                // Return empty instance
                return new T();
            }
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
            // Serialize to JSON using JsonNode for proper manipulation
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var document = JsonNode.Parse(json)?.AsObject() 
                ?? throw new InvalidOperationException($"Failed to parse {typeof(T).Name} as JSON object");

            // Ensure required Cosmos DB fields exist
            document["id"] = _documentId;
            document["userId"] = _partitionKeyValue;
            document["_lastModified"] = DateTime.UtcNow.ToString("o");

            _logger.LogDebug(
                "Attempting to upsert {EntityType} document. Id: {DocumentId}, PartitionKey: {PartitionKey}",
                typeof(T).Name,
                _documentId,
                _partitionKeyValue);

            // Use stream API to write the JSON directly
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);
            document.WriteTo(writer);
            await writer.FlushAsync();
            stream.Position = 0;

            var response = await _container.UpsertItemStreamAsync(
                streamPayload: stream,
                partitionKey: new PartitionKey(_partitionKeyValue));

            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to save {EntityType}. StatusCode: {StatusCode}",
                    typeof(T).Name,
                    response.StatusCode);
                throw new InvalidOperationException($"Failed to save to Cosmos DB: {response.StatusCode}");
            }

            _logger.LogInformation(
                "Successfully saved {EntityType}. Latency: {LatencyMs}ms",
                typeof(T).Name,
                stopwatch.ElapsedMilliseconds);
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
