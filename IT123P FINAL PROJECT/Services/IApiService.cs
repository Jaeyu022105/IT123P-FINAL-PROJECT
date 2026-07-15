using IT123P_FINAL_PROJECT.Models;

namespace IT123P_FINAL_PROJECT.Services
{
    public interface IApiService
    {
        // ── Nutrition ──────────────────────────────────────────────────────────
        /// <summary>Search USDA food database via the backend. Returns empty list on failure.</summary>
        Task<List<FoodCandidate>> SearchFoodsAsync(string query, int limit = 10);

        /// <summary>Get scaled nutrition for a food. Returns null if backend unavailable.</summary>
        Task<(double Calories, double ProteinG, double CarbsG, double FatG)?> GetNutritionAsync(string fdcId, double grams);

        /// <summary>Classifies food items inside a local image file. Returns empty list on failure.</summary>
        Task<List<FoodCandidate>> ClassifyImageAsync(string imagePath);

        // ── Diet Goal ─────────────────────────────────────────────────────────
        /// <summary>Fetch the user's diet goal from the backend. Returns null if unavailable.</summary>
        Task<DietGoal?> GetDietGoalAsync();

        /// <summary>Push a diet goal to the backend. Returns true on success.</summary>
        Task<bool> SaveDietGoalAsync(DietGoal goal);

        // ── Food Logs ─────────────────────────────────────────────────────────
        /// <summary>Fetch all food log entries for a given date from the backend.</summary>
        Task<List<FoodLogEntry>> GetFoodLogsAsync(DateTime date);

        /// <summary>Create a new food log entry on the backend. Returns the server-assigned ID, or -1 on failure.</summary>
        Task<int> CreateFoodLogAsync(FoodLogEntry entry);

        /// <summary>Delete a food log entry by its server ID. Returns true on success.</summary>
        Task<bool> DeleteFoodLogAsync(int serverId);

        // ── Diet Comparison ───────────────────────────────────────────────────
        /// <summary>Ask the backend to compare a proposed meal against today's budget. Returns null if unavailable.</summary>
        Task<DietCompareResult?> CompareDietAsync(double proposedCalories, double todayLogged);

        // ── SOAP Export (Phase 5) ─────────────────────────────────────────────
        /// <summary>Request the backend to trigger a SOAP legacy export. Returns raw XML payload on success, null on failure.</summary>
        Task<string?> ExportDietLogAsync(DateTime from, DateTime to);
    }

    /// <summary>Client-side mirror of the backend DietCompareResultDto.</summary>
    public class DietCompareResult
    {
        public double DailyLimit { get; set; }
        public double TodayLogged { get; set; }
        public double ProposedCalories { get; set; }
        public double ProjectedTotal { get; set; }
        public double RemainingAfter { get; set; }
        public bool FitsDiet { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
