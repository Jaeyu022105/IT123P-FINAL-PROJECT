using System.ComponentModel.DataAnnotations;

namespace FoodLens.Api.Models
{
    /// <summary>
    /// Caches USDA lookup results to avoid re-hitting the API for the same food.
    /// </summary>
    public class FoodCache
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FdcId { get; set; } = string.Empty; // USDA FoodData Central ID

        [Required]
        public string FoodName { get; set; } = string.Empty;

        // Nutrition per 100g (raw from USDA, scaled at query time)
        public double CaloriesPer100g { get; set; }
        public double ProteinPer100g { get; set; }
        public double CarbsPer100g { get; set; }
        public double FatPer100g { get; set; }

        public DateTime CachedAt { get; set; } = DateTime.UtcNow;
    }
}
