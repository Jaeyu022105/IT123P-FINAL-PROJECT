namespace IT123P_FINAL_PROJECT.Models
{
    // Daily summary totals
    public record DayNutritionTotal(
        DateTime Date,
        double Calories,
        double Protein,
        double Carbs,
        double Fat,
        int MealCount)
    {
        public string DayLabel => Date.ToString("ddd");          // Mon, Tue, etc.
        public string DateLabel => Date.ToString("MMM d");       // Jul 14 format
        public bool IsToday => Date.Date == DateTime.Today;
    }

    // 7-day window details
    public class WeeklyNutritionSummary
    {
        public List<DayNutritionTotal> Days { get; }

        public WeeklyNutritionSummary(List<DayNutritionTotal> days)
        {
            Days = days;
        }

        public double MaxCalories => Days.Count > 0 ? Days.Max(d => d.Calories) : 1;
        public double TotalCalories => Days.Sum(d => d.Calories);
        public double TotalProtein  => Days.Sum(d => d.Protein);
        public double TotalCarbs    => Days.Sum(d => d.Carbs);
        public double TotalFat      => Days.Sum(d => d.Fat);

        public double AverageCalories
        {
            get
            {
                var activeDays = Days.Where(d => d.Calories > 0).ToList();
                return activeDays.Count > 0 ? activeDays.Average(d => d.Calories) : 0;
            }
        }

        public DayNutritionTotal? BestDay  => Days.Where(d => d.Calories > 0).MaxBy(d => d.Calories);
        public DayNutritionTotal? WorstDay => Days.Where(d => d.Calories > 0).MinBy(d => d.Calories);

        // Height range from 0 to 1 for rendering the bar chart
        public double NormalizedHeight(DayNutritionTotal day)
            => MaxCalories > 0 ? Math.Clamp(day.Calories / MaxCalories, 0, 1) : 0;

        // Macro split fractions from 0 to 1
        public (double ProteinFrac, double CarbsFrac, double FatFrac) MacroSplit()
        {
            double totalMacroKcal = TotalProtein * 4 + TotalCarbs * 4 + TotalFat * 9;
            if (totalMacroKcal <= 0) return (0.33, 0.34, 0.33);
            return (
                TotalProtein * 4 / totalMacroKcal,
                TotalCarbs   * 4 / totalMacroKcal,
                TotalFat     * 9 / totalMacroKcal
            );
        }
    }
}
