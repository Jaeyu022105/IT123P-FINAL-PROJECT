using FoodLens.Api.Models;

namespace FoodLens.Api.Services
{
    public interface IDietGoalRepository
    {
        Task<DietGoal> GetAsync(string deviceId);
        Task<DietGoal> UpsertAsync(string deviceId, DietGoal goal);
        Task<bool> DeleteAsync(string deviceId);
    }
}
