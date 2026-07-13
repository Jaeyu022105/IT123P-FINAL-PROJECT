using FoodLens.Api.Data;
using FoodLens.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodLens.Api.Services
{
    public class DietGoalRepository : IDietGoalRepository
    {
        private readonly AppDbContext _db;

        public DietGoalRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<DietGoal> GetAsync(string deviceId)
        {
            var goal = await _db.DietGoals.FirstOrDefaultAsync(g => g.DeviceId == deviceId);
            if (goal == null)
            {
                // Fallback / default goal
                goal = new DietGoal
                {
                    DeviceId = deviceId,
                    DailyCalorieLimit = 2000,
                    ProteinPercentage = 30,
                    CarbsPercentage = 40,
                    FatPercentage = 30
                };
                _db.DietGoals.Add(goal);
                await _db.SaveChangesAsync();
            }
            return goal;
        }

        public async Task<DietGoal> UpsertAsync(string deviceId, DietGoal goal)
        {
            var existing = await _db.DietGoals.FirstOrDefaultAsync(g => g.DeviceId == deviceId);
            if (existing == null)
            {
                goal.DeviceId = deviceId;
                _db.DietGoals.Add(goal);
                await _db.SaveChangesAsync();
                return goal;
            }

            existing.DailyCalorieLimit = goal.DailyCalorieLimit;
            existing.ProteinPercentage = goal.ProteinPercentage;
            existing.CarbsPercentage = goal.CarbsPercentage;
            existing.FatPercentage = goal.FatPercentage;
            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(string deviceId)
        {
            var existing = await _db.DietGoals.FirstOrDefaultAsync(g => g.DeviceId == deviceId);
            if (existing == null) return false;

            _db.DietGoals.Remove(existing);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
