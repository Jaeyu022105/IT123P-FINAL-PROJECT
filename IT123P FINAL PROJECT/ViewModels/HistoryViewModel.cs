using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IT123P_FINAL_PROJECT.Models;
using IT123P_FINAL_PROJECT.Services;

namespace IT123P_FINAL_PROJECT.ViewModels
{
    public partial class HistoryViewModel : ObservableObject
    {
        private readonly IDatabaseService _db;
        private readonly IApiService _api;

        public HistoryViewModel(IDatabaseService db, IApiService api)
        {
            _db  = db;
            _api = api;
            WeeklyBars  = [];
            SelectedDayMeals = [];
            _selectedDay = DateTime.Today;
        }

        // State properties
        [ObservableProperty] private bool   _isLoading;
        [ObservableProperty] private bool   _hasData;
        [ObservableProperty] private double _averageCalories;
        [ObservableProperty] private string _bestDayText    = "-";
        [ObservableProperty] private string _worstDayText   = "-";
        [ObservableProperty] private string _selectedDayHeader = "Today";
        [ObservableProperty] private DateTime _selectedDay;

        // Width values from 0 to 1 for the pill chart
        [ObservableProperty] private double _proteinFrac = 0.33;
        [ObservableProperty] private double _carbsFrac   = 0.34;
        [ObservableProperty] private double _fatFrac     = 0.33;

        // 7-day window list for the bar chart
        public ObservableCollection<BarEntry> WeeklyBars { get; }

        // Log entries for the selected day
        public ObservableCollection<FoodLogEntry> SelectedDayMeals { get; }

        // Represents a single bar
        public partial class BarEntry : ObservableObject
        {
            public DateTime Date { get; init; }
            public string DayLabel  { get; init; } = string.Empty;
            public string DateLabel { get; init; } = string.Empty;
            public double Calories  { get; init; }
            public string CalLabel  => Calories > 0 ? $"{Calories:F0}" : "-";
            [ObservableProperty] private bool _isSelected;

            // Bar height in pixels
            public double BarHeight { get; init; }

            // Bar color interpolated between plum and berry
            public string BarColor  { get; init; } = ThemeColors.Plum;
        }

        // UI commands
        [RelayCommand]
        public async Task LoadWeekAsync()
        {
            IsLoading = true;

            // Find date range for last 7 days
            var days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Today.AddDays(-(6 - i)))
                .ToList();

            // Fetch daily nutrition sums
            var totals = new List<DayNutritionTotal>();
            foreach (var day in days)
            {
                // Query backend first, then local sqlite db
                var serverLogs = await _api.GetFoodLogsAsync(day);
                List<FoodLogEntry> entries;
                if (serverLogs.Count > 0)
                {
                    await _db.RefreshLogsFromServerAsync(serverLogs, day);
                    entries = serverLogs;
                }
                else
                {
                    entries = await _db.GetFoodLogEntriesAsync(day);
                }

                totals.Add(new DayNutritionTotal(
                    Date:      day,
                    Calories:  entries.Sum(e => e.Calories),
                    Protein:   entries.Sum(e => e.Protein),
                    Carbs:     entries.Sum(e => e.Carbs),
                    Fat:       entries.Sum(e => e.Fat),
                    MealCount: entries.Count));
            }

            var summary = new WeeklyNutritionSummary(totals);
            HasData = summary.TotalCalories > 0;

            // Calculate stats
            AverageCalories = Math.Round(summary.AverageCalories, 0);
            BestDayText  = summary.BestDay  is { } b ? $"{b.DayLabel}\n{b.Calories:F0} kcal" : "-";
            WorstDayText = summary.WorstDay is { } w ? $"{w.DayLabel}\n{w.Calories:F0} kcal" : "-";

            // Compute macro ratios
            var (pf, cf, ff) = summary.MacroSplit();
            ProteinFrac = pf;
            CarbsFrac   = cf;
            FatFrac     = ff;

            // Generate bar chart entries
            const double MaxBarHeight = 160.0;
            WeeklyBars.Clear();
            foreach (var t in totals)
            {
                double norm = summary.NormalizedHeight(t);
                // Interpolate between colors
                string color = InterpolateColor(norm);
                WeeklyBars.Add(new BarEntry
                {
                    Date      = t.Date,
                    DayLabel  = t.DayLabel,
                    DateLabel = t.DateLabel,
                    Calories  = t.Calories,
                    BarHeight = Math.Max(6, norm * MaxBarHeight),
                    BarColor  = color,
                    IsSelected = t.Date.Date == SelectedDay.Date
                });
            }

            // Drill down into daily meals
            await LoadDayMealsAsync(SelectedDay);

            IsLoading = false;
        }

        [RelayCommand]
        public async Task SelectDayAsync(BarEntry? bar)
        {
            if (bar is null) return;
            SelectedDay = bar.Date;

            // Set high/low highlights
            foreach (var b in WeeklyBars)
                b.IsSelected = b.Date.Date == bar.Date.Date;

            await LoadDayMealsAsync(bar.Date);
        }

        // Helper methods
        private async Task LoadDayMealsAsync(DateTime day)
        {
            SelectedDayHeader = day.Date == DateTime.Today
                ? "Today's Meals"
                : $"{day:dddd, MMMM d}";

            var meals = await _db.GetFoodLogEntriesAsync(day);
            SelectedDayMeals.Clear();
            foreach (var m in meals)
                SelectedDayMeals.Add(m);
        }

        // Linear interpolation between colors
        private static string InterpolateColor(double t)
        {
            // Map low value to plum and high to berry
            int r = (int)(114 + (185 - 114) * t);
            int g = (int)(90  + (80  - 90)  * t);
            int b = (int)(122 + (90  - 122) * t);
            return $"#{r:X2}{g:X2}{b:X2}";
        }
    }
}
