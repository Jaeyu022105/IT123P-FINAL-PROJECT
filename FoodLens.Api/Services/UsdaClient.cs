using System.Net.Http.Json;
using System.Text.Json;
using FoodLens.Api.DTOs;

namespace FoodLens.Api.Services
{
    /// <summary>
    /// Calls the USDA FoodData Central REST API (free tier, no rate-limit ceiling).
    /// API docs: https://fdc.nal.usda.gov/api-guide.html
    /// </summary>
    public class UsdaClient : IUsdaClient
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly ILogger<UsdaClient> _logger;

        // Nutrient IDs used by USDA FoodData Central
        private const int NutrientIdEnergy = 1008;   // Calories (kcal)
        private const int NutrientIdProtein = 1003;  // Protein (g)
        private const int NutrientIdCarbs = 1005;    // Carbohydrate, by difference (g)
        private const int NutrientIdFat = 1004;      // Total lipid (fat) (g)

        public UsdaClient(HttpClient http, IConfiguration config, ILogger<UsdaClient> logger)
        {
            _http = http;
            _apiKey = config["Usda:ApiKey"] ?? throw new InvalidOperationException("USDA API key not configured.");
            _logger = logger;
        }

        public async Task<List<FoodSearchResultDto>> SearchFoodsAsync(string query, int maxResults = 10)
        {
            var url = $"foods/search?query={Uri.EscapeDataString(query)}&pageSize={maxResults}&api_key={_apiKey}";

            try
            {
                var response = await _http.GetAsync(url);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                var json = await JsonDocument.ParseAsync(stream);
                var foods = json.RootElement.GetProperty("foods");

                var results = new List<FoodSearchResultDto>();

                foreach (var food in foods.EnumerateArray())
                {
                    var fdcId = food.GetProperty("fdcId").GetInt32().ToString();
                    var name = food.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "";
                    var brand = food.TryGetProperty("brandOwner", out var b) ? b.GetString() : null;

                    // Parse nutrient values from the foodNutrients array
                    double cal = 0, protein = 0, carbs = 0, fat = 0;
                    if (food.TryGetProperty("foodNutrients", out var nutrients))
                    {
                        foreach (var n in nutrients.EnumerateArray())
                        {
                            var nId = n.TryGetProperty("nutrientId", out var nIdEl) ? nIdEl.GetInt32() : 0;
                            var val = n.TryGetProperty("value", out var valEl) ? valEl.GetDouble() : 0;

                            switch (nId)
                            {
                                case NutrientIdEnergy: cal = val; break;
                                case NutrientIdProtein: protein = val; break;
                                case NutrientIdCarbs: carbs = val; break;
                                case NutrientIdFat: fat = val; break;
                            }
                        }
                    }

                    results.Add(new FoodSearchResultDto
                    {
                        FdcId = fdcId,
                        FoodName = name,
                        BrandOwner = brand,
                        CaloriesPer100g = cal,
                        ProteinPer100g = protein,
                        CarbsPer100g = carbs,
                        FatPer100g = fat
                    });
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "USDA search failed for query '{Query}'", query);
                throw;
            }
        }

        public async Task<NutritionResultDto?> GetNutritionAsync(string fdcId, double grams)
        {
            var url = $"food/{fdcId}?api_key={_apiKey}";

            try
            {
                var response = await _http.GetAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                var json = await JsonDocument.ParseAsync(stream);

                var name = json.RootElement.TryGetProperty("description", out var desc)
                    ? desc.GetString() ?? fdcId
                    : fdcId;

                double calPer100 = 0, proteinPer100 = 0, carbsPer100 = 0, fatPer100 = 0;

                if (json.RootElement.TryGetProperty("foodNutrients", out var nutrients))
                {
                    foreach (var n in nutrients.EnumerateArray())
                    {
                        var nId = 0;
                        if (n.TryGetProperty("nutrient", out var nutrientObj))
                            nId = nutrientObj.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0;
                        else if (n.TryGetProperty("nutrientId", out var flatId))
                            nId = flatId.GetInt32();

                        var val = n.TryGetProperty("amount", out var amt) ? amt.GetDouble()
                                : n.TryGetProperty("value", out var v2) ? v2.GetDouble()
                                : 0;

                        switch (nId)
                        {
                            case NutrientIdEnergy: calPer100 = val; break;
                            case NutrientIdProtein: proteinPer100 = val; break;
                            case NutrientIdCarbs: carbsPer100 = val; break;
                            case NutrientIdFat: fatPer100 = val; break;
                        }
                    }
                }

                var factor = grams / 100.0;
                return new NutritionResultDto
                {
                    FdcId = fdcId,
                    FoodName = name,
                    Grams = grams,
                    Calories = Math.Round(calPer100 * factor, 1),
                    ProteinG = Math.Round(proteinPer100 * factor, 1),
                    CarbsG = Math.Round(carbsPer100 * factor, 1),
                    FatG = Math.Round(fatPer100 * factor, 1)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "USDA nutrition fetch failed for FdcId '{FdcId}'", fdcId);
                throw;
            }
        }
    }
}
