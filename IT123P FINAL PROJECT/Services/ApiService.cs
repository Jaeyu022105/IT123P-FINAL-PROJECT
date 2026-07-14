using System.Net.Http.Json;
using System.Text.Json;
using IT123P_FINAL_PROJECT.Models;

namespace IT123P_FINAL_PROJECT.Services
{
    /// <summary>
    /// Calls the FoodLens backend REST API.
    /// Base URL defaults to https://localhost:5001 for local development.
    /// </summary>
    public class ApiService : IApiService
    {
        private readonly HttpClient _http;

        public ApiService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<FoodCandidate>> SearchFoodsAsync(string query, int limit = 10)
        {
            try
            {
                var url = $"api/nutrition/search?q={Uri.EscapeDataString(query)}&limit={limit}";
                var response = await _http.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var results = await response.Content.ReadFromJsonAsync<List<FoodSearchResult>>(options);

                return results?.Select(r => new FoodCandidate
                {
                    Name = r.FoodName,
                    FdcId = r.FdcId,
                    Confidence = 100.0, // API search results have 100% match confidence
                    CaloriesPer100g = r.CaloriesPer100g,
                    ProteinPer100g = r.ProteinPer100g,
                    CarbsPer100g = r.CarbsPer100g,
                    FatPer100g = r.FatPer100g
                }).ToList() ?? new List<FoodCandidate>();
            }
            catch (Exception)
            {
                // Return empty — caller shows an offline/error message
                return new List<FoodCandidate>();
            }
        }

        public async Task<(double Calories, double ProteinG, double CarbsG, double FatG)?> GetNutritionAsync(string fdcId, double grams)
        {
            try
            {
                var url = $"api/nutrition/{Uri.EscapeDataString(fdcId)}?grams={grams}";
                var response = await _http.GetAsync(url);

                if (!response.IsSuccessStatusCode) return null;

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = await response.Content.ReadFromJsonAsync<NutritionResult>(options);
                if (result is null) return null;

                return (result.Calories, result.ProteinG, result.CarbsG, result.FatG);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<FoodCandidate>> ClassifyImageAsync(string imagePath)
        {
            try
            {
                if (!File.Exists(imagePath)) return new List<FoodCandidate>();

                using var content = new MultipartFormDataContent();
                using var fileStream = File.OpenRead(imagePath);
                using var streamContent = new StreamContent(fileStream);
                // Set boundary content type
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                content.Add(streamContent, "image", Path.GetFileName(imagePath));

                var response = await _http.PostAsync("api/recognition/classify", content);
                response.EnsureSuccessStatusCode();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var candidates = await response.Content.ReadFromJsonAsync<List<FoodCandidate>>(options);
                return candidates ?? new List<FoodCandidate>();
            }
            catch (Exception)
            {
                // Graceful fallback on network/server failures
                return new List<FoodCandidate>();
            }
        }

        // ── Internal response shapes (mirror backend DTOs) ─────────────────────
        private class FoodSearchResult
        {
            public string FdcId { get; set; } = string.Empty;
            public string FoodName { get; set; } = string.Empty;
            public double CaloriesPer100g { get; set; }
            public double ProteinPer100g { get; set; }
            public double CarbsPer100g { get; set; }
            public double FatPer100g { get; set; }
        }

        private class NutritionResult
        {
            public double Calories { get; set; }
            public double ProteinG { get; set; }
            public double CarbsG { get; set; }
            public double FatG { get; set; }
        }
    }
}
