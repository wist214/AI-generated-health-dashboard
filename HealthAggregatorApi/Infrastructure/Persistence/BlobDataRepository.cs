using Azure.Storage.Blobs;
using System.Text.Json;
using HealthAggregatorApi.Core.Interfaces;

namespace HealthAggregatorApi.Infrastructure.Persistence;

/// <summary>
/// Azure Blob Storage implementation of data repository.
/// </summary>
public class BlobDataRepository<T> : IDataRepository<T> where T : class, new()
{
    private readonly BlobContainerClient _container;
    private readonly string _blobName;
    private readonly JsonSerializerOptions _jsonOptions;

    public BlobDataRepository(string connectionString, string containerName, string blobName)
    {
        var client = new BlobServiceClient(connectionString);
        _container = client.GetBlobContainerClient(containerName);
        _container.CreateIfNotExists();
        _blobName = blobName;
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<T?> GetAsync()
    {
        var blob = _container.GetBlobClient(_blobName);
        
        if (!await blob.ExistsAsync())
            return new T();

        var response = await blob.DownloadContentAsync();
        var json = response.Value.Content.ToString();
        
        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    public async Task SaveAsync(T data)
    {
        var blob = _container.GetBlobClient(_blobName);
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        
        await blob.UploadAsync(BinaryData.FromString(json), overwrite: true);
    }

    public async Task<bool> ExistsAsync()
    {
        var blob = _container.GetBlobClient(_blobName);
        return await blob.ExistsAsync();
    }

    public async Task DeleteAsync()
    {
        var blob = _container.GetBlobClient(_blobName);
        await blob.DeleteIfExistsAsync();
    }
}
