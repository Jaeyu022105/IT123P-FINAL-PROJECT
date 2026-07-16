using IT123P_FINAL_PROJECT.Models;

namespace IT123P_FINAL_PROJECT.Services
{
    public interface IApiService
    {
        // Search USDA food database via the backend. Returns empty list on failure.
        Task<List<FoodCandidate>> SearchFoodsAsync(string query, int limit = 10);

        // Get scaled nutrition for a food. Returns null if backend unavailable.
        Task<(double Calories, double ProteinG, double CarbsG, double FatG)?> GetNutritionAsync(string fdcId, double grams);

        // Classifies food items inside a local image file. Returns empty list on failure.
        Task<List<FoodCandidate>> ClassifyImageAsync(string imagePath);

        // Fetch the user's diet goal from the backend. Returns null if unavailable.
        Task<DietGoal?> GetDietGoalAsync();

        // Push a diet goal to the backend. Returns true on success.
        Task<bool> SaveDietGoalAsync(DietGoal goal);

        // Fetch all food log entries for a given date from the backend.
        Task<List<FoodLogEntry>> GetFoodLogsAsync(DateTime date);

        // Create a new food log entry on the backend. Returns the server-assigned ID, or -1 on failure.
        Task<int> CreateFoodLogAsync(FoodLogEntry entry);

        // Delete a food log entry by its server ID. Returns true on success.
        Task<bool> DeleteFoodLogAsync(int serverId);

        // Ask the backend to compare a proposed meal against today's budget. Returns null if unavailable.
        Task<DietCompareResult?> CompareDietAsync(double proposedCalories, double todayLogged);

        // Request the backend to trigger a SOAP legacy export. Returns raw XML payload on success, null on failure.
        Task<string?> ExportDietLogAsync(DateTime from, DateTime to);

        // Send chat messages history to backend and receive response.
        Task<ChatResponseDto?> SendChatMessageAsync(List<ChatMessageDto> messages);
    }

    // Client-side mirror of the backend DietCompareResultDto.
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
