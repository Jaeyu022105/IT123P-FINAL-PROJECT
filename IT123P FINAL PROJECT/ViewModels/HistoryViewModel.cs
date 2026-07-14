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

        // ── Observable State ──────────────────────────────────────────────────

        [ObservableProperty] private bool   _isLoading;
        [ObservableProperty] private bool   _hasData;
        [ObservableProperty] private double _averageCalories;
        [ObservableProperty] private string _bestDayText    = "–";
        [ObservableProperty] private string _worstDayText   = "–";
        [ObservableProperty] private string _selectedDayHeader = "Today";
        [ObservableProperty] private DateTime _selectedDay;

        // Macro split widths (0..1) for the pill chart
        [ObservableProperty] private double _proteinFrac = 0.33;
        [ObservableProperty] private double _carbsFrac   = 0.34;
        [ObservableProperty] private double _fatFrac     = 0.33;

        /// <summary>One item per day in the 7-day window, for bar chart rendering.</summary>
        public ObservableCollection<BarEntry> WeeklyBars { get; }

        /// <summary>Meals for the currently selected day (drill-down).</summary>
        public ObservableCollection<FoodLogEntry> SelectedDayMeals { get; }

        // ── Bar Chart Entry ───────────────────────────────────────────────────

        /// <summary>Single bar in the weekly chart — fully bindable.</summary>
        public partial class BarEntry : ObservableObject
        {
            public DateTime Date { get; init; }
            public string DayLabel  { get; init; } = string.Empty;
            public string DateLabel { get; init; } = string.Empty;
            public double Calories  { get; init; }
            public string CalLabel  => Calories > 0 ? $"{Calories:F0}" : "–";
            [ObservableProperty] private bool _isSelected;
            /// <summary>Pixel height for the bar (max 160px).</summary>
            public double BarHeight { get; init; }
            /// <summary>Color string for the bar fill — interpolated purple→pink.</summary>
            public string BarColor  { get; init; } = "#6C4AB6";
        }

        // ── Commands ──────────────────────────────────────────────────────────

        [RelayCommand]
        public async Task LoadWeekAsync()
        {
            IsLoading = true;

            // Build date window: last 7 days ending today
            var days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Today.AddDays(-(6 - i)))
                .ToList();

            // Collect per-day totals (local DB; after Phase 4 sync already ran on DietSummary load)
            var totals = new List<DayNutritionTotal>();
            foreach (var day in days)
            {
                // Try server for each day, fall back to local
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

            // ── Summary stats ──────────────────────────────────────────────
            AverageCalories = Math.Round(summary.AverageCalories, 0);
            BestDayText  = summary.BestDay  is { } b ? $"{b.DayLabel}\n{b.Calories:F0} kcal" : "–";
            WorstDayText = summary.WorstDay is { } w ? $"{w.DayLabel}\n{w.Calories:F0} kcal" : "–";

            // ── Macro split ────────────────────────────────────────────────
            var (pf, cf, ff) = summary.MacroSplit();
            ProteinFrac = pf;
            CarbsFrac   = cf;
            FatFrac     = ff;

            // ── Bar chart entries ──────────────────────────────────────────
            const double MaxBarHeight = 160.0;
            WeeklyBars.Clear();
            foreach (var t in totals)
            {
                double norm = summary.NormalizedHeight(t);
                // Interpolate color: low = #6C4AB6 (purple), high = #F06292 (pink)
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

            // ── Drill-down for selected day ────────────────────────────────
            await LoadDayMealsAsync(SelectedDay);

            IsLoading = false;
        }

        [RelayCommand]
        public async Task SelectDayAsync(BarEntry? bar)
        {
            if (bar is null) return;
            SelectedDay = bar.Date;

            // Update selection highlight on all bars
            foreach (var b in WeeklyBars)
                b.IsSelected = b.Date.Date == bar.Date.Date;

            await LoadDayMealsAsync(bar.Date);
        }

        // ── Private Helpers ───────────────────────────────────────────────────

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

        /// <summary>
        /// Linear interpolation between purple (#6C4AB6) and pink (#F06292) based on
        /// a normalized 0–1 intensity value. Returns a hex colour string.
        /// </summary>
        private static string InterpolateColor(double t)
        {
            // Low:  R=108 G=74  B=182
            // High: R=240 G=98  B=146
            int r = (int)(108 + (240 - 108) * t);
            int g = (int)(74  + (98  - 74)  * t);
            int b = (int)(182 + (146 - 182) * t);
            return $"#{r:X2}{g:X2}{b:X2}";
        }
    }
}
