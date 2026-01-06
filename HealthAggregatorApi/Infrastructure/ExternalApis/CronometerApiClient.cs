using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using HealthAggregatorApi.Core.Interfaces;
using HealthAggregatorApi.Core.Models.Cronometer;

namespace HealthAggregatorApi.Infrastructure.ExternalApis;

/// <summary>
/// Cronometer API client for exporting nutrition data.
/// Based on CronometerExport project implementation.
/// </summary>
public class CronometerApiClient : ICronometerApiClient, IDisposable
{
    private const string HtmlLoginUrl = "https://cronometer.com/login/";
    private const string ApiLoginUrl = "https://cronometer.com/login";
    private const string GwtBaseUrl = "https://cronometer.com/cronometer/app";
    private const string ApiExportUrl = "https://cronometer.com/export";

    // GWT Magic Values
    private const string GwtModuleBase = "https://cronometer.com/cronometer/";
    private const string GwtPermutation = "7B121DC5483BF272B1BC1916DA9FA963";
    private const string GwtHeader = "2D6A926E3729946302DC68073CB0D550";

    private readonly HttpClient _httpClient;
    private readonly CookieContainer _cookies;
    private readonly ILogger<CronometerApiClient> _logger;
    private string? _nonce;
    private string? _userId;

    public CronometerApiClient(ILogger<CronometerApiClient> logger)
    {
        _logger = logger;
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
        try
        {
            _logger.LogInformation("Attempting Cronometer login for {Username}", username);

            // Step 1: Get anti-CSRF token from login page
            var antiCsrf = await GetAntiCsrfTokenAsync();
            if (string.IsNullOrEmpty(antiCsrf))
            {
                _logger.LogError("Failed to obtain anti-CSRF token");
                return false;
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

            bool isSuccess = loginResponse?.Success == true || !string.IsNullOrEmpty(loginResponse?.Redirect);
            if (!isSuccess)
            {
                _logger.LogError("Login failed: {Error}", loginResponse?.Error ?? "Unknown error");
                return false;
            }

            // Update nonce from cookies
            UpdateNonce();

            // Step 3: Authenticate with GWT
            await GwtAuthenticateAsync();

            _logger.LogInformation("Cronometer login successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cronometer login failed");
            return false;
        }
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

    private async Task<string> ExportDataAsync(string exportType, DateTime startDate, DateTime endDate)
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

    public async Task<List<DailyNutrition>> ExportDailyNutritionAsync(DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("Exporting daily nutrition from {Start} to {End}", startDate, endDate);
        var csv = await ExportDataAsync("dailySummary", startDate, endDate);
        return ParseDailyNutritionCsv(csv);
    }

    public async Task<List<FoodServing>> ExportServingsAsync(DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("Exporting servings from {Start} to {End}", startDate, endDate);
        var csv = await ExportDataAsync("servings", startDate, endDate);
        return ParseServingsCsv(csv);
    }

    public async Task<List<ExerciseEntry>> ExportExercisesAsync(DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("Exporting exercises from {Start} to {End}", startDate, endDate);
        var csv = await ExportDataAsync("exercises", startDate, endDate);
        return ParseExercisesCsv(csv);
    }

    public async Task<List<BiometricEntry>> ExportBiometricsAsync(DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("Exporting biometrics from {Start} to {End}", startDate, endDate);
        var csv = await ExportDataAsync("biometrics", startDate, endDate);
        return ParseBiometricsCsv(csv);
    }

    public async Task<List<NoteEntry>> ExportNotesAsync(DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("Exporting notes from {Start} to {End}", startDate, endDate);
        var csv = await ExportDataAsync("notes", startDate, endDate);
        return ParseNotesCsv(csv);
    }

    public async Task LogoutAsync()
    {
        try
        {
            var requestBody = $"7|0|6|https://cronometer.com/cronometer/|{GwtHeader}|com.cronometer.shared.rpc.CronometerService|logout|java.lang.String/2004016611|{_nonce}|1|2|3|4|1|5|6|";
            var request = CreateGwtRequest(requestBody);
            await _httpClient.SendAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Logout failed");
        }
        finally
        {
            _nonce = null;
            _userId = null;
        }
    }

    #region CSV Parsing

    private List<DailyNutrition> ParseDailyNutritionCsv(string csv)
    {
        var result = new List<DailyNutrition>();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length < 2) return result;
        
        var headers = ParseCsvLine(lines[0]);
        var headerIndex = CreateHeaderIndex(headers);

        for (int i = 1; i < lines.Length; i++)
        {
            var values = ParseCsvLine(lines[i]);
            if (values.Length == 0) continue;

            var nutrition = new DailyNutrition
            {
                Date = GetValue(values, headerIndex, "Date"),
                Completed = GetValue(values, headerIndex, "Completed")?.ToLower() == "true",
                Energy = GetDoubleValue(values, headerIndex, "Energy (kcal)"),
                Carbs = GetDoubleValue(values, headerIndex, "Carbs (g)"),
                Fiber = GetDoubleValue(values, headerIndex, "Fiber (g)"),
                Starch = GetDoubleValue(values, headerIndex, "Starch (g)"),
                Sugars = GetDoubleValue(values, headerIndex, "Sugars (g)"),
                AddedSugars = GetDoubleValue(values, headerIndex, "Added Sugars (g)"),
                NetCarbs = GetDoubleValue(values, headerIndex, "Net Carbs (g)"),
                Fat = GetDoubleValue(values, headerIndex, "Fat (g)"),
                Protein = GetDoubleValue(values, headerIndex, "Protein (g)"),
                Cholesterol = GetDoubleValue(values, headerIndex, "Cholesterol (mg)"),
                Monounsaturated = GetDoubleValue(values, headerIndex, "Monounsaturated (g)"),
                Polyunsaturated = GetDoubleValue(values, headerIndex, "Polyunsaturated (g)"),
                Saturated = GetDoubleValue(values, headerIndex, "Saturated (g)"),
                TransFats = GetDoubleValue(values, headerIndex, "Trans-Fats (g)"),
                Omega3 = GetDoubleValue(values, headerIndex, "Omega-3 (g)"),
                Omega6 = GetDoubleValue(values, headerIndex, "Omega-6 (g)"),
                VitaminA = GetDoubleValue(values, headerIndex, "Vitamin A (µg)"),
                VitaminC = GetDoubleValue(values, headerIndex, "Vitamin C (mg)"),
                VitaminD = GetDoubleValue(values, headerIndex, "Vitamin D (IU)"),
                VitaminE = GetDoubleValue(values, headerIndex, "Vitamin E (mg)"),
                VitaminK = GetDoubleValue(values, headerIndex, "Vitamin K (µg)"),
                B1Thiamine = GetDoubleValue(values, headerIndex, "B1 (Thiamine) (mg)"),
                B2Riboflavin = GetDoubleValue(values, headerIndex, "B2 (Riboflavin) (mg)"),
                B3Niacin = GetDoubleValue(values, headerIndex, "B3 (Niacin) (mg)"),
                B5PantothenicAcid = GetDoubleValue(values, headerIndex, "B5 (Pantothenic Acid) (mg)"),
                B6Pyridoxine = GetDoubleValue(values, headerIndex, "B6 (Pyridoxine) (mg)"),
                B12Cobalamin = GetDoubleValue(values, headerIndex, "B12 (Cobalamin) (µg)"),
                Folate = GetDoubleValue(values, headerIndex, "Folate (µg)"),
                Calcium = GetDoubleValue(values, headerIndex, "Calcium (mg)"),
                Copper = GetDoubleValue(values, headerIndex, "Copper (mg)"),
                Iron = GetDoubleValue(values, headerIndex, "Iron (mg)"),
                Magnesium = GetDoubleValue(values, headerIndex, "Magnesium (mg)"),
                Manganese = GetDoubleValue(values, headerIndex, "Manganese (mg)"),
                Phosphorus = GetDoubleValue(values, headerIndex, "Phosphorus (mg)"),
                Potassium = GetDoubleValue(values, headerIndex, "Potassium (mg)"),
                Selenium = GetDoubleValue(values, headerIndex, "Selenium (µg)"),
                Sodium = GetDoubleValue(values, headerIndex, "Sodium (mg)"),
                Zinc = GetDoubleValue(values, headerIndex, "Zinc (mg)"),
                Alcohol = GetDoubleValue(values, headerIndex, "Alcohol (g)"),
                Caffeine = GetDoubleValue(values, headerIndex, "Caffeine (mg)"),
                Water = GetDoubleValue(values, headerIndex, "Water (g)")
            };

            // Only add if there's actually data (has a date)
            if (!string.IsNullOrEmpty(nutrition.Date))
            {
                result.Add(nutrition);
            }
        }

        return result;
    }

    private List<FoodServing> ParseServingsCsv(string csv)
    {
        var result = new List<FoodServing>();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length < 2) return result;
        
        var headers = ParseCsvLine(lines[0]);
        var headerIndex = CreateHeaderIndex(headers);

        for (int i = 1; i < lines.Length; i++)
        {
            var values = ParseCsvLine(lines[i]);
            if (values.Length == 0) continue;

            var serving = new FoodServing
            {
                Day = GetValue(values, headerIndex, "Day"),
                Group = GetValue(values, headerIndex, "Group"),
                FoodName = GetValue(values, headerIndex, "Food Name"),
                Amount = GetValue(values, headerIndex, "Amount"),
                Category = GetValue(values, headerIndex, "Category"),
                Energy = GetDoubleValue(values, headerIndex, "Energy (kcal)"),
                Carbs = GetDoubleValue(values, headerIndex, "Carbs (g)"),
                Fat = GetDoubleValue(values, headerIndex, "Fat (g)"),
                Protein = GetDoubleValue(values, headerIndex, "Protein (g)"),
                Fiber = GetDoubleValue(values, headerIndex, "Fiber (g)"),
                Sugars = GetDoubleValue(values, headerIndex, "Sugars (g)"),
                Sodium = GetDoubleValue(values, headerIndex, "Sodium (mg)")
            };

            if (!string.IsNullOrEmpty(serving.Day))
            {
                result.Add(serving);
            }
        }

        return result;
    }

