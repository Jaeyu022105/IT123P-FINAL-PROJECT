namespace IT123P_FINAL_PROJECT.Models
{
    public class FoodCandidate
    {
        public string Name { get; set; } = string.Empty;
        public double Confidence { get; set; } // e.g. 92.5

        /// <summary>USDA FoodData Central numeric food ID (returned by the backend search).</summary>
        public string FdcId { get; set; } = string.Empty;

        // Kept for backwards-compatibility with Phase 1 code that uses UsdaFoodId
        [System.Text.Json.Serialization.JsonIgnore]
        public string UsdaFoodId
        {
            get => FdcId;
            set => FdcId = value;
        }

        // Base nutrition per 100g — populated from USDA search results
        public double CaloriesPer100g { get; set; }
        public double ProteinPer100g { get; set; }
        public double CarbsPer100g { get; set; }
        public double FatPer100g { get; set; }

        public string ConfidenceText => $"{Confidence:F1}% Confidence";
    }
}
