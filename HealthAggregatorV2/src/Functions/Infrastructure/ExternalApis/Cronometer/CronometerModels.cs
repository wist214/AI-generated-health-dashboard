namespace HealthAggregatorV2.Functions.Infrastructure.ExternalApis.Cronometer;

/// <summary>
/// Daily nutrition data from Cronometer export.
/// </summary>
public class CronometerNutritionData
{
    public DateTime Date { get; set; }
    public decimal? Calories { get; set; }
    public decimal? Protein { get; set; }
    public decimal? Carbohydrates { get; set; }
    public decimal? Fat { get; set; }
    public decimal? Fiber { get; set; }
    public decimal? Sugar { get; set; }
    public decimal? Sodium { get; set; }
    public decimal? Cholesterol { get; set; }
    public decimal? SaturatedFat { get; set; }
    public decimal? TransFat { get; set; }
    public decimal? MonounsaturatedFat { get; set; }
    public decimal? PolyunsaturatedFat { get; set; }
    public decimal? Potassium { get; set; }
    public decimal? VitaminA { get; set; }
    public decimal? VitaminC { get; set; }
    public decimal? VitaminD { get; set; }
    public decimal? VitaminE { get; set; }
    public decimal? VitaminK { get; set; }
    public decimal? Calcium { get; set; }
    public decimal? Iron { get; set; }
    public decimal? Magnesium { get; set; }
    public decimal? Zinc { get; set; }
    public decimal? B12 { get; set; }
    public decimal? Omega3 { get; set; }
    public decimal? Omega6 { get; set; }
    public decimal? Water { get; set; }
    public decimal? Alcohol { get; set; }
    public decimal? Caffeine { get; set; }
}
