using IT123P_FINAL_PROJECT.Models;

namespace IT123P_FINAL_PROJECT.Services
{
    public interface IApiService
    {
        /// <summary>Search USDA food database via the backend. Returns an empty list on failure (never throws).</summary>
        Task<List<FoodCandidate>> SearchFoodsAsync(string query, int limit = 10);

        /// <summary>Get scaled nutrition for a food. Returns null if backend is unavailable.</summary>
        Task<(double Calories, double ProteinG, double CarbsG, double FatG)?> GetNutritionAsync(string fdcId, double grams);
    }
}
