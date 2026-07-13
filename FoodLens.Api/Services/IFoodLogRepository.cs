using FoodLens.Api.Models;

namespace FoodLens.Api.Services
{
    public interface IFoodLogRepository
    {
        Task<List<FoodLog>> GetByDateAsync(string deviceId, DateTime date);
        Task<FoodLog?> GetByIdAsync(int id);
        Task<FoodLog> CreateAsync(FoodLog log);
        Task<FoodLog?> UpdateAsync(int id, FoodLog updated);
        Task<bool> DeleteAsync(int id);
    }
}
