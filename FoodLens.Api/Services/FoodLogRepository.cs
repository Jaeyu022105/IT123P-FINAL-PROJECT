using FoodLens.Api.Data;
using FoodLens.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodLens.Api.Services
{
    public class FoodLogRepository : IFoodLogRepository
    {
        private readonly AppDbContext _db;

        public FoodLogRepository(AppDbContext db) => _db = db;

        public async Task<List<FoodLog>> GetByDateAsync(string deviceId, DateTime date)
        {
            var start = date.Date.ToUniversalTime();
            var end = start.AddDays(1);
            return await _db.FoodLogs
                .Where(f => f.DeviceId == deviceId && f.LoggedAt >= start && f.LoggedAt < end)
                .OrderBy(f => f.LoggedAt)
                .ToListAsync();
        }

        public async Task<FoodLog?> GetByIdAsync(int id) =>
            await _db.FoodLogs.FindAsync(id);

        public async Task<FoodLog> CreateAsync(FoodLog log)
        {
            _db.FoodLogs.Add(log);
            await _db.SaveChangesAsync();
            return log;
        }

        public async Task<FoodLog?> UpdateAsync(int id, FoodLog updated)
        {
            var existing = await _db.FoodLogs.FindAsync(id);
            if (existing is null) return null;

            existing.Grams = updated.Grams;
            existing.Calories = updated.Calories;
            existing.ProteinG = updated.ProteinG;
            existing.CarbsG = updated.CarbsG;
            existing.FatG = updated.FatG;

            await _db.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _db.FoodLogs.FindAsync(id);
            if (existing is null) return false;
            _db.FoodLogs.Remove(existing);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
