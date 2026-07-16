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
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsBalancedSelected))]
        [NotifyPropertyChangedFor(nameof(IsHighProteinSelected))]
        [NotifyPropertyChangedFor(nameof(IsLowCarbSelected))]
        private double _proteinPercentage;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsBalancedSelected))]
        [NotifyPropertyChangedFor(nameof(IsHighProteinSelected))]
        [NotifyPropertyChangedFor(nameof(IsLowCarbSelected))]
        private double _carbsPercentage;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsBalancedSelected))]
        [NotifyPropertyChangedFor(nameof(IsHighProteinSelected))]
        [NotifyPropertyChangedFor(nameof(IsLowCarbSelected))]
        private double _fatPercentage;

        [ObservableProperty] private string _validationMessage = string.Empty;
        [ObservableProperty] private bool _isSuccess;

        public bool IsBalancedSelected => Math.Abs(ProteinPercentage - 30) < 0.1 && Math.Abs(CarbsPercentage - 40) < 0.1 && Math.Abs(FatPercentage - 30) < 0.1;
        public bool IsHighProteinSelected => Math.Abs(ProteinPercentage - 40) < 0.1 && Math.Abs(CarbsPercentage - 30) < 0.1 && Math.Abs(FatPercentage - 30) < 0.1;
        public bool IsLowCarbSelected => Math.Abs(ProteinPercentage - 30) < 0.1 && Math.Abs(CarbsPercentage - 20) < 0.1 && Math.Abs(FatPercentage - 50) < 0.1;

        [RelayCommand]
        public async Task LoadGoalAsync()
        {
            // Check backend first, otherwise fall back to local db
            var serverGoal = await _api.GetDietGoalAsync();
            if (serverGoal != null)
            {
                // Keep local db in sync with server
                await _db.RefreshGoalFromServerAsync(serverGoal);
                CalorieLimit       = serverGoal.DailyCalorieLimit;
                ProteinPercentage  = serverGoal.ProteinPercentage;
                CarbsPercentage    = serverGoal.CarbsPercentage;
                FatPercentage      = serverGoal.FatPercentage;
            }
            else
            {
                // Offline mode: load from SQLite cache
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
        public async Task ApplyPresetAsync(string preset)
        {
            bool isCurrent = false;
            switch (preset.ToLower())
            {
                case "balanced":
                    isCurrent = IsBalancedSelected; break;
                case "highprotein":
                    isCurrent = IsHighProteinSelected; break;
                case "lowcarb":
                    isCurrent = IsLowCarbSelected; break;
            }

            string name = preset.ToLower() switch
            {
                "balanced" => "Balanced",
                "highprotein" => "High Protein",
                "lowcarb" => "Low Carb",
                _ => preset
            };

            if (isCurrent)
            {
                await Shell.Current.DisplayAlert("Preset Active", $"The {name} preset is already applied.", "OK");
                return;
            }

            // Ask user before we override their numbers
            bool confirm = await Shell.Current.DisplayAlert(
                "Apply Preset?",
                $"This will override your current settings and save the {name} targets immediately. Do you want to continue?",
                "Yes",
                "No");

            if (!confirm) return;

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

            // Automatically save settings
            await SaveGoalAsync();

            await Shell.Current.DisplayAlert("Preset Applied", $"The {name} preset has been successfully applied and saved.", "OK");
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

            // Save to local cache first
            await _db.SaveDietGoalAsync(goal);

            // Try to sync with server, but ignore if offline
            await _api.SaveDietGoalAsync(goal);

            ValidationMessage = string.Empty;
            IsSuccess = true;
            await Task.Delay(2000);
            IsSuccess = false;
        }

        [ObservableProperty] private bool _isExporting;
        [ObservableProperty] private string _exportStatus = string.Empty;

        [RelayCommand]
        public async Task ExportLogsAsync()
        {
            IsExporting = true;
            ExportStatus = "Initiating SOAP XML export...";

            try
            {
                // Grab the last 7 days of logs
                var fromDate = DateTime.Today.AddDays(-7);
                var toDate = DateTime.Today;

                var result = await _api.ExportDietLogAsync(fromDate, toDate);

                if (string.IsNullOrEmpty(result))
                {
                    ExportStatus = "Export failed.";
                    await Shell.Current.DisplayAlert("Export Failed", "Could not connect to the legacy SOAP export service.", "OK");
                }
                else if (result.StartsWith("Error:"))
                {
                    ExportStatus = "Export failed.";
                    var errMsg = result.Substring(6).Trim();

                    // Try parsing the error JSON
                    string cleanMsg = errMsg;
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(errMsg);
                        if (doc.RootElement.TryGetProperty("error", out var err) && err.TryGetProperty("message", out var msg))
                        {
                            cleanMsg = msg.GetString() ?? cleanMsg;
                        }
                    }
                    catch { }

                    await Shell.Current.DisplayAlert("Export Failed", cleanMsg, "OK");
                }
                else
                {
                    ExportStatus = "Export succeeded!";

                    // Show them the raw WCF SOAP XML just in case
                    await Shell.Current.DisplayAlert("Export Succeeded", 
                        $"Successfully exported diet logs to the legacy system!\n\nRaw SOAP-XML:\n\n{result}", "OK");
                }
            }
            catch (Exception ex)
            {
                ExportStatus = "Export failed.";
                await Shell.Current.DisplayAlert("Error", $"An unexpected error occurred: {ex.Message}", "OK");
            }
            finally
            {
                IsExporting = false;
                await Task.Delay(3000);
                if (ExportStatus == "Export succeeded!" || ExportStatus == "Export failed.")
                {
                    ExportStatus = string.Empty;
                }
            }
        }
    }
}
