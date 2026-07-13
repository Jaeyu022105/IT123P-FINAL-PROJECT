using FoodLens.Api.DTOs;
using FoodLens.Api.Models;
using FoodLens.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodLens.Api.Controllers
{
    /// <summary>
    /// Full CRUD for food log entries stored in the backend database.
    /// Satisfies the assignment's required CRUD web service requirement.
    /// </summary>
    [ApiController]
    [Route("api/foodlogs")]
    [Produces("application/json", "application/xml")]
    public class FoodLogController : ControllerBase
    {
        private readonly IFoodLogRepository _repo;
        private const string DefaultDeviceId = "default"; // device-based identity for v1

        public FoodLogController(IFoodLogRepository repo) => _repo = repo;

        /// <summary>GET /api/foodlogs?date=2026-07-13</summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<FoodLogResponseDto>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] DateTime? date)
        {
            var day = date?.Date ?? DateTime.UtcNow.Date;
            var logs = await _repo.GetByDateAsync(DefaultDeviceId, day);
            return Ok(logs.Select(MapToDto));
        }

        /// <summary>GET /api/foodlogs/{id}</summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(FoodLogResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            var log = await _repo.GetByIdAsync(id);
            return log is null ? NotFound(NotFoundError(id)) : Ok(MapToDto(log));
        }

        /// <summary>POST /api/foodlogs</summary>
        [HttpPost]
        [ProducesResponseType(typeof(FoodLogResponseDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] FoodLogCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entity = new FoodLog
            {
                DeviceId = DefaultDeviceId,
                FoodName = dto.FoodName,
                FdcId = dto.FdcId,
                Calories = dto.Calories,
                ProteinG = dto.ProteinG,
                CarbsG = dto.CarbsG,
                FatG = dto.FatG,
                Grams = dto.Grams,
                LoggedAt = dto.LoggedAt.ToUniversalTime()
            };

            var created = await _repo.CreateAsync(entity);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
        }

        /// <summary>PUT /api/foodlogs/{id}</summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(FoodLogResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(int id, [FromBody] FoodLogUpdateDto dto)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing is null) return NotFound(NotFoundError(id));

            var updated = new FoodLog
            {
                Grams = dto.Grams ?? existing.Grams,
                Calories = dto.Calories ?? existing.Calories,
                ProteinG = dto.ProteinG ?? existing.ProteinG,
                CarbsG = dto.CarbsG ?? existing.CarbsG,
                FatG = dto.FatG ?? existing.FatG
            };

            var result = await _repo.UpdateAsync(id, updated);
            return Ok(MapToDto(result!));
        }

        /// <summary>DELETE /api/foodlogs/{id}</summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _repo.DeleteAsync(id);
            return deleted ? NoContent() : NotFound(NotFoundError(id));
        }

        private static FoodLogResponseDto MapToDto(FoodLog log) => new()
        {
            Id = log.Id,
            FoodName = log.FoodName,
            FdcId = log.FdcId,
            Calories = log.Calories,
            ProteinG = log.ProteinG,
            CarbsG = log.CarbsG,
            FatG = log.FatG,
            Grams = log.Grams,
            LoggedAt = log.LoggedAt
        };

        private static object NotFoundError(int id) =>
            new { error = new { code = "NOT_FOUND", message = $"Food log entry {id} not found." } };
    }
}
