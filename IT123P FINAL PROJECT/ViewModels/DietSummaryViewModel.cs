using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IT123P_FINAL_PROJECT.Models;
using IT123P_FINAL_PROJECT.Services;

namespace IT123P_FINAL_PROJECT.ViewModels
{
    public partial class DietSummaryViewModel : ObservableObject
    {
        private readonly IDatabaseService _databaseService;

        public DietSummaryViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
            LoggedMeals = new ObservableCollection<FoodLogEntry>();
            SelectedDate = DateTime.Today;
        }

        [ObservableProperty]
        private DateTime _selectedDate;

        [ObservableProperty]
        private double _calorieLimit;

        [ObservableProperty]
        private double _calorieUsed;

        [ObservableProperty]
        private double _calorieRemaining;

        [ObservableProperty]
        private double _calorieProgress; // 0.0 to 1.0

        [ObservableProperty]
        private double _proteinLimit; // grams

        [ObservableProperty]
        private double _proteinUsed;

        [ObservableProperty]
        private double _proteinProgress;

        [ObservableProperty]
        private double _carbsLimit; // grams

        [ObservableProperty]
        private double _carbsUsed;

        [ObservableProperty]
        private double _carbsProgress;

        [ObservableProperty]
        private double _fatLimit; // grams

        [ObservableProperty]
        private double _fatUsed;

        [ObservableProperty]
        private double _fatProgress;

        public ObservableCollection<FoodLogEntry> LoggedMeals { get; }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            // 1. Get Diet Goal
            var goal = await _databaseService.GetDietGoalAsync();
            CalorieLimit = goal.DailyCalorieLimit;

            // Calculate macro limits in grams
            // Protein: 4 kcal per gram
            ProteinLimit = (CalorieLimit * (goal.ProteinPercentage / 100.0)) / 4.0;
            // Carbs: 4 kcal per gram
            CarbsLimit = (CalorieLimit * (goal.CarbsPercentage / 100.0)) / 4.0;
            // Fat: 9 kcal per gram
            FatLimit = (CalorieLimit * (goal.FatPercentage / 100.0)) / 9.0;

            // 2. Get logged meals for SelectedDate
            var meals = await _databaseService.GetFoodLogEntriesAsync(SelectedDate);
            
            LoggedMeals.Clear();
            double totalCalories = 0;
            double totalProtein = 0;
            double totalCarbs = 0;
            double totalFat = 0;

            foreach (var meal in meals)
            {
                LoggedMeals.Add(meal);
                totalCalories += meal.Calories;
                totalProtein += meal.Protein;
                totalCarbs += meal.Carbs;
                totalFat += meal.Fat;
            }

            // Update status properties
            CalorieUsed = totalCalories;
            CalorieRemaining = Math.Max(0, CalorieLimit - CalorieUsed);
            CalorieProgress = CalorieLimit > 0 ? Math.Min(1.0, CalorieUsed / CalorieLimit) : 0;

            ProteinUsed = totalProtein;
            ProteinProgress = ProteinLimit > 0 ? Math.Min(1.0, ProteinUsed / ProteinLimit) : 0;

            CarbsUsed = totalCarbs;
            CarbsProgress = CarbsLimit > 0 ? Math.Min(1.0, CarbsUsed / CarbsLimit) : 0;

            FatUsed = totalFat;
            FatProgress = FatLimit > 0 ? Math.Min(1.0, FatUsed / FatLimit) : 0;
        }

        [RelayCommand]
        public async Task DeleteLogEntryAsync(FoodLogEntry entry)
        {
            if (entry == null) return;
            await _databaseService.DeleteFoodLogEntryAsync(entry);
            await LoadDataAsync(); // Reload
        }

        [RelayCommand]
        public async Task NavigateToLogFoodAsync()
        {
            // Switch tab to CameraPage / Log Food
            await Shell.Current.GoToAsync("///CameraPage");
        }
    }
}
