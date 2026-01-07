using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using HealthAggregatorApi.Core.Interfaces;
using HealthAggregatorApi.Core.Models.Settings;

namespace HealthAggregatorApi.Functions;

/// <summary>
/// Azure Functions for user settings management.
/// Settings are stored in Azure Blob Storage.
/// </summary>
public class SettingsFunctions
{
    private readonly IDataRepository<UserSettings> _repository;
    private readonly ILogger<SettingsFunctions> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public SettingsFunctions(IDataRepository<UserSettings> repository, ILogger<SettingsFunctions> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Get user settings.
    /// </summary>
    [Function("GetSettings")]
    public async Task<HttpResponseData> GetSettings(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "settings")] HttpRequestData req)
    {
        _logger.LogInformation("GetSettings called");

        try
        {
            var settings = await _repository.GetAsync();
            
            // Return default settings if none exist
            settings ??= new UserSettings();

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            await response.WriteStringAsync(JsonSerializer.Serialize(settings, JsonOptions));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settings");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            await response.WriteStringAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            return response;
        }
    }

    /// <summary>
    /// Save user settings.
    /// </summary>
    [Function("SaveSettings")]
    public async Task<HttpResponseData> SaveSettings(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "settings")] HttpRequestData req)
    {
        _logger.LogInformation("SaveSettings called");

        try
        {
            var requestBody = await req.ReadAsStringAsync();
            
            if (string.IsNullOrEmpty(requestBody))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                badResponse.Headers.Add("Access-Control-Allow-Origin", "*");
                await badResponse.WriteStringAsync(JsonSerializer.Serialize(new { error = "Request body is required" }));
                return badResponse;
            }

            var settings = JsonSerializer.Deserialize<UserSettings>(requestBody, JsonOptions);
            
            if (settings == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                badResponse.Headers.Add("Access-Control-Allow-Origin", "*");
                await badResponse.WriteStringAsync(JsonSerializer.Serialize(new { error = "Invalid settings format" }));
                return badResponse;
            }

            // Set last updated timestamp
            settings.LastUpdated = DateTime.UtcNow;

            // Save to blob storage
            await _repository.SaveAsync(settings);

            _logger.LogInformation("Settings saved successfully");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            await response.WriteStringAsync(JsonSerializer.Serialize(new { 
                success = true, 
                message = "Settings saved successfully",
                settings 
            }, JsonOptions));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            await response.WriteStringAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            return response;
        }
    }

    /// <summary>
    /// Handle CORS preflight requests.
    /// </summary>
    [Function("SettingsOptions")]
    public HttpResponseData Options(
        [HttpTrigger(AuthorizationLevel.Anonymous, "options", Route = "settings")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
        return response;
    }
}
