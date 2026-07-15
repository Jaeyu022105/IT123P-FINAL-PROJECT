using System.Text.Json;
using FoodLens.Api.Models;
using FoodLens.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodLens.Api.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Produces("application/json")]
    public class ChatController : ControllerBase
    {
        private readonly HttpClient _http;
        private readonly IDietGoalRepository _repo;
        private readonly IConfiguration _config;
        private readonly ILogger<ChatController> _logger;

        private const string SystemPrompt = @"You are a professional, friendly, certified AI dietitian and nutritionist for the FoodLens app. 
You answer the user's questions about food, nutrition, healthy eating, recipes, and diet tracking.
Your primary goal is to help them establish healthy dietary guidelines.

If the user discusses setting, changing, or accepting a daily target, or if you recommend a diet plan (e.g. Ketogenic, High Protein, Mediterranean, Calorie deficit, etc.) and they agree/ask you to set it, you MUST append a structured JSON block at the very end of your response.
The format MUST be EXACTLY as follows (all on one line, with no extra formatting or backticks around it):
|||DIET_PLAN:{""calories"":2000,""protein"":30,""carbs"":50,""fat"":20}|||

The JSON block must specify:
1. ""calories"" (integer, daily calorie limit)
2. ""protein"" (integer percentage, e.g. 30)
3. ""carbs"" (integer percentage, e.g. 50)
4. ""fat"" (integer percentage, e.g. 20)
Crucially, the protein, carbs, and fat percentages must add up to exactly 100%.

If the user asks questions unrelated to diet, food, or nutrition, politely steer them back to dietary topics. Keep your messages concise, encouraging, and clear.";

        public ChatController(
            HttpClient http,
            IDietGoalRepository repo,
            IConfiguration config,
            ILogger<ChatController> logger)
        {
            _http = http;
            _repo = repo;
            _config = config;
            _logger = logger;
        }

        [HttpPost("message")]
        [ProducesResponseType(typeof(ChatResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(502)]
        public async Task<IActionResult> GetMessage([FromBody] List<ChatMessageDto> messages)
        {
            if (messages == null || messages.Count == 0)
            {
                return BadRequest("Messages list cannot be empty.");
            }

            var apiKey = _config["Gemini:ApiKey"];
            var model = _config["Gemini:Model"] ?? "gemini-1.5-flash";

            if (string.IsNullOrEmpty(apiKey))
            {
                return StatusCode(502, new { error = new { code = "GEMINI_NOT_CONFIGURED", message = "Gemini API key is not configured in the backend." } });
            }

            try
            {
                // Map the conversation history to Gemini structure
                var contentsList = new List<object>();
                foreach (var msg in messages)
                {
                    contentsList.Add(new
                    {
                        role = msg.Role == "assistant" ? "model" : "user",
                        parts = new[] { new { text = msg.Text } }
                    });
                }

                var requestBody = new
                {
                    contents = contentsList,
                    systemInstruction = new
                    {
                        parts = new[] { new { text = SystemPrompt } }
                    }
                };

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");

                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini API error (Status {Status}): {Body}", response.StatusCode, errorBody);
                    return StatusCode(502, new { error = new { code = "GEMINI_ERROR", message = "Gemini service returned an error." } });
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseBody);
                
                var text = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? string.Empty;

                bool planUpdated = false;
                double calories = 0;
                double protein = 0;
                double carbs = 0;
                double fat = 0;

                int startIndex = text.IndexOf("|||DIET_PLAN:");
                if (startIndex >= 0)
                {
                    int endIndex = text.IndexOf("|||", startIndex + 13);
                    if (endIndex >= 0)
                    {
                        var jsonStr = text.Substring(startIndex + 13, endIndex - (startIndex + 13)).Trim();
                        try
                        {
                            using var planDoc = JsonDocument.Parse(jsonStr);
                            calories = planDoc.RootElement.GetProperty("calories").GetDouble();
                            protein = planDoc.RootElement.GetProperty("protein").GetDouble();
                            carbs = planDoc.RootElement.GetProperty("carbs").GetDouble();
                            fat = planDoc.RootElement.GetProperty("fat").GetDouble();

                            if (Math.Abs((protein + carbs + fat) - 100) < 0.1)
                            {
                                planUpdated = true;
                                _logger.LogInformation("Parsed Gemini AI recommended plan. Calories: {Calories}", calories);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to parse diet plan JSON from Gemini response: {Json}", jsonStr);
                        }

                        // Strip the tag out of the text
                        text = (text.Substring(0, startIndex) + text.Substring(endIndex + 3)).Trim();
                    }
                }

                return Ok(new ChatResponseDto
                {
                    Text = text,
                    PlanUpdated = planUpdated,
                    ProposedCalories = calories,
                    ProposedProtein = protein,
                    ProposedCarbs = carbs,
                    ProposedFat = fat
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini chat process failed");
                return StatusCode(500, new { error = new { code = "CHAT_FAILED", message = "An error occurred during chat processing." } });
            }
        }
    }

    public class ChatMessageDto
    {
        public string Role { get; set; } = string.Empty; // "user" or "assistant"
        public string Text { get; set; } = string.Empty;
    }

    public class ChatResponseDto
    {
        public string Text { get; set; } = string.Empty;
        public bool PlanUpdated { get; set; }
        public double ProposedCalories { get; set; }
        public double ProposedProtein { get; set; }
        public double ProposedCarbs { get; set; }
        public double ProposedFat { get; set; }
    }
}
