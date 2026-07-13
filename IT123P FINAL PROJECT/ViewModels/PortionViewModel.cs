using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IT123P_FINAL_PROJECT.Models;
using IT123P_FINAL_PROJECT.Services;

namespace IT123P_FINAL_PROJECT.ViewModels
{
    [QueryProperty(nameof(Candidate), "Candidate")]
    public partial class PortionViewModel : ObservableObject
    {
        private readonly IDatabaseService _databaseService;

        public PortionViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
            _grams = 150; // Default portion
        }

        [ObservableProperty]
        private FoodCandidate? _candidate;

        [ObservableProperty]
        private double _grams;

        [ObservableProperty]
        private double _calories;

        [ObservableProperty]
        private double _protein;

        [ObservableProperty]
        private double _carbs;

        [ObservableProperty]
        private double _fat;

        [ObservableProperty]
        private double _remainingCaloriesBefore;

        [ObservableProperty]
        private double _remainingCaloriesAfter;

        [ObservableProperty]
        private bool _fitsDiet;

        [ObservableProperty]
        private string _fitsDietText = string.Empty;

        [ObservableProperty]
        private string _fitsDietColor = "#4CAF50"; // Green default

        private double _dailyLimit;
        private double _todayLoggedCalories;

        partial void OnCandidateChanged(FoodCandidate? value)
        {
            UpdateNutrition();
        }

        partial void OnGramsChanged(double value)
        {
            UpdateNutrition();
        }

        [RelayCommand]
        public async Task LoadBudgetAsync()
        {
            // Fetch today's current logs and goal
            var goal = await _databaseService.GetDietGoalAsync();
            _dailyLimit = goal.DailyCalorieLimit;

            var entries = await _databaseService.GetFoodLogEntriesAsync(DateTime.Today);
            _todayLoggedCalories = 0;
            foreach (var entry in entries)
            {
                _todayLoggedCalories += entry.Calories;
            }

            RemainingCaloriesBefore = Math.Max(0, _dailyLimit - _todayLoggedCalories);
            UpdateNutrition();
        }

        private void UpdateNutrition()
        {
            if (Candidate == null) return;

            // Scale per 100g
            double factor = Grams / 100.0;
            Calories = Candidate.CaloriesPer100g * factor;
            Protein = Candidate.ProteinPer100g * factor;
            Carbs = Candidate.CarbsPer100g * factor;
            Fat = Candidate.FatPer100g * factor;

            double projectedTotal = _todayLoggedCalories + Calories;
            double remaining = _dailyLimit - projectedTotal;

            if (remaining >= 0)
            {
                FitsDiet = true;
                RemainingCaloriesAfter = remaining;
                FitsDietText = $"Fits your remaining budget! ({remaining:F0} kcal left)";
                FitsDietColor = "#2E7D32"; // Darker, rich green
            }
            else
            {
                FitsDiet = false;
                RemainingCaloriesAfter = 0;
                double overage = projectedTotal - _dailyLimit;
                FitsDietText = $"Over budget! Exceeds daily goal by {overage:F0} kcal ⚠️";
                FitsDietColor = "#D84315"; // Rich amber/red-orange
            }
        }

        [RelayCommand]
        public void SelectPreset(string portionType)
        {
            Grams = portionType.ToLower() switch
            {
                "small" => 100,
                "medium" => 200,
                "large" => 350,
                _ => 150
            };
        }

        [RelayCommand]
        public async Task SaveLogEntryAsync()
        {
            if (Candidate == null) return;

            var newEntry = new FoodLogEntry
            {
                FoodName = Candidate.Name,
                Calories = Math.Round(Calories, 1),
                Protein = Math.Round(Protein, 1),
                Carbs = Math.Round(Carbs, 1),
                Fat = Math.Round(Fat, 1),
                Grams = Math.Round(Grams, 1),
                DateLogged = DateTime.Now
            };

            await _databaseService.SaveFoodLogEntryAsync(newEntry);

            // Pop navigation back to home page root
            await Shell.Current.GoToAsync("///MainPage");
        }

        [RelayCommand]
        public async Task CancelAsync()
        {
            // Back one step in navigation stack
            await Shell.Current.GoToAsync("..");
        }
    }
}
