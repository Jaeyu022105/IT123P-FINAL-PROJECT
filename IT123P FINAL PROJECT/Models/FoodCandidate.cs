namespace IT123P_FINAL_PROJECT.Models
{
    public class FoodCandidate
    {
        public string Name { get; set; } = string.Empty;
        public double Confidence { get; set; } // e.g. 92.5

        // USDA FoodData Central food ID
        public string FdcId { get; set; } = string.Empty;

        // Keep for backwards compatibility
        [System.Text.Json.Serialization.JsonIgnore]
        public string UsdaFoodId
        {
            get => FdcId;
            set => FdcId = value;
        }

        // Base nutrition values per 100g from USDA results
        public double CaloriesPer100g { get; set; }
        public double ProteinPer100g { get; set; }
        public double CarbsPer100g { get; set; }
        public double FatPer100g { get; set; }

        public string ConfidenceText => $"{Confidence:F1}% Confidence";
    }
}
