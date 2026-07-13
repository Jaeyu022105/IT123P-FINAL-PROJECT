using SQLite;

namespace IT123P_FINAL_PROJECT.Models
{
    public class DietGoal
    {
        [PrimaryKey]
        public int Id { get; set; } // We only need 1 active goal (e.g. Id = 1)

        public double DailyCalorieLimit { get; set; }

        public double ProteinPercentage { get; set; }

        public double CarbsPercentage { get; set; }

        public double FatPercentage { get; set; }
    }
}
