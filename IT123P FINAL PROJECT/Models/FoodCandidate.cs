namespace IT123P_FINAL_PROJECT.Models
{
    public class FoodCandidate
    {
        public string Name { get; set; } = string.Empty;
        public double Confidence { get; set; } // e.g. 92.5
        public string UsdaFoodId { get; set; } = string.Empty;

        // Base nutrition details per 100g
        public double CaloriesPer100g { get; set; }
        public double ProteinPer100g { get; set; }
        public double CarbsPer100g { get; set; }
        public double FatPer100g { get; set; }

        public string ConfidenceText => $"{Confidence:F1}% Confidence";
    }
}
