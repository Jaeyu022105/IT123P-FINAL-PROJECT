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
            if (_database != null) return;

            _database = new SQLiteAsyncConnection(_dbPath,
                SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache);

            await _database.CreateTableAsync<FoodLogEntry>();
            await _database.CreateTableAsync<DietGoal>();

            // Seed a default Diet Goal if none exists
            var existingGoal = await _database.Table<DietGoal>().FirstOrDefaultAsync();
            if (existingGoal == null)
            {
                await _database.InsertAsync(new DietGoal
                {
                    Id = 1,
                    DailyCalorieLimit = 2000,
                    ProteinPercentage = 30,
                    CarbsPercentage = 40,
                    FatPercentage = 30
                });
            }
        }

        // ── Food Logs ─────────────────────────────────────────────────────────

        public async Task<List<FoodLogEntry>> GetFoodLogEntriesAsync(DateTime date)
        {
            await InitializeAsync();
            var start = date.Date;
            var end   = date.Date.AddDays(1);
            return await _database!.Table<FoodLogEntry>()
                .Where(e => e.DateLogged >= start && e.DateLogged < end)
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
            return entry.Id != 0
                ? await _database!.UpdateAsync(entry)
                : await _database!.InsertAsync(entry);
        }

        public async Task<int> DeleteFoodLogEntryAsync(FoodLogEntry entry)
        {
            await InitializeAsync();
            return await _database!.DeleteAsync(entry);
        }

        // ── Diet Goal ─────────────────────────────────────────────────────────

        public async Task<DietGoal> GetDietGoalAsync()
        {
            await InitializeAsync();
            var goal = await _database!.Table<DietGoal>().FirstOrDefaultAsync();
            return goal ?? new DietGoal
            {
                Id = 1,
                DailyCalorieLimit = 2000,
                ProteinPercentage = 30,
                CarbsPercentage = 40,
                FatPercentage = 30
            };
        }

        public async Task<int> SaveDietGoalAsync(DietGoal goal)
        {
            await InitializeAsync();
            goal.Id = 1; // Enforce single goal row
            return await _database!.UpdateAsync(goal);
        }

        // ── Sync Helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// Uploads any locally-created entries that haven't been pushed to the backend yet.
        /// Marks each as IsSynced = true and records the backend ServerId on success.
        /// Call this before fetching the authoritative list from the server.
        /// </summary>
        public async Task SyncPendingLogsAsync(IApiService apiService)
        {
            await InitializeAsync();
            var pending = await _database!.Table<FoodLogEntry>()
                .Where(e => !e.IsSynced)
                .ToListAsync();

            foreach (var entry in pending)
            {
                int serverId = await apiService.CreateFoodLogAsync(entry);
                if (serverId > 0)
                {
                    entry.ServerId = serverId;
                    entry.IsSynced = true;
                    await _database!.UpdateAsync(entry);
                }
            }
        }

        /// <summary>
        /// Replaces local food log entries for the given date with the authoritative
        /// server list. Any locally-unsynced entries that survived SyncPendingLogsAsync
        /// are preserved (they failed to upload, so we keep them for the next attempt).
        /// </summary>
        public async Task RefreshLogsFromServerAsync(List<FoodLogEntry> serverEntries, DateTime date)
        {
            await InitializeAsync();

            // Delete all locally-synced entries for the date (server is the source of truth)
            var start = date.Date;
            var end   = date.Date.AddDays(1);
            var local = await _database!.Table<FoodLogEntry>()
                .Where(e => e.DateLogged >= start && e.DateLogged < end && e.IsSynced)
                .ToListAsync();

            foreach (var old in local)
                await _database!.DeleteAsync(old);

            // Insert fresh server records
            foreach (var entry in serverEntries)
                await _database!.InsertAsync(entry);
        }

        /// <summary>
        /// Overwrites the local diet goal with data fetched from the server.
        /// </summary>
        public async Task RefreshGoalFromServerAsync(DietGoal serverGoal)
        {
            await InitializeAsync();
            serverGoal.Id = 1;
            var existing = await _database!.Table<DietGoal>().FirstOrDefaultAsync();
            if (existing is null)
                await _database!.InsertAsync(serverGoal);
            else
                await _database!.UpdateAsync(serverGoal);
        }
    }
}
