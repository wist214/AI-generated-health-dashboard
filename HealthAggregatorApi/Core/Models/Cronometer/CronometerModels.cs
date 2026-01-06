namespace HealthAggregatorApi.Core.Models.Cronometer;

/// <summary>
/// Container for all Cronometer data cached in storage.
/// </summary>
public class CronometerData
{
    public List<DailyNutrition> DailyNutrition { get; set; } = [];
    public List<FoodServing> FoodServings { get; set; } = [];
    public List<ExerciseEntry> Exercises { get; set; } = [];
    public List<BiometricEntry> Biometrics { get; set; } = [];
    public List<NoteEntry> Notes { get; set; } = [];
    public DateTime? LastSync { get; set; }
}

/// <summary>
/// Daily nutrition summary with all macro and micro nutrients.
/// </summary>
public class DailyNutrition
{
    public string Date { get; set; } = string.Empty;
    public bool Completed { get; set; }
    
    // Energy
    public double? Energy { get; set; }  // kcal
    
    // Macronutrients
    public double? Carbs { get; set; }  // g
    public double? Fiber { get; set; }  // g
    public double? Starch { get; set; }  // g
    public double? Sugars { get; set; }  // g
    public double? AddedSugars { get; set; }  // g
    public double? NetCarbs { get; set; }  // g
    public double? Fat { get; set; }  // g
    public double? Protein { get; set; }  // g
    
    // Fat breakdown
    public double? Cholesterol { get; set; }  // mg
    public double? Monounsaturated { get; set; }  // g
    public double? Polyunsaturated { get; set; }  // g
    public double? Saturated { get; set; }  // g
    public double? TransFats { get; set; }  // g
    public double? Omega3 { get; set; }  // g
    public double? Omega6 { get; set; }  // g
    
    // Vitamins
    public double? VitaminA { get; set; }  // µg
    public double? VitaminC { get; set; }  // mg
    public double? VitaminD { get; set; }  // IU
    public double? VitaminE { get; set; }  // mg
    public double? VitaminK { get; set; }  // µg
    public double? B1Thiamine { get; set; }  // mg
    public double? B2Riboflavin { get; set; }  // mg
    public double? B3Niacin { get; set; }  // mg
    public double? B5PantothenicAcid { get; set; }  // mg
    public double? B6Pyridoxine { get; set; }  // mg
    public double? B12Cobalamin { get; set; }  // µg
    public double? Folate { get; set; }  // µg
    
    // Minerals
    public double? Calcium { get; set; }  // mg
    public double? Copper { get; set; }  // mg
    public double? Iron { get; set; }  // mg
    public double? Magnesium { get; set; }  // mg
    public double? Manganese { get; set; }  // mg
    public double? Phosphorus { get; set; }  // mg
    public double? Potassium { get; set; }  // mg
    public double? Selenium { get; set; }  // µg
    public double? Sodium { get; set; }  // mg
    public double? Zinc { get; set; }  // mg
    
    // Other
    public double? Alcohol { get; set; }  // g
    public double? Caffeine { get; set; }  // mg
    public double? Water { get; set; }  // g
}

/// <summary>
/// Individual food serving entry from Cronometer.
/// </summary>
public class FoodServing
{
    public string Day { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string FoodName { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    
    // Core nutrition
    public double? Energy { get; set; }  // kcal
    public double? Carbs { get; set; }  // g
    public double? Fat { get; set; }  // g
    public double? Protein { get; set; }  // g
    public double? Fiber { get; set; }  // g
    public double? Sugars { get; set; }  // g
    public double? Sodium { get; set; }  // mg
}

/// <summary>
/// Exercise entry from Cronometer.
/// </summary>
public class ExerciseEntry
{
    public string Day { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Exercise { get; set; } = string.Empty;
    public double? Minutes { get; set; }
    public double? CaloriesBurned { get; set; }
}

/// <summary>
/// Biometric entry from Cronometer (weight, measurements, etc).
/// </summary>
public class BiometricEntry
{
    public string Day { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public double? Amount { get; set; }
}

/// <summary>
/// Daily notes from Cronometer.
/// </summary>
public class NoteEntry
{
    public string Day { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}
