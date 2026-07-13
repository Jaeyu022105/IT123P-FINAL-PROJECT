# Feature: Photo Capture & Food Recognition

**Pattern used:** MVVM (client-side)

## Why This Feature Exists
This is the app's core hook — instead of manually searching a food database, the user takes a photo and the app identifies the food. It removes the biggest friction point in food logging apps: manual entry.

## User Flow
1. User taps "Log Food" → `CameraPage` opens
2. User taps capture (or picks from gallery) → `MediaPicker` returns a photo stream
3. `CameraViewModel.CapturePhotoCommand` sends the image to `IFoodRecognitionService`
4. Loading state shown (`IsBusy = true`) while recognition runs
5. Result(s) returned as a list of candidate foods with confidence scores
6. User confirms the correct match (or manually searches if none match) → moves to portion size entry

## MVVM Breakdown
- **Model:** `FoodCandidate { string Name; double Confidence; string? UsdaFoodId; }`
- **ViewModel:** `CameraViewModel`
  - `ObservableProperty`: `IsBusy`, `CapturedImage`, `Candidates`
  - `RelayCommand`: `CapturePhotoCommand`, `PickFromGalleryCommand`, `RetryCommand`
- **View:** `CameraPage.xaml` — camera preview, capture button, loading spinner, candidate list bound to `Candidates`

## Recognition Approach (free-tier constraint)
Since fully free *unlimited* image-recognition APIs don't really exist, two supported modes:

| Mode | How it works | Trade-off |
|---|---|---|
| **On-device TFLite model** (default, recommended) | A pre-trained food-classification model (e.g. Food-101-based) bundled in the app, runs locally | Zero ongoing cost, works offline, slightly less accurate than cloud models, larger app size |
| **Manual fallback** | User types/searches food name directly | Always available as a fallback if recognition confidence is low or the model doesn't know the food |

The manual fallback isn't just an edge case — it's a first-class part of the flow, since recognition will never be 100%.

## Error Handling
- **Camera permission denied** → show inline message with a button to open app settings; don't crash
- **No camera available (rare/emulator)** → fall back to gallery picker automatically
- **Recognition returns nothing / low confidence (<40%)** → show "Couldn't identify this — search manually" instead of a wrong guess
- **Recognition model fails to load** → app falls back to manual search mode entirely for that session, with a one-time toast explaining why

## Scope & Limitations
- Recognizes single, clearly-visible food items well; mixed plates (e.g. a full dinner plate with 4 items) are a known weak spot — v1 will ask the user to confirm/adjust rather than trying to segment the plate
- Portion size is user-estimated (e.g. "small/medium/large" or grams slider), not computed from the image — depth/size estimation from a photo is out of scope for v1
- No barcode scanning in v1 (potential future feature, separate from photo recognition)
