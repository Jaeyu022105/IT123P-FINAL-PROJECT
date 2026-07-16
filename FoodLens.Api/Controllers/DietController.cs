using FoodLens.Api.DTOs;
using FoodLens.Api.Models;
using FoodLens.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodLens.Api.Controllers
{
    // Diet goal configuration and comparison endpoints
    [ApiController]
    [Route("api/diet")]
    [Produces("application/json", "application/xml")]
    public class DietController : ControllerBase
    {
        private readonly IDietGoalRepository _repo;
        private const string DefaultDeviceId = "default";

        public DietController(IDietGoalRepository repo) => _repo = repo;

        // GET /api/diet/goal
        [HttpGet("goal")]
        [ProducesResponseType(typeof(DietGoalDto), 200)]
        public async Task<IActionResult> GetGoal()
        {
            var goal = await _repo.GetAsync(DefaultDeviceId);
            return Ok(MapToDto(goal));
        }

        // POST /api/diet/goal: first-time setup
        [HttpPost("goal")]
        [ProducesResponseType(typeof(DietGoalDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateGoal([FromBody] DietGoalDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!ValidateMacroSplit(dto, out var err)) return BadRequest(err);

            var saved = await _repo.UpsertAsync(DefaultDeviceId, MapToEntity(dto));
            return CreatedAtAction(nameof(GetGoal), null, MapToDto(saved));
        }

        // PUT /api/diet/goal: update existing goal
        [HttpPut("goal")]
        [ProducesResponseType(typeof(DietGoalDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UpdateGoal([FromBody] DietGoalDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!ValidateMacroSplit(dto, out var err)) return BadRequest(err);

            var saved = await _repo.UpsertAsync(DefaultDeviceId, MapToEntity(dto));
            return Ok(MapToDto(saved));
        }

        // DELETE /api/diet/goal: reset goal to defaults
        [HttpDelete("goal")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> DeleteGoal()
        {
            await _repo.DeleteAsync(DefaultDeviceId);
            return NoContent();
        }

        // GET /api/diet/compare?proposedCalories=450&todayLogged=1200
        // Pure comparison calculation with no DB write or external call.
        [HttpGet("compare")]
        [ProducesResponseType(typeof(DietCompareResultDto), 200)]
        public async Task<IActionResult> Compare(
            [FromQuery] double proposedCalories,
            [FromQuery] double todayLogged)
        {
            var goal = await _repo.GetAsync(DefaultDeviceId);
            var projected = todayLogged + proposedCalories;
            var remaining = goal.DailyCalorieLimit - projected;
            var fits = remaining >= 0;

            return Ok(new DietCompareResultDto
            {
                DailyLimit = goal.DailyCalorieLimit,
                TodayLogged = todayLogged,
                ProposedCalories = proposedCalories,
                ProjectedTotal = projected,
                RemainingAfter = Math.Max(0, remaining),
                FitsDiet = fits,
                Message = fits
                    ? $"Fits your budget! {remaining:F0} kcal remaining after this meal."
                    : $"Exceeds daily goal by {Math.Abs(remaining):F0} kcal."
            });
        }

        private static bool ValidateMacroSplit(DietGoalDto dto, out object error)
        {
            var total = dto.ProteinPercentage + dto.CarbsPercentage + dto.FatPercentage;
            if (Math.Abs(total - 100.0) > 0.1)
            {
                error = new { error = new { code = "INVALID_MACRO_SPLIT", message = $"Macro percentages must total 100%. Got {total:F1}%." } };
                return false;
            }
            error = null!;
            return true;
        }

        private static DietGoalDto MapToDto(DietGoal g) => new()
        {
            DailyCalorieLimit = g.DailyCalorieLimit,
            ProteinPercentage = g.ProteinPercentage,
            CarbsPercentage = g.CarbsPercentage,
            FatPercentage = g.FatPercentage
        };

        private static DietGoal MapToEntity(DietGoalDto dto) => new()
        {
            DailyCalorieLimit = dto.DailyCalorieLimit,
            ProteinPercentage = dto.ProteinPercentage,
            CarbsPercentage = dto.CarbsPercentage,
            FatPercentage = dto.FatPercentage
        };
    }
}
