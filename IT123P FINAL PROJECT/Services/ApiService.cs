using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using IT123P_FINAL_PROJECT.Models;

namespace IT123P_FINAL_PROJECT.Services
{
    // Communicates with the FoodLens backend API.
    // Falls back to safe default operations when connection is unavailable.
    public class ApiService : IApiService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

        public ApiService(HttpClient http) => _http = http;

        // Nutrition lookup and search
        public async Task<List<FoodCandidate>> SearchFoodsAsync(string query, int limit = 10)
        {
            try
            {
                var url = $"api/nutrition/search?q={Uri.EscapeDataString(query)}&limit={limit}";
                var response = await _http.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var results = await response.Content.ReadFromJsonAsync<List<FoodSearchResult>>(_json);
                return results?.Select(r => new FoodCandidate
                {
                    Name = r.FoodName,
                    FdcId = r.FdcId,
                    Confidence = 100.0,
                    CaloriesPer100g = r.CaloriesPer100g,
                    ProteinPer100g = r.ProteinPer100g,
                    CarbsPer100g = r.CarbsPer100g,
                    FatPer100g = r.FatPer100g
                }).ToList() ?? [];
            }
            catch { return []; }
        }

        public async Task<(double Calories, double ProteinG, double CarbsG, double FatG)?> GetNutritionAsync(string fdcId, double grams)
        {
            try
            {
                var response = await _http.GetAsync($"api/nutrition/{Uri.EscapeDataString(fdcId)}?grams={grams}");
                if (!response.IsSuccessStatusCode) return null;
                var result = await response.Content.ReadFromJsonAsync<NutritionResult>(_json);
                if (result is null) return null;
                return (result.Calories, result.ProteinG, result.CarbsG, result.FatG);
            }
            catch { return null; }
        }

        public async Task<List<FoodCandidate>> ClassifyImageAsync(string imagePath)
        {
            try
            {
                if (!File.Exists(imagePath)) return [];
                using var content = new MultipartFormDataContent();
                using var fileStream = File.OpenRead(imagePath);
                using var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                content.Add(streamContent, "image", Path.GetFileName(imagePath));
                var response = await _http.PostAsync("api/recognition/classify", content);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<FoodCandidate>>(_json) ?? [];
            }
            catch { return []; }
        }

        // Diet Goal configuration
        public async Task<DietGoal?> GetDietGoalAsync()
        {
            try
            {
                var response = await _http.GetAsync("api/diet/goal");
                if (!response.IsSuccessStatusCode) return null;
                var dto = await response.Content.ReadFromJsonAsync<DietGoalDto>(_json);
                if (dto is null) return null;
                return new DietGoal
                {
                    Id = 1,
                    DailyCalorieLimit = dto.DailyCalorieLimit,
                    ProteinPercentage = dto.ProteinPercentage,
                    CarbsPercentage = dto.CarbsPercentage,
                    FatPercentage = dto.FatPercentage
                };
            }
            catch { return null; }
        }

        public async Task<bool> SaveDietGoalAsync(DietGoal goal)
        {
            try
            {
                var dto = new DietGoalDto
                {
                    DailyCalorieLimit = goal.DailyCalorieLimit,
                    ProteinPercentage = goal.ProteinPercentage,
                    CarbsPercentage = goal.CarbsPercentage,
                    FatPercentage = goal.FatPercentage
                };
                var json = JsonSerializer.Serialize(dto);
                var response = await _http.PutAsync("api/diet/goal",
                     new StringContent(json, Encoding.UTF8, "application/json"));
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // Food logging endpoints
        public async Task<List<FoodLogEntry>> GetFoodLogsAsync(DateTime date)
        {
            try
            {
                var dateStr = date.ToString("yyyy-MM-dd");
                var response = await _http.GetAsync($"api/foodlogs?date={dateStr}");
                if (!response.IsSuccessStatusCode) return [];
                var dtos = await response.Content.ReadFromJsonAsync<List<FoodLogDto>>(_json);
                return dtos?.Select(MapFromDto).ToList() ?? [];
            }
            catch { return []; }
        }

        public async Task<int> CreateFoodLogAsync(FoodLogEntry entry)
        {
            try
            {
                var dto = new CreateFoodLogDto
                {
                    FoodName = entry.FoodName,
                    Calories = entry.Calories,
                    ProteinG = entry.Protein,
                    CarbsG = entry.Carbs,
                    FatG = entry.Fat,
                    Grams = entry.Grams,
                    LoggedAt = entry.DateLogged
                };
                var json = JsonSerializer.Serialize(dto);
                var response = await _http.PostAsync("api/foodlogs",
                    new StringContent(json, Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode) return -1;
                var created = await response.Content.ReadFromJsonAsync<FoodLogDto>(_json);
                return created?.Id ?? -1;
            }
            catch { return -1; }
        }

        public async Task<bool> DeleteFoodLogAsync(int serverId)
        {
            try
            {
                var response = await _http.DeleteAsync($"api/foodlogs/{serverId}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // Diet Comparison API helper
        public async Task<DietCompareResult?> CompareDietAsync(double proposedCalories, double todayLogged)
        {
            try
            {
                var url = $"api/diet/compare?proposedCalories={proposedCalories}&todayLogged={todayLogged}";
                var response = await _http.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync<DietCompareResult>(_json);
            }
            catch { return null; }
        }

        public async Task<string?> ExportDietLogAsync(DateTime from, DateTime to)
        {
            try
            {
                var body = new { UserId = "default", FromDate = from, ToDate = to };
                var json = JsonSerializer.Serialize(body);
                
                using var request = new HttpRequestMessage(HttpMethod.Post, "api/export");
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/xml"));
                
                var response = await _http.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    return $"Error: {content}";
                }
                
                return content;
            }
            catch (Exception ex)
            {
                return $"Error: Connection failed. {ex.Message}";
            }
        }

        public async Task<ChatResponseDto?> SendChatMessageAsync(List<ChatMessageDto> messages)
        {
            try
            {
                var json = JsonSerializer.Serialize(messages);
                using var request = new HttpRequestMessage(HttpMethod.Post, "api/chat/message");
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;
                
                return await response.Content.ReadFromJsonAsync<ChatResponseDto>(_json);
            }
            catch { return null; }
        }

        // Internal DTO mapping helpers
        private static FoodLogEntry MapFromDto(FoodLogDto dto) => new()
        {
            ServerId = dto.Id,
            IsSynced = true,
            FoodName = dto.FoodName,
            Calories = dto.Calories,
            Protein = dto.ProteinG,
            Carbs = dto.CarbsG,
            Fat = dto.FatG,
            Grams = dto.Grams,
            DateLogged = dto.LoggedAt
        };

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

        private class DietGoalDto
        {
            public double DailyCalorieLimit { get; set; }
            public double ProteinPercentage { get; set; }
            public double CarbsPercentage { get; set; }
            public double FatPercentage { get; set; }
        }

        private class FoodLogDto
        {
            public int Id { get; set; }
            public string FoodName { get; set; } = string.Empty;
            public double Calories { get; set; }
            public double ProteinG { get; set; }
            public double CarbsG { get; set; }
            public double FatG { get; set; }
            public double Grams { get; set; }
            public DateTime LoggedAt { get; set; }
        }

        private class CreateFoodLogDto
        {
            public string FoodName { get; set; } = string.Empty;
            public double Calories { get; set; }
            public double ProteinG { get; set; }
            public double CarbsG { get; set; }
            public double FatG { get; set; }
            public double Grams { get; set; }
            public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
        }
    }
}
