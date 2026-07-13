# UI / UX Design

## Navigation Structure (Shell-based, MAUI `AppShell`)
```
TabBar
├── Home (Diet Summary)
├── Log Food (Camera)
└── Settings
```

## Screen 1 — Diet Summary (Home)
- Top: circular progress ring showing calories used / daily target
- Below: 3 small bars for protein/carbs/fat vs target
- List: today's logged meals (time + food name + calories), tap to view/edit
- Floating action button → jumps straight to "Log Food"

## Screen 2 — Camera / Log Food
- Full-screen camera preview
- Bottom bar: capture button (center, large), gallery-picker icon (side)
- After capture: loading spinner overlay with short status text ("Identifying food…")
- Candidate results appear as a bottom sheet: food name, confidence %, thumbnail
- "None of these — search manually" link always visible below the candidates

## Screen 3 — Portion & Confirm
- Selected food name at top
- Portion size control: simple slider (grams) + preset chips ("Small / Medium / Large")
- Live-updating nutrition preview as portion changes
- "Add to log" primary button

## Screen 4 — Results / Fits-Diet Check
- Nutrition breakdown card (calories, protein, carbs, fat)
- Fits-diet banner:
  - ✅ green — "Fits your remaining budget today"
  - ⚠️ amber — "This will put you over your daily goal by X kcal"
- "Log it anyway" always available — the app informs, never blocks

## Screen 5 — Settings
- Diet goal editor (calorie target, macro split presets)
- Export diet log (triggers the SOAP export feature, Phase 5)
- About / data source disclosure (USDA FoodData Central attribution)

## Visual Direction
- Clean card-based layout, rounded corners, soft shadows
- Since you're into purple/pink tones — suggested palette: a deep purple primary (`#6C4AB6`-ish) with a pink accent (`#F06292`-ish) for CTAs/highlights, neutral light background, keeps nutrition data (numbers/charts) in a clean dark-gray/black for readability
- Progress rings and macro bars use the pink/purple accent pair so the "fits your diet" moment feels visually rewarding, not clinical

## Accessibility Notes
- Fits-diet status is never color-only — always paired with an icon (✅/⚠️) and text label
- Minimum tap target 44x44pt on camera/capture controls
- Font sizes respect system text-scaling settings (MAUI `FontAutoScalingEnabled`)
