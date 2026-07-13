using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IT123P_FINAL_PROJECT.Models;
using IT123P_FINAL_PROJECT.Services;
using IT123P_FINAL_PROJECT.Views;

namespace IT123P_FINAL_PROJECT.ViewModels
{
    public partial class CameraViewModel : ObservableObject
    {
        private readonly IApiService _apiService;

        public CameraViewModel(IApiService apiService)
        {
            _apiService = apiService;
            Candidates = new ObservableCollection<FoodCandidate>();
            StatusMessage = "Ready to analyze food";
        }

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _statusMessage;

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private bool _showCandidates;

        [ObservableProperty]
        private bool _showViewfinder = true;

        partial void OnShowCandidatesChanged(bool value)
        {
            ShowViewfinder = !value;
        }

        [ObservableProperty]
        private ImageSource? _capturedImageSource;

        public ObservableCollection<FoodCandidate> Candidates { get; }

        [RelayCommand]
        public async Task CapturePhotoAsync()
        {
            IsBusy = true;
            ShowCandidates = false;
            StatusMessage = "Capturing photo...";
            await Task.Delay(800);

            StatusMessage = "Analyzing food details...";
            await Task.Delay(1200);

            // Populate mock results
            Candidates.Clear();
            Candidates.Add(new FoodCandidate
            {
                Name = "Grilled Chicken Breast",
                Confidence = 94.2,
                UsdaFoodId = "USDA_171140",
                CaloriesPer100g = 165,
                ProteinPer100g = 31,
                CarbsPer100g = 0,
                FatPer100g = 3.6
            });
            Candidates.Add(new FoodCandidate
            {
                Name = "Roasted Turkey Breast",
                Confidence = 62.5,
                UsdaFoodId = "USDA_171162",
                CaloriesPer100g = 135,
                ProteinPer100g = 30,
                CarbsPer100g = 0,
                FatPer100g = 1.0
            });
            Candidates.Add(new FoodCandidate
            {
                Name = "Grilled Salmon",
                Confidence = 41.8,
                UsdaFoodId = "USDA_175132",
                CaloriesPer100g = 206,
                ProteinPer100g = 22,
                CarbsPer100g = 0,
                FatPer100g = 12
            });

            ShowCandidates = true;
            IsBusy = false;
            StatusMessage = "Recognition complete. Please select the correct food:";
        }

        [RelayCommand]
        public async Task PickFromGalleryAsync()
        {
            IsBusy = true;
            ShowCandidates = false;
            StatusMessage = "Opening photo gallery...";
            await Task.Delay(800);

            StatusMessage = "Analyzing photo...";
            await Task.Delay(1200);

            // Populate different mock results for variety
            Candidates.Clear();
            Candidates.Add(new FoodCandidate
            {
                Name = "Avocado Toast",
                Confidence = 89.1,
                UsdaFoodId = "USDA_234123",
                CaloriesPer100g = 260,
                ProteinPer100g = 6,
                CarbsPer100g = 25,
                FatPer100g = 16
            });
            Candidates.Add(new FoodCandidate
            {
                Name = "Boiled Egg",
                Confidence = 55.4,
                UsdaFoodId = "USDA_112233",
                CaloriesPer100g = 155,
                ProteinPer100g = 13,
                CarbsPer100g = 1.1,
                FatPer100g = 11
            });
            Candidates.Add(new FoodCandidate
            {
                Name = "French Fries",
                Confidence = 30.2,
                UsdaFoodId = "USDA_445566",
                CaloriesPer100g = 312,
                ProteinPer100g = 3.4,
                CarbsPer100g = 41,
                FatPer100g = 15
            });

            ShowCandidates = true;
            IsBusy = false;
            StatusMessage = "Recognition complete. Please select the correct food:";
        }

        [RelayCommand]
        public async Task ManualSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                StatusMessage = "Please enter a food name to search";
                return;
            }

            IsBusy = true;
            ShowCandidates = false;
            StatusMessage = $"Searching USDA database for \u2018{SearchQuery}\u2019...";

            Candidates.Clear();
            var results = await _apiService.SearchFoodsAsync(SearchQuery.Trim());

            if (results.Count > 0)
            {
                foreach (var r in results)
                    Candidates.Add(r);

                ShowCandidates = true;
                StatusMessage = $"{results.Count} result(s) found for \u2018{SearchQuery}\u2019:";
            }
            else
            {
                // Offline / backend unavailable fallback
                StatusMessage = "Nutrition service unavailable. Showing local fallback.";
                string query = SearchQuery.Trim();
                Candidates.Add(new FoodCandidate
                {
                    Name = $"{query} (estimated)",
                    Confidence = 100.0,
                    FdcId = string.Empty,
                    CaloriesPer100g = 120,
                    ProteinPer100g = 4,
                    CarbsPer100g = 22,
                    FatPer100g = 2
                });
                ShowCandidates = true;
            }

            IsBusy = false;
        }

        [RelayCommand]
        public async Task SelectCandidateAsync(FoodCandidate candidate)
        {
            if (candidate == null) return;

            // Navigate to PortionPage passing the selected candidate
            var navigationParameter = new Dictionary<string, object>
            {
                { "Candidate", candidate }
            };

            await Shell.Current.GoToAsync(nameof(PortionPage), navigationParameter);
        }
    }
}
