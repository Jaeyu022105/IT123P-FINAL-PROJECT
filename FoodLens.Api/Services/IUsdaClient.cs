using FoodLens.Api.DTOs;

namespace FoodLens.Api.Services
{
    public interface IUsdaClient
    {
        Task<List<FoodSearchResultDto>> SearchFoodsAsync(string query, int maxResults = 10);
        Task<NutritionResultDto?> GetNutritionAsync(string fdcId, double grams);
    }
}
