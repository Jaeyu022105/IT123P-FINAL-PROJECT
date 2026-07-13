using System.ComponentModel.DataAnnotations;

namespace FoodLens.Api.DTOs
{
    public class DietGoalDto
    {
        [Range(100, 10000, ErrorMessage = "Daily calorie limit must be between 100 and 10000.")]
        public double DailyCalorieLimit { get; set; } = 2000;

        [Range(0, 100)]
        public double ProteinPercentage { get; set; } = 30;

        [Range(0, 100)]
        public double CarbsPercentage { get; set; } = 40;

        [Range(0, 100)]
        public double FatPercentage { get; set; } = 30;
    }

    public class DietCompareRequestDto
    {
        public double ProposedCalories { get; set; }
        public double TodayLoggedCalories { get; set; }
    }

    public class DietCompareResultDto
    {
        public double DailyLimit { get; set; }
        public double TodayLogged { get; set; }
        public double ProposedCalories { get; set; }
        public double ProjectedTotal { get; set; }
        public double RemainingAfter { get; set; }
        public bool FitsDiet { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ErrorDto
    {
        public ErrorDetail Error { get; set; } = new();
    }

    public class ErrorDetail
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
