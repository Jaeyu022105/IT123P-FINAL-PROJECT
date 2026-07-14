using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IT123P_FINAL_PROJECT.Models;
using IT123P_FINAL_PROJECT.Services;

namespace IT123P_FINAL_PROJECT.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IDatabaseService _db;
        private readonly IApiService _api;

        public SettingsViewModel(IDatabaseService db, IApiService api)
        {
            _db = db;
            _api = api;
        }

        [ObservableProperty] private double _calorieLimit;
        [ObservableProperty] private double _proteinPercentage;
        [ObservableProperty] private double _carbsPercentage;
        [ObservableProperty] private double _fatPercentage;
        [ObservableProperty] private string _validationMessage = string.Empty;
        [ObservableProperty] private bool _isSuccess;

        [RelayCommand]
        public async Task LoadGoalAsync()
        {
            // Try backend first; fall back to local cache
            var serverGoal = await _api.GetDietGoalAsync();
            if (serverGoal != null)
            {
                // Update local cache to keep it consistent
                await _db.RefreshGoalFromServerAsync(serverGoal);
                CalorieLimit       = serverGoal.DailyCalorieLimit;
                ProteinPercentage  = serverGoal.ProteinPercentage;
                CarbsPercentage    = serverGoal.CarbsPercentage;
                FatPercentage      = serverGoal.FatPercentage;
            }
            else
            {
                // Offline — read from local SQLite cache
                var local = await _db.GetDietGoalAsync();
                CalorieLimit       = local.DailyCalorieLimit;
                ProteinPercentage  = local.ProteinPercentage;
                CarbsPercentage    = local.CarbsPercentage;
                FatPercentage      = local.FatPercentage;
            }

            ValidationMessage = string.Empty;
            IsSuccess = false;
        }

        [RelayCommand]
        public void ApplyPreset(string preset)
        {
            switch (preset.ToLower())
            {
                case "balanced":
                    ProteinPercentage = 30; CarbsPercentage = 40; FatPercentage = 30; break;
                case "highprotein":
                    ProteinPercentage = 40; CarbsPercentage = 30; FatPercentage = 30; break;
                case "lowcarb":
                    ProteinPercentage = 30; CarbsPercentage = 20; FatPercentage = 50; break;
            }
            ValidationMessage = string.Empty;
        }

        [RelayCommand]
        public async Task SaveGoalAsync()
        {
            IsSuccess = false;
            double total = ProteinPercentage + CarbsPercentage + FatPercentage;
            if (Math.Abs(total - 100.0) > 0.01)
            {
                ValidationMessage = $"Macronutrient percentages must total 100%. Currently: {total:F0}%.";
                return;
            }
            if (CalorieLimit <= 0)
            {
                ValidationMessage = "Daily calorie limit must be greater than 0.";
                return;
            }

            var goal = new DietGoal
            {
                Id = 1,
                DailyCalorieLimit = CalorieLimit,
                ProteinPercentage = ProteinPercentage,
                CarbsPercentage = CarbsPercentage,
                FatPercentage = FatPercentage
            };

            // Save locally first (always succeeds)
            await _db.SaveDietGoalAsync(goal);

            // Push to backend (best-effort — failure is silent, local cache remains)
            await _api.SaveDietGoalAsync(goal);

            ValidationMessage = string.Empty;
            IsSuccess = true;
            await Task.Delay(2000);
            IsSuccess = false;
        }
    }
}
