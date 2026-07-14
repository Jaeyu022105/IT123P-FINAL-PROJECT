using IT123P_FINAL_PROJECT.Models;

namespace IT123P_FINAL_PROJECT.Services
{
    public interface IDatabaseService
    {
        Task InitializeAsync();

        // ── Food Logs ─────────────────────────────────────────────────────────
        Task<List<FoodLogEntry>> GetFoodLogEntriesAsync(DateTime date);
        Task<FoodLogEntry?> GetFoodLogEntryAsync(int id);
        Task<int> SaveFoodLogEntryAsync(FoodLogEntry entry);
        Task<int> DeleteFoodLogEntryAsync(FoodLogEntry entry);

        // ── Diet Goal ─────────────────────────────────────────────────────────
        Task<DietGoal> GetDietGoalAsync();
        Task<int> SaveDietGoalAsync(DietGoal goal);

        // ── Sync Helpers ──────────────────────────────────────────────────────
        Task SyncPendingLogsAsync(IApiService apiService);
        Task RefreshLogsFromServerAsync(List<FoodLogEntry> serverEntries, DateTime date);
        Task RefreshGoalFromServerAsync(DietGoal serverGoal);
    }
}
