using System.ComponentModel.DataAnnotations;

namespace FoodLens.Api.Models
{
    public class DietGoal
    {
        [Key]
        public int Id { get; set; } = 1; // single global goal per device in v1

        public string DeviceId { get; set; } = "default";

        public double DailyCalorieLimit { get; set; } = 2000;
        public double ProteinPercentage { get; set; } = 30;
        public double CarbsPercentage { get; set; } = 40;
        public double FatPercentage { get; set; } = 30;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
