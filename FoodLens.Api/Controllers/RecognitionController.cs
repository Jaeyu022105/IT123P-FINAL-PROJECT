using System.Text.Json;
using System.Text.Json.Serialization;
using FoodLens.Api.DTOs;
using FoodLens.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodLens.Api.Controllers
{
    /// <summary>
    /// Proxies photo recognition requests to the LogMeal API, parses identified dishes,
    /// and resolves them against USDA FoodData Central.
    /// </summary>
    [ApiController]
    [Route("api/recognition")]
    [Produces("application/json", "application/xml")]
    public class RecognitionController : ControllerBase
    {
        private readonly IUsdaClient _usda;
        private readonly HttpClient _http;
        private readonly string _userToken;
        private readonly string _apiUrl;
        private readonly ILogger<RecognitionController> _logger;

        private static string? _cachedUserToken = null;
        private static readonly string CacheFilePath = Path.Combine(Path.GetTempPath(), "logmeal_user_token.txt");

        private async Task<string> GetActiveUserTokenAsync()
        {
            if (!string.IsNullOrEmpty(_cachedUserToken))
            {
                return _cachedUserToken;
            }

            if (System.IO.File.Exists(CacheFilePath))
            {
                _cachedUserToken = await System.IO.File.ReadAllTextAsync(CacheFilePath);
                if (!string.IsNullOrEmpty(_cachedUserToken))
                {
                    return _cachedUserToken;
                }
            }

            try
            {
                _logger.LogInformation("Attempting to register a default APIUser on LogMeal using Company token...");
                using var signupRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.logmeal.com/v2/users/signUp");
                signupRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _userToken);
                
                var signupData = new { username = $"foodlens_user_{Guid.NewGuid().ToString("N")[..8]}", language = "eng" };
                signupRequest.Content = new StringContent(JsonSerializer.Serialize(signupData), System.Text.Encoding.UTF8, "application/json");

                var response = await _http.SendAsync(signupRequest);
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("token", out var tokenProp))
                    {
                        var token = tokenProp.GetString();
                        if (!string.IsNullOrEmpty(token))
                        {
                            _cachedUserToken = token;
                            await System.IO.File.WriteAllTextAsync(CacheFilePath, token);
                            _logger.LogInformation("Successfully registered and cached new LogMeal APIUser token.");
                            return token;
                        }
                    }
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("LogMeal user registration returned non-success: {Body}. Using configured token directly.", err);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed programmatically registering LogMeal APIUser. Falling back to configured token.");
            }

            return _userToken;
        }

        public RecognitionController(
            IUsdaClient usda,
            HttpClient http,
            IConfiguration config,
            ILogger<RecognitionController> logger)
        {
            _usda = usda;
            _http = http;
            _userToken = config["LogMeal:UserToken"] ?? throw new InvalidOperationException("LogMeal UserToken is not configured.");
            _apiUrl = config["LogMeal:ApiUrl"] ?? "https://api.logmeal.com/v2/image/recognition/dish";
            _logger = logger;
        }

        /// <summary>
        /// Classify food inside an uploaded image file.
        /// POST /api/recognition/classify
        /// </summary>
        [HttpPost("classify")]
        [ProducesResponseType(typeof(List<FoodCandidateDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(502)]
        public async Task<IActionResult> Classify(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                return BadRequest(new { error = new { code = "MISSING_IMAGE", message = "An image file is required." } });
            }

            try
            {
                _logger.LogInformation("Uploading image '{FileName}' to LogMeal API...", image.FileName);

                // Prepare multipart form data content
                using var multipartContent = new MultipartFormDataContent();
                using var stream = image.OpenReadStream();
                using var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(image.ContentType ?? "image/jpeg");
                multipartContent.Add(streamContent, "image", image.FileName);

                var activeToken = await GetActiveUserTokenAsync();

                // Build request to LogMeal
                using var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", activeToken);
                request.Content = multipartContent;

                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("LogMeal API error (Status {Status}): {Body}", response.StatusCode, errorBody);

                    // If unauthorized, clear cached token so we retry registration next time
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _cachedUserToken = null;
                        if (System.IO.File.Exists(CacheFilePath))
                        {
                            System.IO.File.Delete(CacheFilePath);
                        }
                    }

                    return StatusCode(502, new { error = new { code = "LOGMEAL_ERROR", message = "LogMeal recognition service returned an error. Try again." } });
                }

                // Parse LogMeal response
                var responseBody = await response.Content.ReadAsStringAsync();
                var logMealData = JsonSerializer.Deserialize<LogMealRecognitionResponse>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var recognitionResults = logMealData?.RecognitionResults;
                if (recognitionResults == null || recognitionResults.Count == 0)
                {
                    // Fallback to segmentation_results
                    if (logMealData?.SegmentationResults != null)
                    {
                        recognitionResults = logMealData.SegmentationResults
                            .Where(s => s.RecognitionResults != null)
                            .SelectMany(s => s.RecognitionResults!)
                            .ToList();
                    }
                }

                if (recognitionResults == null || recognitionResults.Count == 0)
                {
                    _logger.LogWarning("LogMeal returned zero food recognition candidates.");
                    return Ok(new List<FoodCandidateDto>());
                }

                // Build output list and resolve each candidate name against USDA
                var candidates = new List<FoodCandidateDto>();
                foreach (var logMealDish in recognitionResults.OrderByDescending(d => d.Prob).Take(5))
                {
                    var candidate = new FoodCandidateDto
                    {
                        Name = CapitalizeWords(logMealDish.Name),
                        Confidence = Math.Round(logMealDish.Prob * 100, 1)
                    };

                    try
                    {
                        // Auto-resolve USDA FdcId & nutrition data from name
                        var usdaSearch = await _usda.SearchFoodsAsync(logMealDish.Name, 1);
                        if (usdaSearch != null && usdaSearch.Count > 0)
                        {
                            var topMatch = usdaSearch[0];
                            candidate.FdcId = topMatch.FdcId;
                            candidate.CaloriesPer100g = topMatch.CaloriesPer100g;
                            candidate.ProteinPer100g = topMatch.ProteinPer100g;
                            candidate.CarbsPer100g = topMatch.CarbsPer100g;
                            candidate.FatPer100g = topMatch.FatPer100g;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Keep candidate with empty USDA details on USDA lookup failures
                        _logger.LogError(ex, "Failed to resolve USDA match for candidate name '{Name}'", logMealDish.Name);
                    }

                    candidates.Add(candidate);
                }

                return Ok(candidates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Image classification failed");
                return StatusCode(500, new { error = new { code = "CLASSIFICATION_FAILED", message = "An error occurred during food image classification." } });
            }
        }

        private static string CapitalizeWords(string source)
        {
            if (string.IsNullOrWhiteSpace(source)) return string.Empty;
            return string.Join(" ", source.Split(' ')
                .Select(word => word.Length > 0 ? char.ToUpper(word[0]) + word[1..].ToLower() : string.Empty));
        }

        // Internal JSON mapping classes
        private class LogMealRecognitionResponse
        {
            [JsonPropertyName("recognition_results")]
            public List<LogMealDish>? RecognitionResults { get; set; }

            [JsonPropertyName("segmentation_results")]
            public List<LogMealSegment>? SegmentationResults { get; set; }
        }

        private class LogMealSegment
        {
            [JsonPropertyName("recognition_results")]
            public List<LogMealDish>? RecognitionResults { get; set; }
        }

        private class LogMealDish
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("prob")]
            public double Prob { get; set; }
        }
    }
}
