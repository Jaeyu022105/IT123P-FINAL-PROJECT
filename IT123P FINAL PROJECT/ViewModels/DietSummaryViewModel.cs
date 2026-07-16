using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IT123P_FINAL_PROJECT.Models;
using IT123P_FINAL_PROJECT.Services;

namespace IT123P_FINAL_PROJECT.ViewModels
{
    public partial class DietSummaryViewModel : ObservableObject
    {
        private readonly IDatabaseService _db;
        private readonly IApiService _api;

        public DietSummaryViewModel(IDatabaseService db, IApiService api)
        {
            _db = db;
            _api = api;
            LoggedMeals = [];
            SelectedDate = DateTime.Today;

            // Listen for goal updates from chat
            MessagingCenter.Subscribe<ChatViewModel>(this, "DietGoalUpdated", async (sender) =>
            {
                await LoadDataAsync();
            });
        }

        [ObservableProperty] private DateTime _selectedDate;
        [ObservableProperty] private double _calorieLimit;
        [ObservableProperty] private double _calorieUsed;
        [ObservableProperty] private double _calorieRemaining;
        [ObservableProperty] private double _calorieProgress; // 0.0 to 1.0 range
        [ObservableProperty] private double _proteinLimit;
        [ObservableProperty] private double _proteinUsed;
        [ObservableProperty] private double _proteinProgress;
        [ObservableProperty] private double _carbsLimit;
        [ObservableProperty] private double _carbsUsed;
        [ObservableProperty] private double _carbsProgress;
        [ObservableProperty] private double _fatLimit;
        [ObservableProperty] private double _fatUsed;
        [ObservableProperty] private double _fatProgress;
        [ObservableProperty] private bool _isSyncing;
        [ObservableProperty] private bool _hasMeals;

        public ObservableCollection<FoodLogEntry> LoggedMeals { get; }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            IsSyncing = true;

            // 1. Sync diet goals with server
            var serverGoal = await _api.GetDietGoalAsync();
            if (serverGoal != null)
                await _db.RefreshGoalFromServerAsync(serverGoal);

            var goal = serverGoal ?? await _db.GetDietGoalAsync();
            CalorieLimit = goal.DailyCalorieLimit;
            ProteinLimit = (CalorieLimit * (goal.ProteinPercentage / 100.0)) / 4.0;
            CarbsLimit   = (CalorieLimit * (goal.CarbsPercentage / 100.0))  / 4.0;
            FatLimit     = (CalorieLimit * (goal.FatPercentage / 100.0))    / 9.0;

            // 2. Push unsynced logs to server
            await _db.SyncPendingLogsAsync(_api);

            // 3. Grab latest food logs from server and update cache
            var serverLogs = await _api.GetFoodLogsAsync(SelectedDate);
            if (serverLogs.Count > 0 || IsOnline())
                await _db.RefreshLogsFromServerAsync(serverLogs, SelectedDate);

            // 4. Load from local database (acts as offline fallback)
            var meals = await _db.GetFoodLogEntriesAsync(SelectedDate);

            LoggedMeals.Clear();
            double cal = 0, pro = 0, carb = 0, fat = 0;
            foreach (var m in meals)
            {
                LoggedMeals.Add(m);
                cal  += m.Calories;
                pro  += m.Protein;
                carb += m.Carbs;
                fat  += m.Fat;
            }

            CalorieUsed      = cal;
            CalorieRemaining = Math.Max(0, CalorieLimit - CalorieUsed);
            CalorieProgress  = CalorieLimit > 0 ? Math.Min(1.0, CalorieUsed / CalorieLimit) : 0;

            ProteinUsed     = pro;
            ProteinProgress = ProteinLimit > 0 ? Math.Min(1.0, ProteinUsed / ProteinLimit) : 0;

            CarbsUsed     = carb;
            CarbsProgress = CarbsLimit > 0 ? Math.Min(1.0, CarbsUsed / CarbsLimit) : 0;

            FatUsed     = fat;
            FatProgress = FatLimit > 0 ? Math.Min(1.0, FatUsed / FatLimit) : 0;

            HasMeals = LoggedMeals.Count > 0;
            IsSyncing = false;
        }

        [RelayCommand]
        public async Task DeleteLogEntryAsync(FoodLogEntry entry)
        {
            if (entry == null) return;

            // Remove from backend if already synced
            if (entry.IsSynced && entry.ServerId > 0)
                await _api.DeleteFoodLogAsync(entry.ServerId);

            // Make sure it's gone from local db
            await _db.DeleteFoodLogEntryAsync(entry);
            await LoadDataAsync();
        }

        [RelayCommand]
        public async Task NavigateToLogFoodAsync()
        {
            await Shell.Current.GoToAsync("///CameraPage");
        }

        // Helper to check if we are online
        private static bool IsOnline() => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
    }
}
