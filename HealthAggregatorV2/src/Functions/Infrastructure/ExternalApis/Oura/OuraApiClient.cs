using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthAggregatorV2.Functions.Infrastructure.ExternalApis.Oura;

/// <summary>
/// HTTP client for Oura API v2.
/// </summary>
public class OuraApiClient : IOuraApiClient
{
    private readonly HttpClient _httpClient;
    private readonly OuraApiOptions _options;
    private readonly ILogger<OuraApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OuraApiClient(
        HttpClient httpClient,
        IOptions<OuraApiOptions> options,
        ILogger<OuraApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        // Configure base address and auth header
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        if (!string.IsNullOrEmpty(_options.AccessToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.AccessToken);
        }
    }

    public async Task<IEnumerable<OuraSleepData>> GetDailySleepAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var url = $"usercollection/daily_sleep?start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}";
        return await FetchAllPagesAsync<OuraSleepData>(url, cancellationToken);
    }

    public async Task<IEnumerable<OuraSleepDocument>> GetSleepDocumentsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var url = $"usercollection/sleep?start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}";
        return await FetchAllPagesAsync<OuraSleepDocument>(url, cancellationToken);
    }

    public async Task<IEnumerable<OuraActivityData>> GetDailyActivityAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var url = $"usercollection/daily_activity?start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}";
        return await FetchAllPagesAsync<OuraActivityData>(url, cancellationToken);
    }

    public async Task<IEnumerable<OuraReadinessData>> GetDailyReadinessAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var url = $"usercollection/daily_readiness?start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}";
        return await FetchAllPagesAsync<OuraReadinessData>(url, cancellationToken);
    }

    /// <summary>
    /// Fetch all pages of data from a paginated Oura API endpoint.
    /// </summary>
    private async Task<List<T>> FetchAllPagesAsync<T>(
        string initialUrl,
        CancellationToken cancellationToken)
    {
        var allData = new List<T>();
        var url = initialUrl;

        while (!string.IsNullOrEmpty(url))
        {
            _logger.LogDebug("Fetching Oura API: {Url}", url);

            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "Oura API error: {StatusCode} - {Content}",
                        response.StatusCode,
                        errorContent);
                    response.EnsureSuccessStatusCode();
                }

                var apiResponse = await response.Content.ReadFromJsonAsync<OuraApiResponse<T>>(
                    JsonOptions,
                    cancellationToken);

                if (apiResponse?.Data != null)
                {
                    allData.AddRange(apiResponse.Data);
                    _logger.LogDebug("Fetched {Count} records", apiResponse.Data.Count);
                }

                // Handle pagination
                url = apiResponse?.NextToken != null
                    ? $"{initialUrl.Split('?')[0]}?next_token={apiResponse.NextToken}"
                    : null;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error fetching Oura data from {Url}", url);
                throw;
            }
        }

        _logger.LogInformation("Total records fetched: {Count}", allData.Count);
        return allData;
    }
}
