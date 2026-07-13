namespace FoodLens.Api.DTOs
{
    /// <summary>
    /// Represents a single food search result from USDA.
    /// </summary>
    public class FoodSearchResultDto
    {
        public string FdcId { get; set; } = string.Empty;
        public string FoodName { get; set; } = string.Empty;
        public string? BrandOwner { get; set; }
        public double CaloriesPer100g { get; set; }
        public double ProteinPer100g { get; set; }
        public double CarbsPer100g { get; set; }
        public double FatPer100g { get; set; }
    }

    /// <summary>
    /// Returned after fetching nutrition for a specific food + portion.
    /// </summary>
    public class NutritionResultDto
    {
        public string FdcId { get; set; } = string.Empty;
        public string FoodName { get; set; } = string.Empty;
        public double Grams { get; set; }
        public double Calories { get; set; }
        public double ProteinG { get; set; }
        public double CarbsG { get; set; }
        public double FatG { get; set; }
    }
}