    private List<ExerciseEntry> ParseExercisesCsv(string csv)
    {
        var result = new List<ExerciseEntry>();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length < 2) return result;
        
        var headers = ParseCsvLine(lines[0]);
        var headerIndex = CreateHeaderIndex(headers);

        for (int i = 1; i < lines.Length; i++)
        {
            var values = ParseCsvLine(lines[i]);
            if (values.Length == 0) continue;

            var exercise = new ExerciseEntry
            {
                Day = GetValue(values, headerIndex, "Day"),
                Group = GetValue(values, headerIndex, "Group"),
                Exercise = GetValue(values, headerIndex, "Exercise"),
                Minutes = GetDoubleValue(values, headerIndex, "Minutes"),
                CaloriesBurned = GetDoubleValue(values, headerIndex, "Calories Burned")
            };

            if (!string.IsNullOrEmpty(exercise.Day))
            {
                result.Add(exercise);
            }
        }

        return result;
    }

    private List<BiometricEntry> ParseBiometricsCsv(string csv)
    {
        var result = new List<BiometricEntry>();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length < 2) return result;
        
        var headers = ParseCsvLine(lines[0]);
        var headerIndex = CreateHeaderIndex(headers);

        for (int i = 1; i < lines.Length; i++)
        {
            var values = ParseCsvLine(lines[i]);
            if (values.Length == 0) continue;

            var biometric = new BiometricEntry
            {
                Day = GetValue(values, headerIndex, "Day"),
                Group = GetValue(values, headerIndex, "Group"),
                Metric = GetValue(values, headerIndex, "Metric"),
                Unit = GetValue(values, headerIndex, "Unit"),
                Amount = GetDoubleValue(values, headerIndex, "Amount")
            };

            if (!string.IsNullOrEmpty(biometric.Day))
            {
                result.Add(biometric);
            }
        }

        return result;
    }

    private List<NoteEntry> ParseNotesCsv(string csv)
    {
        var result = new List<NoteEntry>();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length < 2) return result;
        
        var headers = ParseCsvLine(lines[0]);
        var headerIndex = CreateHeaderIndex(headers);

        for (int i = 1; i < lines.Length; i++)
        {
            var values = ParseCsvLine(lines[i]);
            if (values.Length == 0) continue;

            var note = new NoteEntry
            {
                Day = GetValue(values, headerIndex, "Day"),
                Group = GetValue(values, headerIndex, "Group"),
                Note = GetValue(values, headerIndex, "Note")
            };

            if (!string.IsNullOrEmpty(note.Day))
            {
                result.Add(note);
            }
        }

        return result;
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var currentValue = "";

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentValue.Trim());
                currentValue = "";
            }
            else
            {
                currentValue += c;
            }
        }

        result.Add(currentValue.Trim());
        return result.ToArray();
    }

    private static Dictionary<string, int> CreateHeaderIndex(string[] headers)
    {
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++)
        {
            var header = headers[i].Trim().Trim('"');
            if (!index.ContainsKey(header))
            {
                index[header] = i;
            }
        }
        return index;
    }

    private static string GetValue(string[] values, Dictionary<string, int> headerIndex, string header)
    {
        if (headerIndex.TryGetValue(header, out int index) && index < values.Length)
        {
            return values[index].Trim().Trim('"');
        }
        return string.Empty;
    }

    private static double? GetDoubleValue(string[] values, Dictionary<string, int> headerIndex, string header)
    {
        var strValue = GetValue(values, headerIndex, header);
        if (string.IsNullOrEmpty(strValue)) return null;
        if (double.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
        {
            return result;
        }
        return null;
    }

    #endregion

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
