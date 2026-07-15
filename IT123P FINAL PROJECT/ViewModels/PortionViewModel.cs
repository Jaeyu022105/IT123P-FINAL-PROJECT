using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IT123P_FINAL_PROJECT.Models;
using IT123P_FINAL_PROJECT.Services;

namespace IT123P_FINAL_PROJECT.ViewModels
{
    [QueryProperty(nameof(Candidate), "Candidate")]
    public partial class PortionViewModel : ObservableObject
    {
        private readonly IDatabaseService _db;
        private readonly IApiService _api;

        private double _dailyLimit;
        private double _todayLoggedCalories;

        public PortionViewModel(IDatabaseService db, IApiService api)
        {
            _db = db;
            _api = api;
            _grams = 150; // Sensible default portion
        }

        [ObservableProperty] private FoodCandidate? _candidate;
        [ObservableProperty] private double _grams;
        [ObservableProperty] private double _calories;
        [ObservableProperty] private double _protein;
        [ObservableProperty] private double _carbs;
        [ObservableProperty] private double _fat;
        [ObservableProperty] private double _remainingCaloriesBefore;
        [ObservableProperty] private double _remainingCaloriesAfter;
        [ObservableProperty] private double _projectedCalories;
        [ObservableProperty] private double _projectedProgress;
        [ObservableProperty] private bool _fitsDiet;
        [ObservableProperty] private string _fitsDietText = string.Empty;
        [ObservableProperty] private string _fitsDietColor = ThemeColors.Leaf;

        partial void OnCandidateChanged(FoodCandidate? value) => _ = RefreshNutritionAsync();
        partial void OnGramsChanged(double value)             => _ = RefreshNutritionAsync();

        // ── Load today's budget ───────────────────────────────────────────────

        [RelayCommand]
        public async Task LoadBudgetAsync()
        {
            // Try to get the goal from backend; fall back to local
            var serverGoal = await _api.GetDietGoalAsync();
            var goal = serverGoal ?? await _db.GetDietGoalAsync();
            _dailyLimit = goal.DailyCalorieLimit;

            var entries = await _db.GetFoodLogEntriesAsync(DateTime.Today);
            _todayLoggedCalories = entries.Sum(e => e.Calories);

            RemainingCaloriesBefore = Math.Max(0, _dailyLimit - _todayLoggedCalories);
            await RefreshNutritionAsync();
        }

        // ── Nutrition refresh + backend comparison ────────────────────────────

        /// <summary>
        /// Gets real scaled nutrition (from USDA via backend when FdcId available),
        /// then calls the backend comparison endpoint to determine budget fitness.
        /// Falls back to local calculation throughout when offline.
        /// </summary>
        private async Task RefreshNutritionAsync()
        {
            if (Candidate == null) return;

            // 1. Get scaled nutrition
            if (!string.IsNullOrWhiteSpace(Candidate.FdcId))
            {
                var result = await _api.GetNutritionAsync(Candidate.FdcId, Grams);
                if (result.HasValue)
                {
                    Calories = result.Value.Calories;
                    Protein  = result.Value.ProteinG;
                    Carbs    = result.Value.CarbsG;
                    Fat      = result.Value.FatG;
                    await UpdateBudgetIndicatorAsync();
                    return;
                }
            }

            // Offline fallback: scale from per-100g values
            double factor = Grams / 100.0;
            Calories = Candidate.CaloriesPer100g * factor;
            Protein  = Candidate.ProteinPer100g  * factor;
            Carbs    = Candidate.CarbsPer100g    * factor;
            Fat      = Candidate.FatPer100g      * factor;

            await UpdateBudgetIndicatorAsync();
        }

        /// <summary>
        /// Asks the backend /api/diet/compare to evaluate the current portion
        /// against today's running total. Falls back to local math when offline.
        /// </summary>
        private async Task UpdateBudgetIndicatorAsync()
        {
            var comparison = await _api.CompareDietAsync(Calories, _todayLoggedCalories);

            bool fits;
            double remaining;
            string message;

            if (comparison != null)
            {
                fits      = comparison.FitsDiet;
                remaining = comparison.RemainingAfter;
                message   = comparison.Message;
                // Also keep local limit up-to-date in case it changed on server
                _dailyLimit = comparison.DailyLimit;
            }
            else
            {
                // Offline — compute locally
                double projected = _todayLoggedCalories + Calories;
                fits      = projected <= _dailyLimit;
                remaining = Math.Max(0, _dailyLimit - projected);
                message   = fits
                    ? $"Fits your budget! ({remaining:F0} kcal left)"
                    : $"Over budget by {projected - _dailyLimit:F0} kcal ⚠️";
            }

            FitsDiet             = fits;
            RemainingCaloriesAfter = remaining;
            ProjectedCalories    = _todayLoggedCalories + Calories;
            ProjectedProgress    = _dailyLimit > 0 ? Math.Min(1.0, ProjectedCalories / _dailyLimit) : 0;
            FitsDietText         = fits
                ? $"Fits your remaining budget! ({remaining:F0} kcal left)"
                : message;
            FitsDietColor        = fits ? ThemeColors.Forest : ThemeColors.Danger;
        }

        // ── Preset portions ───────────────────────────────────────────────────

        [RelayCommand]
        public void SelectPreset(string portionType)
        {
            Grams = portionType.ToLower() switch
            {
                "small"  => 100,
                "medium" => 200,
                "large"  => 350,
                _        => 150
            };
        }

        // ── Save log entry ────────────────────────────────────────────────────

        [RelayCommand]
        public async Task SaveLogEntryAsync()
        {
            if (Candidate == null) return;

            var entry = new FoodLogEntry
            {
                FoodName   = Candidate.Name,
                Calories   = Math.Round(Calories, 1),
                Protein    = Math.Round(Protein, 1),
                Carbs      = Math.Round(Carbs, 1),
                Fat        = Math.Round(Fat, 1),
                Grams      = Math.Round(Grams, 1),
                DateLogged = DateTime.Now,
                IsSynced   = false   // will be set to true after successful backend push
            };

            // 1. Save locally first (always succeeds)
            await _db.SaveFoodLogEntryAsync(entry);

            // 2. Try to push to backend (don't block navigation on failure)
            try
            {
                int serverId = await _api.CreateFoodLogAsync(entry);
                if (serverId > 0)
                {
                    entry.ServerId = serverId;
                    entry.IsSynced = true;
                    await _db.SaveFoodLogEntryAsync(entry); // update with ServerId
                }
            }
            catch
            {
                // Swallow – entry is saved locally; will sync later
            }

            MessagingCenter.Send(this, "FoodLogSaved");

            // Leave the portion screen first so the Log Food tab returns to a clean root page.
            await Shell.Current.GoToAsync("..");
            await Shell.Current.GoToAsync("///DietSummaryPage");
        }

        [RelayCommand]
        public async Task CancelAsync() => await Shell.Current.GoToAsync("..");
    }
}
