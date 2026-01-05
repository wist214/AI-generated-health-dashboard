using System.Net.Http.Headers;
using System.Text.Json;
using HealthAggregator.Core.Interfaces;
using HealthAggregator.Core.Models.Oura;

namespace HealthAggregator.Infrastructure.ExternalApis;

/// <summary>
/// Oura Ring API v2 client implementation.
/// Handles all communication with Oura's REST API.
/// </summary>
public class OuraApiClient : IOuraApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private const string BaseUrl = "https://api.ouraring.com/v2/usercollection";

    public OuraApiClient(string accessToken)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", accessToken);
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<OuraSleepRecord>> GetSleepRecordsAsync(DateTime startDate, DateTime endDate)
    {
        var url = $"{BaseUrl}/sleep?start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}";
        return await FetchPaginatedDataAsync<OuraSleepRecord>(url);
    }

    public async Task<List<OuraDailySleepRecord>> GetDailySleepAsync(DateTime startDate, DateTime endDate)
    {
        var url = $"{BaseUrl}/daily_sleep?start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}";
        return await FetchPaginatedDataAsync<OuraDailySleepRecord>(url);
    }

    public async Task<List<OuraReadinessRecord>> GetReadinessAsync(DateTime startDate, DateTime endDate)
    {
        var url = $"{BaseUrl}/daily_readiness?start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}";
        return await FetchPaginatedDataAsync<OuraReadinessRecord>(url);
    }

    public async Task<List<OuraActivityRecord>> GetActivityAsync(DateTime startDate, DateTime endDate)
    {
        var url = $"{BaseUrl}/daily_activity?start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}";
        return await FetchPaginatedDataAsync<OuraActivityRecord>(url);
    }

    public async Task<OuraPersonalInfo?> GetPersonalInfoAsync()
    {
        var url = "https://api.ouraring.com/v2/usercollection/personal_info";
        var response = await _httpClient.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
            return null;
            
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<OuraPersonalInfo>(json, _jsonOptions);
    }

    private async Task<List<T>> FetchPaginatedDataAsync<T>(string url)
    {
        var allData = new List<T>();
        string? nextToken = null;

        do
        {
            try
            {
                var requestUrl = nextToken != null ? $"{url}&next_token={nextToken}" : url;
                var response = await _httpClient.GetAsync(requestUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Oura API error ({response.StatusCode}): {errorContent}");
                    break;
                }

                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json))
                    break;
                    
                var result = JsonSerializer.Deserialize<OuraApiResponse<T>>(json, _jsonOptions);
                
                if (result?.Data != null)
                    allData.AddRange(result.Data);
                    
                nextToken = result?.NextToken;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Oura API exception: {ex.Message}");
                break;
            }
        } while (!string.IsNullOrEmpty(nextToken));

        return allData;
    }
}

/// <summary>
/// Generic response wrapper for Oura API paginated results.
/// </summary>
internal class OuraApiResponse<T>
{
    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public List<T>? Data { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("next_token")]
    public string? NextToken { get; set; }
}
