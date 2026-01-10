using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthAggregatorV2.Functions.Infrastructure.ExternalApis.Cronometer;

/// <summary>
/// Reads Cronometer data from CSV export files.
/// </summary>
public class CronometerDataReader : ICronometerDataReader
{
    private readonly CronometerApiOptions _options;
    private readonly ILogger<CronometerDataReader> _logger;

    private const string DailyNutritionFileName = "daily_nutrition.csv";

    public CronometerDataReader(
        IOptions<CronometerApiOptions> options,
        ILogger<CronometerDataReader> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IEnumerable<CronometerNutritionData>> GetNutritionDataAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_options.ExportsPath, DailyNutritionFileName);

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Cronometer export file not found: {FilePath}", filePath);
            return Enumerable.Empty<CronometerNutritionData>();
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);

            if (lines.Length < 2)
            {
                _logger.LogWarning("Cronometer export file is empty or has no data rows");
                return Enumerable.Empty<CronometerNutritionData>();
            }

            // Parse header to get column indices
            var header = ParseCsvLine(lines[0]);
            var columnIndices = BuildColumnIndices(header);

            var results = new List<CronometerNutritionData>();

            for (var i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                try
                {
                    var values = ParseCsvLine(lines[i]);
                    var nutrition = ParseNutritionRow(values, columnIndices);

                    if (nutrition != null && nutrition.Date >= startDate && nutrition.Date <= endDate)
                    {
                        results.Add(nutrition);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing line {LineNumber} in Cronometer export", i + 1);
                }
            }

            _logger.LogInformation("Read {Count} nutrition records from Cronometer export", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading Cronometer export file");
            throw;
        }
    }

    private Dictionary<string, int> BuildColumnIndices(string[] header)
    {
        var indices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < header.Length; i++)
        {
            var columnName = header[i].Trim().ToLowerInvariant();
            indices[columnName] = i;
        }

        return indices;
    }

    private CronometerNutritionData? ParseNutritionRow(string[] values, Dictionary<string, int> columns)
    {
        var dateStr = GetColumnValue(values, columns, "date");
        if (string.IsNullOrEmpty(dateStr) || !DateTime.TryParse(dateStr, out var date))
        {
            return null;
        }

        return new CronometerNutritionData
        {
            Date = date.Date,
            Calories = GetDecimalValue(values, columns, "energy (kcal)", "calories"),
            Protein = GetDecimalValue(values, columns, "protein (g)", "protein"),
            Carbohydrates = GetDecimalValue(values, columns, "carbs (g)", "carbohydrates"),
            Fat = GetDecimalValue(values, columns, "fat (g)", "fat"),
            Fiber = GetDecimalValue(values, columns, "fiber (g)", "fiber"),
            Sugar = GetDecimalValue(values, columns, "sugars (g)", "sugar"),
            Sodium = GetDecimalValue(values, columns, "sodium (mg)", "sodium"),
            Cholesterol = GetDecimalValue(values, columns, "cholesterol (mg)", "cholesterol"),
            SaturatedFat = GetDecimalValue(values, columns, "saturated (g)", "saturated fat"),
            TransFat = GetDecimalValue(values, columns, "trans-fats (g)", "trans fat"),
            MonounsaturatedFat = GetDecimalValue(values, columns, "monounsaturated (g)", "monounsaturated"),
            PolyunsaturatedFat = GetDecimalValue(values, columns, "polyunsaturated (g)", "polyunsaturated"),
            Potassium = GetDecimalValue(values, columns, "potassium (mg)", "potassium"),
            VitaminA = GetDecimalValue(values, columns, "vitamin a (iu)", "vitamin a"),
            VitaminC = GetDecimalValue(values, columns, "vitamin c (mg)", "vitamin c"),
            VitaminD = GetDecimalValue(values, columns, "vitamin d (iu)", "vitamin d"),
            VitaminE = GetDecimalValue(values, columns, "vitamin e (mg)", "vitamin e"),
            VitaminK = GetDecimalValue(values, columns, "vitamin k (µg)", "vitamin k"),
            Calcium = GetDecimalValue(values, columns, "calcium (mg)", "calcium"),
            Iron = GetDecimalValue(values, columns, "iron (mg)", "iron"),
            Magnesium = GetDecimalValue(values, columns, "magnesium (mg)", "magnesium"),
            Zinc = GetDecimalValue(values, columns, "zinc (mg)", "zinc"),
            B12 = GetDecimalValue(values, columns, "b12 (µg)", "vitamin b12"),
            Omega3 = GetDecimalValue(values, columns, "omega-3 (g)", "omega3"),
            Omega6 = GetDecimalValue(values, columns, "omega-6 (g)", "omega6"),
            Water = GetDecimalValue(values, columns, "water (g)", "water"),
            Alcohol = GetDecimalValue(values, columns, "alcohol (g)", "alcohol"),
            Caffeine = GetDecimalValue(values, columns, "caffeine (mg)", "caffeine")
        };
    }

    private static string? GetColumnValue(string[] values, Dictionary<string, int> columns, string columnName)
    {
        if (columns.TryGetValue(columnName, out var index) && index < values.Length)
        {
            return values[index].Trim();
        }
        return null;
    }

    private static decimal? GetDecimalValue(string[] values, Dictionary<string, int> columns, params string[] columnNames)
    {
        foreach (var columnName in columnNames)
        {
            var value = GetColumnValue(values, columns, columnName);
            if (!string.IsNullOrEmpty(value) && decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
        }
        return null;
    }

    private static string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var inQuotes = false;
        var currentValue = string.Empty;

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(currentValue);
                currentValue = string.Empty;
            }
            else
            {
                currentValue += c;
            }
        }

        values.Add(currentValue);
        return values.ToArray();
    }
}
