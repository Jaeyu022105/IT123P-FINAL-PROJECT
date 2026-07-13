# Feature: Nutrition Lookup & Diet Comparison

**Pattern used:** MVVM (client) calling a REST web service (backend)

## Why This Feature Exists
Identifying the food is only half the job — the app needs to turn "grilled chicken breast, medium portion" into actual calories/macros, then tell the user if it fits their remaining daily budget. This is the payoff moment of the app.

## User Flow
1. After a food is confirmed (from recognition or manual search) and portion size is set
2. `ResultsViewModel` calls the backend `GET /api/nutrition/{foodId}?grams={x}`
3. Backend queries USDA FoodData Central, maps the response to calories/protein/carbs/fat
4. `ResultsViewModel` also calls `GET /api/diet/compare` with today's running total + this item
5. UI shows: nutrition breakdown + a clear fits/doesn't-fit indicator against the daily goal

## MVVM Breakdown
- **Model:** `NutritionResult { double Calories; double ProteinG; double CarbsG; double FatG; }`, `DietGoal { double DailyCalorieLimit; MacroSplit Targets; }`
- **ViewModel:** `ResultsViewModel`
  - `ObservableProperty`: `Nutrition`, `FitsDiet`, `RemainingCaloriesToday`
  - `RelayCommand`: `LogThisMealCommand`, `AdjustPortionCommand`
- **View:** `ResultsPage.xaml` — nutrition card, progress ring for daily budget, "fits your diet ✅ / over budget ⚠️" banner

## Backend Web Service Layer (MVC Controller)
- `NutritionController.GetNutrition(string foodId, int grams)` → calls USDA FoodData Central REST API, scales per-100g values to the requested portion
- `DietController.CompareToGoal(DietComparisonRequest request)` → pure calculation, no external call: compares proposed meal + today's logged total against the user's stored `DietGoal`

## Diet Goal Setup
- Set once in onboarding, editable anytime in Settings
- Simple inputs: daily calorie target, optional macro split (protein/carbs/fat %) — no complex nutrition science required from the user, sensible presets offered (e.g. "Balanced", "High protein", "Low carb")

## Error Handling
- **USDA API unreachable/timeout** → backend returns a clear `503`-style error; client shows "Nutrition data unavailable right now, try again" and still allows the user to log the food with placeholder/unknown nutrition (so the app never blocks logging)
- **Food not found in USDA database** → client offers a manual macro entry form as fallback
- **No diet goal set yet** → app skips the comparison banner and prompts the user to set one, rather than guessing

## Scope & Limitations
- Nutrition values are estimates based on generic USDA entries, not the exact product/restaurant item (USDA data is close to raw ingredients, not brand-specific)
- No micronutrient tracking (vitamins/minerals) in v1 — calories + the 3 macros only
- No multi-day trend charts in v1 (just "today's" comparison) — could be a v2 feature
