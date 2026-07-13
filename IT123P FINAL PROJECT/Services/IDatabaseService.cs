using IT123P_FINAL_PROJECT.Models;

namespace IT123P_FINAL_PROJECT.Services
{
    public interface IDatabaseService
    {
        Task InitializeAsync();
        Task<List<FoodLogEntry>> GetFoodLogEntriesAsync(DateTime date);
        Task<FoodLogEntry?> GetFoodLogEntryAsync(int id);
        Task<int> SaveFoodLogEntryAsync(FoodLogEntry entry);
        Task<int> DeleteFoodLogEntryAsync(FoodLogEntry entry);
        Task<DietGoal> GetDietGoalAsync();
        Task<int> SaveDietGoalAsync(DietGoal goal);
    }
}
