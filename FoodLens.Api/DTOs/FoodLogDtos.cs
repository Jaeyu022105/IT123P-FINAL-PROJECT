using System.ComponentModel.DataAnnotations;

namespace FoodLens.Api.DTOs
{
    public class FoodLogCreateDto
    {
        [Required]
        public string FoodName { get; set; } = string.Empty;
        public string? FdcId { get; set; }
        public double Calories { get; set; }
        public double ProteinG { get; set; }
        public double CarbsG { get; set; }
        public double FatG { get; set; }
        public double Grams { get; set; }
        public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
    }

    public class FoodLogUpdateDto
    {
        public double? Grams { get; set; }
        public double? Calories { get; set; }
        public double? ProteinG { get; set; }
        public double? CarbsG { get; set; }
        public double? FatG { get; set; }
    }

    public class FoodLogResponseDto
    {
        public int Id { get; set; }
        public string FoodName { get; set; } = string.Empty;
        public string? FdcId { get; set; }
        public double Calories { get; set; }
        public double ProteinG { get; set; }
        public double CarbsG { get; set; }
        public double FatG { get; set; }
        public double Grams { get; set; }
        public DateTime LoggedAt { get; set; }
    }
}
