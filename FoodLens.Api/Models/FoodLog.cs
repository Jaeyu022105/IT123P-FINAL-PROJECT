using System.ComponentModel.DataAnnotations;

namespace FoodLens.Api.Models
{
    public class FoodLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string DeviceId { get; set; } = "default"; // lightweight device-based identity (no full auth in v1)

        [Required]
        public string FoodName { get; set; } = string.Empty;

        public string? FdcId { get; set; } // USDA FoodData Central ID

        public double Calories { get; set; }
        public double ProteinG { get; set; }
        public double CarbsG { get; set; }
        public double FatG { get; set; }
        public double Grams { get; set; }

        public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
    }
}
