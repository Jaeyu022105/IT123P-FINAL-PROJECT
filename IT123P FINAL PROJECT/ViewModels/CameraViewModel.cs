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
            try
            {
                // 1. Request Camera Permission
                var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (cameraStatus != PermissionStatus.Granted)
                {
                    cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
                }

                if (cameraStatus != PermissionStatus.Granted)
                {
                    StatusMessage = "Camera permission denied.";
                    await Shell.Current.DisplayAlert("Permission Denied", "Camera permission is required to snap a photo of your food.", "OK");
                    return;
                }

                // 2. Check if Capture is Supported
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    StatusMessage = "Camera capture not supported. Opening photo gallery instead...";
                    await Shell.Current.DisplayAlert("Not Supported", "Camera capture is not supported on this device. Opening gallery instead.", "OK");
                    await PickFromGalleryAsync();
                    return;
                }

                // 3. Snap Photo
                IsBusy = true;
                ShowCandidates = false;
                StatusMessage = "Launching camera...";
                
                FileResult photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo == null)
                {
                    IsBusy = false;
                    StatusMessage = "Photo capture cancelled.";
                    return;
                }

                // 4. Save to cache
                StatusMessage = "Processing captured image...";
                string localFilePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
                using (Stream sourceStream = await photo.OpenReadAsync())
                using (FileStream localFileStream = File.Create(localFilePath))
                {
                    await sourceStream.CopyToAsync(localFileStream);
                }

                CapturedImageSource = ImageSource.FromFile(localFilePath);

                // 5. Run classification
                await ProcessRecognitionAsync(localFilePath);
            }
            catch (Exception ex)
            {
                IsBusy = false;
                StatusMessage = "Failed to capture photo.";
                await Shell.Current.DisplayAlert("Error", $"Photo capture failed: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task PickFromGalleryAsync()
        {
            try
            {
                IsBusy = true;
                ShowCandidates = false;
                StatusMessage = "Opening photo gallery...";

                FileResult photo = await MediaPicker.Default.PickPhotoAsync();
                if (photo == null)
                {
                    IsBusy = false;
                    StatusMessage = "Photo selection cancelled.";
                    return;
                }

                // Save to cache
                StatusMessage = "Processing selected image...";
                string localFilePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
                using (Stream sourceStream = await photo.OpenReadAsync())
                using (FileStream localFileStream = File.Create(localFilePath))
                {
                    await sourceStream.CopyToAsync(localFileStream);
                }

                CapturedImageSource = ImageSource.FromFile(localFilePath);

                // Run classification
                await ProcessRecognitionAsync(localFilePath);
            }
            catch (Exception ex)
            {
                IsBusy = false;
                StatusMessage = "Failed to pick photo.";
                await Shell.Current.DisplayAlert("Error", $"Photo selection failed: {ex.Message}", "OK");
            }
        }

        private async Task ProcessRecognitionAsync(string filePath)
        {
            IsBusy = true;
            ShowCandidates = false;
            StatusMessage = "Identifying food items via LogMeal API...";

            Candidates.Clear();
            var results = await _apiService.ClassifyImageAsync(filePath);

            if (results != null && results.Count > 0)
            {
                foreach (var r in results)
                {
                    Candidates.Add(r);
                }
                ShowCandidates = true;
                StatusMessage = "Recognition complete. Please select the correct food:";
            }
            else
            {
                StatusMessage = "Failed to recognize food. Please search manually.";
                await Shell.Current.DisplayAlert("No Results", "LogMeal could not identify the food in this image. Please search manually using the bar below.", "OK");
            }

            IsBusy = false;
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
