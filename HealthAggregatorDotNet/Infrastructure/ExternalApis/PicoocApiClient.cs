using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HealthAggregator.Core.Interfaces;
using HealthAggregator.Core.Models.Picooc;
using Microsoft.Extensions.Logging;

namespace HealthAggregator.Infrastructure.ExternalApis;

/// <summary>
/// Native .NET implementation of Picooc API client.
/// Replaces Docker-based SmartScaleConnect approach.
/// </summary>
public class PicoocApiClient : IPicoocSyncClient
{
    private const string ApiBaseUrl = "https://api2.picooc-int.com/v1/api/";
    private const string AppVersion = "i4.1.11.0";
    private const int PageSize = 1000;

    private readonly HttpClient _httpClient;
    private readonly string _email;
    private readonly string _password;
    private readonly ILogger<PicoocApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private string _deviceId = string.Empty;
    private string _userId = string.Empty;
    private string _roleId = string.Empty;

    public PicoocApiClient(
        HttpClient httpClient,
        string email,
        string password,
        ILogger<PicoocApiClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(ApiBaseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(1);

        _email = email;
        _password = password;
        _logger = logger;

        // Picooc API uses snake_case but with some mixed casing
        // We'll use case-insensitive deserialization
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Checks if credentials are configured.
    /// </summary>
    public Task<bool> CheckDockerAvailableAsync()
    {
        // Renamed semantically - no Docker needed anymore
        return Task.FromResult(IsConfigured());
    }

    /// <summary>
    /// Checks if Picooc credentials are configured.
    /// </summary>
    public bool IsConfigured()
    {
        return !string.IsNullOrEmpty(_email) && !string.IsNullOrEmpty(_password);
    }

    /// <summary>
    /// Syncs all weight measurements from Picooc cloud.
    /// </summary>
    public async Task<IEnumerable<HealthMeasurement>> SyncFromCloudAsync()
    {
        if (!IsConfigured())
        {
            _logger.LogWarning("Picooc credentials not configured");
            return [];
        }

        try
        {
            _logger.LogInformation("Starting Picooc cloud sync...");

            // Step 1: Login
            var loginSuccess = await LoginAsync();
            if (!loginSuccess)
            {
                _logger.LogError("Picooc login failed");
                return [];
            }

            _logger.LogInformation("Picooc login successful, userId: {UserId}", _userId);

            // Step 2: Fetch all weights with pagination
            var measurements = await GetAllWeightsAsync();

            _logger.LogInformation("Picooc sync completed, retrieved {Count} measurements", measurements.Count);

            return measurements;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Picooc sync failed with error");
            return [];
        }
    }

    /// <summary>
    /// Authenticates with Picooc API.
    /// </summary>
    private async Task<bool> LoginAsync()
    {
        EnsureDeviceId();

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var method = "user_login_new";
        var sign = GenerateSignature(method, timestamp);

        var loginRequest = new PicoocLoginRequest
        {
            Appver = AppVersion,
            Timestamp = timestamp,
            Lang = "en",
            Method = method,
            Timezone = "",
            Sign = sign,
            PushToken = $"android::{_deviceId}",
            DeviceId = _deviceId,
            Req = new PicoocLoginRequestRec
            {
                AppVersion = AppVersion,
                Email = _email,
                Password = _password
            }
        };

        var reqDataJson = JsonSerializer.Serialize(loginRequest, _jsonOptions);

        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["appver"] = AppVersion,
            ["timestamp"] = timestamp,
            ["lang"] = "en",
            ["method"] = method,
            ["timezone"] = "",
            ["sign"] = sign,
            ["push_token"] = $"android::{_deviceId}",
            ["device_id"] = _deviceId,
            ["reqData"] = reqDataJson
        });

        var response = await _httpClient.PostAsync("account/login", formContent);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Picooc login HTTP error: {StatusCode}", response.StatusCode);
            return false;
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        _logger.LogDebug("Picooc login response: {Response}", responseBody);

        var loginResponse = JsonSerializer.Deserialize<PicoocLoginResponse>(responseBody, _jsonOptions);

        if (loginResponse == null)
        {
            _logger.LogError("Picooc login response deserialization failed");
            return false;
        }
        
        if (loginResponse.Code != 0)
        {
            _logger.LogError("Picooc login failed: code={Code}, msg={Message}", loginResponse.Code, loginResponse.Msg);
            return false;
        }

        _userId = loginResponse.Resp.User_id;
        _roleId = loginResponse.Resp.Role_id;

        return true;
    }

