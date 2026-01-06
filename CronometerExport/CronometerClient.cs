using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace CronometerExport;

public class CronometerClient : IDisposable
{
    private const string HtmlLoginUrl = "https://cronometer.com/login/";
    private const string ApiLoginUrl = "https://cronometer.com/login";
    private const string GwtBaseUrl = "https://cronometer.com/cronometer/app";
    private const string ApiExportUrl = "https://cronometer.com/export";

    // GWT Magic Values (these may need updating if Cronometer updates their app)
    private const string GwtContentType = "text/x-gwt-rpc; charset=UTF-8";
    private const string GwtModuleBase = "https://cronometer.com/cronometer/";
    private const string GwtPermutation = "7B121DC5483BF272B1BC1916DA9FA963";
    private const string GwtHeader = "2D6A926E3729946302DC68073CB0D550";

    private readonly HttpClient _httpClient;
    private readonly CookieContainer _cookies;
    private string? _nonce;
    private string? _userId;

    public CronometerClient()
    {
        _cookies = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookies,
            UseCookies = true
        };
        _httpClient = new HttpClient(handler);
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        // Step 1: Get anti-CSRF token from login page
        var antiCsrf = await GetAntiCsrfTokenAsync();
        if (string.IsNullOrEmpty(antiCsrf))
        {
            throw new Exception("Failed to obtain anti-CSRF token");
        }

        // Step 2: Login with credentials
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("anticsrf", antiCsrf),
            new KeyValuePair<string, string>("username", username),
            new KeyValuePair<string, string>("password", password)
        });

        var response = await _httpClient.PostAsync(ApiLoginUrl, formData);
        var content = await response.Content.ReadAsStringAsync();

        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        // Login is successful if we get a redirect OR success=true
        bool isSuccess = loginResponse?.Success == true || !string.IsNullOrEmpty(loginResponse?.Redirect);
        if (!isSuccess)
        {
            throw new Exception($"Login failed: {loginResponse?.Error ?? "Unknown error"}");
        }

        // Update nonce from cookies
        UpdateNonce();

        // Step 3: Authenticate with GWT
        await GwtAuthenticateAsync();

        return true;
    }

    private async Task<string?> GetAntiCsrfTokenAsync()
    {
        var response = await _httpClient.GetStringAsync(HtmlLoginUrl);
        
        var doc = new HtmlDocument();
        doc.LoadHtml(response);

        var csrfInput = doc.DocumentNode.SelectSingleNode("//input[@name='anticsrf']");
        return csrfInput?.GetAttributeValue("value", null);
    }

    private void UpdateNonce()
    {
        var cookies = _cookies.GetCookies(new Uri("https://cronometer.com"));
        var nonceCookie = cookies["sesnonce"];
        if (nonceCookie != null)
        {
            _nonce = nonceCookie.Value;
        }
    }

    private async Task GwtAuthenticateAsync()
    {
        var requestBody = $"7|0|5|https://cronometer.com/cronometer/|{GwtHeader}|com.cronometer.shared.rpc.CronometerService|authenticate|java.lang.Integer/3438268394|1|2|3|4|1|5|5|-300|";

        var request = CreateGwtRequest(requestBody);
        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        UpdateNonce();

        // Parse user ID from response
        var match = Regex.Match(content, @"OK\[(\d+),");
        if (match.Success)
        {
            _userId = match.Groups[1].Value;
        }
        else
        {
            throw new Exception("Failed to authenticate with GWT API");
        }
    }

    private async Task<string> GenerateAuthTokenAsync()
    {
        var requestBody = $"7|0|8|https://cronometer.com/cronometer/|{GwtHeader}|com.cronometer.shared.rpc.CronometerService|generateAuthorizationToken|java.lang.String/2004016611|I|com.cronometer.shared.user.AuthScope/2065601159|{_nonce}|1|2|3|4|4|5|6|6|7|8|{_userId}|3600|7|2|";

        var request = CreateGwtRequest(requestBody);
        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        var match = Regex.Match(content, "\"(.*)\"");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        throw new Exception("Failed to generate auth token");
    }

    private HttpRequestMessage CreateGwtRequest(string body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, GwtBaseUrl);
        request.Content = new StringContent(body, System.Text.Encoding.UTF8);
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/x-gwt-rpc")
        {
            CharSet = "UTF-8"
        };
        request.Headers.Add("X-GWT-Module-Base", GwtModuleBase);
        request.Headers.Add("X-GWT-Permutation", GwtPermutation);
        return request;
    }

    public async Task<string> ExportDataAsync(string exportType, DateTime startDate, DateTime endDate)
    {
        var token = await GenerateAuthTokenAsync();

        var url = $"{ApiExportUrl}?nonce={token}&generate={exportType}&start={startDate:yyyy-MM-dd}&end={endDate:yyyy-MM-dd}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("sec-fetch-dest", "document");
        request.Headers.Add("sec-fetch-mode", "navigate");
        request.Headers.Add("sec-fetch-site", "same-origin");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    public Task<string> ExportDailyNutritionAsync(DateTime startDate, DateTime endDate) 
        => ExportDataAsync("dailySummary", startDate, endDate);

    public Task<string> ExportServingsAsync(DateTime startDate, DateTime endDate) 
        => ExportDataAsync("servings", startDate, endDate);

    public Task<string> ExportExercisesAsync(DateTime startDate, DateTime endDate) 
        => ExportDataAsync("exercises", startDate, endDate);

    public Task<string> ExportBiometricsAsync(DateTime startDate, DateTime endDate) 
        => ExportDataAsync("biometrics", startDate, endDate);

    public Task<string> ExportNotesAsync(DateTime startDate, DateTime endDate) 
        => ExportDataAsync("notes", startDate, endDate);

    public async Task LogoutAsync()
    {
        var requestBody = $"7|0|6|https://cronometer.com/cronometer/|{GwtHeader}|com.cronometer.shared.rpc.CronometerService|logout|java.lang.String/2004016611|{_nonce}|1|2|3|4|1|5|6|";

        var request = CreateGwtRequest(requestBody);
        await _httpClient.SendAsync(request);

        _nonce = null;
        _userId = null;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private class LoginResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? Redirect { get; set; }
    }
}
