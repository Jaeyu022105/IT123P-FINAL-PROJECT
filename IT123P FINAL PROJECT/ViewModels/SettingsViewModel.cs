using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IT123P_FINAL_PROJECT.Models;
using IT123P_FINAL_PROJECT.Services;

namespace IT123P_FINAL_PROJECT.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IDatabaseService _databaseService;

        public SettingsViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        [ObservableProperty]
        private double _calorieLimit;

        [ObservableProperty]
        private double _proteinPercentage;

        [ObservableProperty]
        private double _carbsPercentage;

        [ObservableProperty]
        private double _fatPercentage;

        [ObservableProperty]
        private string _validationMessage = string.Empty;

        [ObservableProperty]
        private bool _isSuccess;

        [RelayCommand]
        public async Task LoadGoalAsync()
        {
            var goal = await _databaseService.GetDietGoalAsync();
            CalorieLimit = goal.DailyCalorieLimit;
            ProteinPercentage = goal.ProteinPercentage;
            CarbsPercentage = goal.CarbsPercentage;
            FatPercentage = goal.FatPercentage;
            ValidationMessage = string.Empty;
            IsSuccess = false;
        }

        [RelayCommand]
        public void ApplyPreset(string preset)
        {
            switch (preset.ToLower())
            {
                case "balanced":
                    ProteinPercentage = 30;
                    CarbsPercentage = 40;
                    FatPercentage = 30;
                    break;
                case "highprotein":
                    ProteinPercentage = 40;
                    CarbsPercentage = 30;
                    FatPercentage = 30;
                    break;
                case "lowcarb":
                    ProteinPercentage = 30;
                    CarbsPercentage = 20;
                    FatPercentage = 50;
                    break;
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
                ValidationMessage = $"Macronutrient percentages must total exactly 100%. Currently they total: {total:F0}%.";
                return;
            }

            if (CalorieLimit <= 0)
            {
                ValidationMessage = "Daily calorie limit must be greater than 0.";
                return;
            }

            var updatedGoal = new DietGoal
            {
                Id = 1,
                DailyCalorieLimit = CalorieLimit,
                ProteinPercentage = ProteinPercentage,
                CarbsPercentage = CarbsPercentage,
                FatPercentage = FatPercentage
            };

            await _databaseService.SaveDietGoalAsync(updatedGoal);
            ValidationMessage = string.Empty;
            IsSuccess = true;

            // Wait 2 seconds and turn off success notification
            await Task.Delay(2000);
            IsSuccess = false;
        }
    }
}
