using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthAggregatorV2.Functions.Infrastructure.ExternalApis.Picooc;

/// <summary>
/// Client for fetching Picooc scale data via SmartScaleConnect Docker container.
/// </summary>
public class PicoocApiClient : IPicoocApiClient
{
    private readonly PicoocApiOptions _options;
    private readonly ILogger<PicoocApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PicoocApiClient(
        IOptions<PicoocApiOptions> options,
        ILogger<PicoocApiClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IEnumerable<PicoocMeasurement>> GetMeasurementsAsync(
        DateTime? startDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if Docker is running and container exists
            if (!await IsDockerAvailableAsync(cancellationToken))
            {
                _logger.LogWarning("Docker is not available, skipping Picooc sync");
                return Enumerable.Empty<PicoocMeasurement>();
            }

            // Execute SmartScaleConnect in Docker container
            var command = BuildSyncCommand(startDate);
            var result = await ExecuteDockerCommandAsync(command, cancellationToken);

            if (string.IsNullOrEmpty(result))
            {
                _logger.LogWarning("Empty response from SmartScaleConnect");
                return Enumerable.Empty<PicoocMeasurement>();
            }

            // Parse JSON response
            var response = JsonSerializer.Deserialize<PicoocSyncResponse>(result, JsonOptions);

            if (!string.IsNullOrEmpty(response?.Error))
            {
                _logger.LogError("SmartScaleConnect error: {Error}", response.Error);
                return Enumerable.Empty<PicoocMeasurement>();
            }

            _logger.LogInformation("Retrieved {Count} measurements from Picooc", response?.Measurements.Count ?? 0);
            return response?.Measurements ?? Enumerable.Empty<PicoocMeasurement>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Picooc data");
            throw;
        }
    }

    private string BuildSyncCommand(DateTime? startDate)
    {
        var baseCommand = $"smartscaleconnect sync --email \"{_options.Email}\" --password \"{_options.Password}\" --format json";

        if (startDate.HasValue)
        {
            baseCommand += $" --from {startDate.Value:yyyy-MM-dd}";
        }

        return baseCommand;
    }

    private async Task<bool> IsDockerAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await ExecuteCommandAsync("docker", "ps", cancellationToken);
            return !string.IsNullOrEmpty(result);
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> ExecuteDockerCommandAsync(string command, CancellationToken cancellationToken)
    {
        var dockerCommand = $"exec {_options.DockerContainerName} {command}";
        return await ExecuteCommandAsync("docker", dockerCommand, cancellationToken);
    }

    private async Task<string> ExecuteCommandAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        _logger.LogDebug("Executing: {FileName} {Arguments}", fileName, arguments);

        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var output = await outputTask;
        var error = await errorTask;

        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning("Process stderr: {Error}", error);
        }

        if (process.ExitCode != 0)
        {
            _logger.LogError("Process exited with code {ExitCode}", process.ExitCode);
            throw new InvalidOperationException($"Command failed with exit code {process.ExitCode}: {error}");
        }

        return output;
    }
}
