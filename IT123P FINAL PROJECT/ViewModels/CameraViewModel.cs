using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IT123P_FINAL_PROJECT.Models;
using IT123P_FINAL_PROJECT.Services;
using IT123P_FINAL_PROJECT.Views;
using Microsoft.Maui.Graphics;

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
                // Need camera permission first
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

                // Check if camera capture works on this device
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    StatusMessage = "Camera capture not supported. Opening photo gallery instead...";
                    await Shell.Current.DisplayAlert("Not Supported", "Camera capture is not supported on this device. Opening gallery instead.", "OK");
                    await PickFromGalleryAsync();
                    return;
                }

                // Try to capture the photo
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

                // Save to local cache folder as JPEG
                StatusMessage = "Processing captured image...";
                string localFilePath = Path.Combine(FileSystem.CacheDirectory, Path.ChangeExtension(photo.FileName, ".jpg"));
                using (Stream sourceStream = await photo.OpenReadAsync())
                {
                    var image = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(sourceStream);
                    using (FileStream localFileStream = File.Create(localFilePath))
                    {
                        image.Save(localFileStream, ImageFormat.Jpeg);
                    }
                }

                CapturedImageSource = ImageSource.FromFile(localFilePath);

                // Send image to classifier
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

                // Save photo to local cache folder as JPEG
                StatusMessage = "Processing selected image...";
                string localFilePath = Path.Combine(FileSystem.CacheDirectory, Path.ChangeExtension(photo.FileName, ".jpg"));
                using (Stream sourceStream = await photo.OpenReadAsync())
                {
                    var image = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(sourceStream);
                    using (FileStream localFileStream = File.Create(localFilePath))
                    {
                        image.Save(localFileStream, ImageFormat.Jpeg);
                    }
                }

                CapturedImageSource = ImageSource.FromFile(localFilePath);

                // Send picked image to classifier
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
            StatusMessage = $"Searching USDA database for '{SearchQuery}'...";

            Candidates.Clear();
            var results = await _apiService.SearchFoodsAsync(SearchQuery.Trim());

            if (results.Count > 0)
            {
                foreach (var r in results)
                    Candidates.Add(r);

                ShowCandidates = true;
                StatusMessage = $"{results.Count} result(s) found for '{SearchQuery}':";
            }
            else
            {
                // Offline or backend unavailable fallback
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

            // Go to PortionPage with candidate details
            var navigationParameter = new Dictionary<string, object>
            {
                { "Candidate", candidate }
            };

            await Shell.Current.GoToAsync(nameof(PortionPage), navigationParameter);
        }
    }
}
