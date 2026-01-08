using System.Net.Http.Headers;
using System.Text.Json;
using HealthAggregatorApi.Core.Interfaces;
using HealthAggregatorApi.Core.Models.Oura;

namespace HealthAggregatorApi.Infrastructure.ExternalApis;

/// <summary>
/// Oura Ring API v2 client implementation.
/// </summary>
public class OuraApiClient : IOuraApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private const string BaseUrl = "https://api.ouraring.com/v2/usercollection";

    public OuraApiClient(HttpClient httpClient, string accessToken)
    {
        _httpClient = httpClient;
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

    public async Task<List<OuraStressRecord>> GetDailyStressAsync(DateTime startDate, DateTime endDate)
    {
        var url = $"{BaseUrl}/daily_stress?start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}";
        return await FetchPaginatedDataAsync<OuraStressRecord>(url);
    }

    public async Task<List<OuraResilienceRecord>> GetDailyResilienceAsync(DateTime startDate, DateTime endDate)
    {
        var url = $"{BaseUrl}/daily_resilience?start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}";
        return await FetchPaginatedDataAsync<OuraResilienceRecord>(url);
    }

    public async Task<List<OuraVo2MaxRecord>> GetVo2MaxAsync(DateTime startDate, DateTime endDate)
    {
        var url = $"{BaseUrl}/vO2_max?start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}";
        return await FetchPaginatedDataAsync<OuraVo2MaxRecord>(url);
    }

    public async Task<List<OuraCardiovascularAgeRecord>> GetCardiovascularAgeAsync(DateTime startDate, DateTime endDate)
    {
        var url = $"{BaseUrl}/daily_cardiovascular_age?start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}";
        return await FetchPaginatedDataAsync<OuraCardiovascularAgeRecord>(url);
    }

    public async Task<List<OuraWorkoutRecord>> GetWorkoutsAsync(DateTime startDate, DateTime endDate)
    {
        var url = $"{BaseUrl}/workout?start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}";
        return await FetchPaginatedDataAsync<OuraWorkoutRecord>(url);
    }

    public async Task<List<OuraSleepTimeRecord>> GetSleepTimeAsync(DateTime startDate, DateTime endDate)
    {
        var url = $"{BaseUrl}/sleep_time?start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}";
        return await FetchPaginatedDataAsync<OuraSleepTimeRecord>(url);
    }

    public async Task<List<OuraSpO2Record>> GetSpO2Async(DateTime startDate, DateTime endDate)
    {
        var url = $"{BaseUrl}/daily_spo2?start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}";
        return await FetchPaginatedDataAsync<OuraSpO2Record>(url);
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
                    break;

                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json))
                    break;
                    
                var result = JsonSerializer.Deserialize<OuraApiResponse<T>>(json, _jsonOptions);
                
                if (result?.Data != null)
                    allData.AddRange(result.Data);
                    
                nextToken = result?.NextToken;
            }
            catch
            {
                break;
            }
        } while (!string.IsNullOrEmpty(nextToken));

        return allData;
    }
}

internal class OuraApiResponse<T>
{
    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public List<T>? Data { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("next_token")]
    public string? NextToken { get; set; }
}