    /// <summary>
    /// Fetches all weight measurements with pagination.
    /// </summary>
    private async Task<List<HealthMeasurement>> GetAllWeightsAsync()
    {
        var allMeasurements = new List<HealthMeasurement>();
        var lastTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var hasMore = true;

        while (hasMore)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var method = "bodyIndexList";
            var sign = GenerateSignature(method, timestamp);

            var queryParams = new Dictionary<string, string>
            {
                ["appver"] = AppVersion,
                ["timestamp"] = timestamp,
                ["lang"] = "en",
                ["method"] = method,
                ["timezone"] = "",
                ["sign"] = sign,
                ["push_token"] = $"android::{_deviceId}",
                ["device_id"] = _deviceId,
                ["pageSize"] = PageSize.ToString(),
                ["time"] = lastTime,
                ["userId"] = _userId,
                ["roleId"] = _roleId
            };

            var queryString = string.Join("&", queryParams.Select(kvp =>
                $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            var response = await _httpClient.GetAsync($"bodyIndex/bodyIndexList?{queryString}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Picooc bodyIndexList HTTP error: {StatusCode}", response.StatusCode);
                break;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Picooc bodyIndexList response length: {Length}", responseBody.Length);

            var bodyIndexResponse = JsonSerializer.Deserialize<PicoocBodyIndexResponse>(responseBody, _jsonOptions);

            if (bodyIndexResponse?.Resp.Records == null)
            {
                _logger.LogWarning("Picooc bodyIndexList returned null records");
                break;
            }

            foreach (var record in bodyIndexResponse.Resp.Records)
            {
                // Skip deleted or abnormal records (matching Go logic)
                if (record.Abnormal_flag != 0 || record.Is_del != 0)
                {
                    continue;
                }

                var measurement = new HealthMeasurement
                {
                    Date = DateTimeOffset.FromUnixTimeSeconds(record.BodyTime).LocalDateTime,
                    Weight = record.Weight,
                    BMI = record.Bmi,
                    BodyFat = record.Body_fat,
                    BodyWater = record.Water_race,
                    BoneMass = record.Bone_mass,
                    MetabolicAge = record.Body_age,
                    VisceralFat = record.Visceral_fat_level,
                    BasalMetabolism = record.Basic_metabolism,
                    SkeletalMuscleMass = record.Skeletal_muscle,
                    Source = string.IsNullOrEmpty(record.Mac) ? "picooc-api" : record.Mac
                };

                allMeasurements.Add(measurement);
            }

            _logger.LogDebug("Fetched {Count} records, continue={Continue}, lastTime={LastTime}",
                bodyIndexResponse.Resp.Records.Count,
                bodyIndexResponse.Resp.Continue,
                bodyIndexResponse.Resp.LastTime);

            hasMore = bodyIndexResponse.Resp.Continue;
            if (hasMore && bodyIndexResponse.Resp.LastTime.HasValue)
            {
                lastTime = bodyIndexResponse.Resp.LastTime.Value.ToString();
            }
            else
            {
                hasMore = false; // Stop if LastTime is null
            }
        }

        return allMeasurements;
    }

    /// <summary>
    /// Generates MD5 signature required by Picooc API.
    /// Algorithm: MD5(deviceId + MD5(timestamp + MD5(method) + MD5(appVer)))
    /// </summary>
    private string GenerateSignature(string method, string timestamp)
    {
        var methodHash = ComputeMD5Upper(method);
        var appVerHash = ComputeMD5Upper(AppVersion);
        var innerHash = ComputeMD5Upper(timestamp + methodHash + appVerHash);
        var signature = ComputeMD5Upper(_deviceId + innerHash);

        return signature;
    }

    /// <summary>
    /// Computes uppercase MD5 hash of input string.
    /// </summary>
    private static string ComputeMD5Upper(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = MD5.HashData(inputBytes);
        return Convert.ToHexString(hashBytes); // Returns uppercase by default
    }

    /// <summary>
    /// Ensures device ID is initialized (generates UUID if needed).
    /// </summary>
    private void EnsureDeviceId()
    {
        if (string.IsNullOrEmpty(_deviceId))
        {
            _deviceId = Guid.NewGuid().ToString("D").ToUpperInvariant();
            _logger.LogDebug("Generated device ID: {DeviceId}", _deviceId);
        }
    }
}
