using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HealthAggregatorApi.Infrastructure.Configuration;

namespace HealthAggregatorApi.Infrastructure.Persistence;

/// <summary>
/// Thread-safe factory for creating and managing Cosmos DB client instances.
/// Implements Singleton pattern with lazy initialization.
/// </summary>
public sealed class CosmosDbClientFactory : ICosmosDbClientFactory, IDisposable
{
    private readonly CosmosDbOptions _options;
    private readonly ILogger<CosmosDbClientFactory> _logger;
    private readonly Lazy<CosmosClient> _lazyClient;
    private Database? _database;
    private bool _disposed;

    public CosmosDbClientFactory(
        IOptions<CosmosDbOptions> options,
        ILogger<CosmosDbClientFactory> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Validate configuration
        _options.Validate();

        // Lazy initialization ensures thread-safe singleton creation
        _lazyClient = new Lazy<CosmosClient>(CreateCosmosClient);

        _logger.LogInformation(
            "CosmosDbClientFactory initialized. Database: {DatabaseName}, UseEmulator: {UseEmulator}",
            _options.DatabaseName,
            _options.UseEmulator);
    }

    /// <summary>
    /// Gets the singleton CosmosClient instance.
    /// Thread-safe lazy initialization.
    /// </summary>
    public CosmosClient GetClient()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _lazyClient.Value;
    }

    /// <summary>
    /// Gets a container reference from the configured database.
    /// </summary>
    public Container GetContainer(string containerName)
    {
        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ArgumentException("Container name cannot be null or empty", nameof(containerName));
        }

        ObjectDisposedException.ThrowIf(_disposed, this);

        var client = GetClient();
        _database ??= client.GetDatabase(_options.DatabaseName);

        return _database.GetContainer(containerName);
    }

    /// <summary>
    /// Ensures the database and specified containers exist.
    /// Creates them if they don't exist. Idempotent operation.
    /// </summary>
    public async Task EnsureDatabaseAndContainersExistAsync(
        IEnumerable<string> containerNames,
        string partitionKeyPath = "/userId",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(containerNames);

        if (string.IsNullOrWhiteSpace(partitionKeyPath))
        {
            throw new ArgumentException("Partition key path cannot be null or empty", nameof(partitionKeyPath));
        }

        ObjectDisposedException.ThrowIf(_disposed, this);

        var client = GetClient();

        try
        {
            // Create database if it doesn't exist
            _logger.LogInformation("Ensuring database {DatabaseName} exists", _options.DatabaseName);
            
            var databaseResponse = await client.CreateDatabaseIfNotExistsAsync(
                _options.DatabaseName,
                cancellationToken: cancellationToken);

            _database = databaseResponse.Database;

            if (databaseResponse.StatusCode == System.Net.HttpStatusCode.Created)
            {
                _logger.LogInformation("Created database {DatabaseName}", _options.DatabaseName);
            }
            else
            {
                _logger.LogDebug("Database {DatabaseName} already exists", _options.DatabaseName);
            }

            // Create containers if they don't exist
            foreach (var containerName in containerNames)
            {
                if (string.IsNullOrWhiteSpace(containerName))
                {
                    _logger.LogWarning("Skipping empty container name");
                    continue;
                }

                _logger.LogInformation("Ensuring container {ContainerName} exists", containerName);

                var containerResponse = await _database.CreateContainerIfNotExistsAsync(
                    id: containerName,
                    partitionKeyPath: partitionKeyPath,
                    cancellationToken: cancellationToken);

                if (containerResponse.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    _logger.LogInformation(
                        "Created container {ContainerName} with partition key {PartitionKeyPath}",
                        containerName,
                        partitionKeyPath);
                }
                else
                {
                    _logger.LogDebug("Container {ContainerName} already exists", containerName);
                }
            }
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex,
                "Cosmos DB error while ensuring database/containers exist. StatusCode: {StatusCode}, Message: {Message}",
                ex.StatusCode,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while ensuring database/containers exist");
            throw;
        }
    }

    /// <summary>
    /// Creates the CosmosClient with optimal configuration.
    /// </summary>
    private CosmosClient CreateCosmosClient()
    {
        var connectionString = _options.EffectiveConnectionString;

        // Configure client options for optimal performance and reliability
        var clientOptions = new CosmosClientOptions
        {
            // Serialization - use camelCase for JSON compatibility with frontend
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                IgnoreNullValues = false
            },

            // Connection settings - use Gateway for emulator (Direct doesn't work with Docker emulator)
            ConnectionMode = _options.UseEmulator ? ConnectionMode.Gateway : ConnectionMode.Direct,
            MaxRetryAttemptsOnRateLimitedRequests = _options.MaxRetryAttempts,
            MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(_options.MaxRetryWaitTimeSeconds),

            // Request timeout - shorter for emulator to fail faster
            RequestTimeout = TimeSpan.FromSeconds(_options.UseEmulator ? 30 : 60),

            // Application name for diagnostics
            ApplicationName = "HealthAggregator",

            // Enable content response on write (needed for ETags)
            AllowBulkExecution = false,

            // Consistency level (Session is default and best for single-user)
            ConsistencyLevel = ConsistencyLevel.Session
        };

        // Disable SSL validation for local emulator
        if (_options.UseEmulator)
        {
            _logger.LogWarning("Using Cosmos DB Emulator - SSL validation disabled, using Gateway mode");
            clientOptions.HttpClientFactory = () =>
            {
                var httpHandler = new SocketsHttpHandler
                {
                    SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                    {
                        // Disable SSL certificate validation for emulator
                        RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true
                    },
                    // Set connection timeout
                    ConnectTimeout = TimeSpan.FromSeconds(10)
                };
                return new HttpClient(httpHandler)
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };
            };
        }

        _logger.LogInformation("Creating CosmosClient with connection mode: {ConnectionMode}", 
            clientOptions.ConnectionMode);

        return new CosmosClient(connectionString, clientOptions);
    }

    /// <summary>
    /// Disposes the CosmosClient instance.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_lazyClient.IsValueCreated)
        {
            _logger.LogInformation("Disposing CosmosClient");
            _lazyClient.Value?.Dispose();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
