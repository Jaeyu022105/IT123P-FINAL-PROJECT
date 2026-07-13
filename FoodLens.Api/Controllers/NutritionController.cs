using FoodLens.Api.DTOs;
using FoodLens.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodLens.Api.Controllers
{
    /// <summary>
    /// Wraps USDA FoodData Central for food search and per-portion nutrition lookup.
    /// </summary>
    [ApiController]
    [Route("api/nutrition")]
    [Produces("application/json", "application/xml")]
    public class NutritionController : ControllerBase
    {
        private readonly IUsdaClient _usda;
        private readonly ILogger<NutritionController> _logger;

        public NutritionController(IUsdaClient usda, ILogger<NutritionController> logger)
        {
            _usda = usda;
            _logger = logger;
        }

        /// <summary>
        /// Search USDA food database by name.
        /// GET /api/nutrition/search?q=chicken&limit=10
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(List<FoodSearchResultDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(503)]
        public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { error = new { code = "MISSING_QUERY", message = "Query parameter 'q' is required." } });

            try
            {
                var results = await _usda.SearchFoodsAsync(q, Math.Clamp(limit, 1, 25));
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "USDA search unavailable for query '{Q}'", q);
                return StatusCode(503, new { error = new { code = "USDA_UNAVAILABLE", message = "Nutrition data service is temporarily unavailable. Try again shortly." } });
            }
        }

        /// <summary>
        /// Get scaled nutrition for a specific food + portion size.
        /// GET /api/nutrition/{fdcId}?grams=150
        /// </summary>
        [HttpGet("{fdcId}")]
        [ProducesResponseType(typeof(NutritionResultDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(503)]
        public async Task<IActionResult> GetNutrition(string fdcId, [FromQuery] double grams = 100)
        {
            if (grams <= 0)
                return BadRequest(new { error = new { code = "INVALID_GRAMS", message = "Grams must be greater than 0." } });

            try
            {
                var result = await _usda.GetNutritionAsync(fdcId, grams);
                if (result is null)
                    return NotFound(new { error = new { code = "FOOD_NOT_FOUND", message = $"No food found with USDA ID '{fdcId}'." } });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "USDA nutrition fetch unavailable for FdcId '{FdcId}'", fdcId);
                return StatusCode(503, new { error = new { code = "USDA_UNAVAILABLE", message = "Nutrition data service is temporarily unavailable. Try again shortly." } });
            }
        }
    }
}
