using IT123P_FINAL_PROJECT.Models;
using SQLite;

namespace IT123P_FINAL_PROJECT.Services
{
    public class DatabaseService : IDatabaseService
    {
        private SQLiteAsyncConnection? _database;
        private readonly string _dbPath;

        public DatabaseService()
        {
            _dbPath = Path.Combine(FileSystem.AppDataDirectory, "foodlens.db3");
        }

        public async Task InitializeAsync()
        {
            if (_database != null)
                return;

            _database = new SQLiteAsyncConnection(_dbPath, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache);

            await _database.CreateTableAsync<FoodLogEntry>();
            await _database.CreateTableAsync<DietGoal>();

            // Seed a default Diet Goal if none exists
            var existingGoal = await _database.Table<DietGoal>().FirstOrDefaultAsync();
            if (existingGoal == null)
            {
                var defaultGoal = new DietGoal
                {
                    Id = 1, // Only one goal entry needed
                    DailyCalorieLimit = 2000,
                    ProteinPercentage = 30, // 30% Protein
                    CarbsPercentage = 40,   // 40% Carbs
                    FatPercentage = 30      // 30% Fat
                };
                await _database.InsertAsync(defaultGoal);
            }
        }

        public async Task<List<FoodLogEntry>> GetFoodLogEntriesAsync(DateTime date)
        {
            await InitializeAsync();
            var startOfDay = date.Date;
            var endOfDay = date.Date.AddDays(1);

            return await _database!.Table<FoodLogEntry>()
                .Where(e => e.DateLogged >= startOfDay && e.DateLogged < endOfDay)
                .OrderBy(e => e.DateLogged)
                .ToListAsync();
        }

        public async Task<FoodLogEntry?> GetFoodLogEntryAsync(int id)
        {
            await InitializeAsync();
            return await _database!.Table<FoodLogEntry>()
                .Where(e => e.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<int> SaveFoodLogEntryAsync(FoodLogEntry entry)
        {
            await InitializeAsync();
            if (entry.Id != 0)
            {
                return await _database!.UpdateAsync(entry);
            }
            else
            {
                return await _database!.InsertAsync(entry);
            }
        }

        public async Task<int> DeleteFoodLogEntryAsync(FoodLogEntry entry)
        {
            await InitializeAsync();
            return await _database!.DeleteAsync(entry);
        }

        public async Task<DietGoal> GetDietGoalAsync()
        {
            await InitializeAsync();
            var goal = await _database!.Table<DietGoal>().FirstOrDefaultAsync();
            if (goal == null)
            {
                // Fallback in case of database sync/access issue
                return new DietGoal
                {
                    Id = 1,
                    DailyCalorieLimit = 2000,
                    ProteinPercentage = 30,
                    CarbsPercentage = 40,
                    FatPercentage = 30
                };
            }
            return goal;
        }

        public async Task<int> SaveDietGoalAsync(DietGoal goal)
        {
            await InitializeAsync();
            goal.Id = 1; // Enforce single goal ID
            return await _database!.UpdateAsync(goal);
        }
    }
}
