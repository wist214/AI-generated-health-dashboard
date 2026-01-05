using System.Text.Json;
using HealthAggregator.Core.Interfaces;

namespace HealthAggregator.Infrastructure.Persistence;

/// <summary>
/// File-based data repository implementation.
/// Stores data as JSON files for persistence.
/// </summary>
public class FileDataRepository<T> : IDataRepository<T> where T : class, new()
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public FileDataRepository(string filePath)
    {
        _filePath = filePath;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true // Allow reading both camelCase and PascalCase
        };
    }

    public async Task<T?> GetAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (!File.Exists(_filePath))
                return null;

            var json = await File.ReadAllTextAsync(_filePath);
            if (string.IsNullOrWhiteSpace(json))
                return null;
                
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON deserialization error in {_filePath}: {ex.Message}");
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(T data)
    {
        await _lock.WaitAsync();
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(data, _jsonOptions);
            await File.WriteAllTextAsync(_filePath, json);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> ExistsAsync()
    {
        return await Task.FromResult(File.Exists(_filePath));
    }

    public async Task DeleteAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (File.Exists(_filePath))
                File.Delete(_filePath);
        }
        finally
        {
            _lock.Release();
        }
    }
}
