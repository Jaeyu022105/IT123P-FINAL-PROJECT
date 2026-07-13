# Cross-Cutting Error Handling, Scope & Limitations

## 1. Global Error Handling Philosophy
The app should **inform, not block**. A wrong food guess, a missing nutrition value, or a network hiccup should never prevent the user from logging a meal — worst case, they log with partial/placeholder data and can correct it later.

## 2. Error Handling by Layer

| Layer | Approach |
|---|---|
| MAUI ViewModels | Wrap service calls in try/catch, expose an `ErrorMessage` observable property bound to a non-blocking banner/toast in the View |
| Backend Middleware | Central `ExceptionHandlingMiddleware` converts any unhandled exception into a consistent JSON error envelope |
| Backend Controllers | Validate input, return proper HTTP status codes (`400` bad input, `404` not found, `502/503` upstream failure) |
| SOAP service | `FaultException<T>` for expected failures, no internal exception detail leaked |
| Local SQLite | Offline-first — writes to local DB always succeed even if backend calls fail; a background sync retries when connectivity returns |
| Backend Database | System of record (SQLite in development, MySQL in production) — all synced food logs and diet goals persist here; Controllers never write directly, always through a Repository layer so failed writes can be retried/logged consistently |

## 3. Offline Behavior
- Diet log entries are always written locally first (SQLite), then synced to backend when possible
- If nutrition lookup fails while offline, the entry is saved with `NutritionStatus = Pending` and calories shown as "estimating…" until sync succeeds
- The camera/recognition feature (on-device TFLite) works fully offline; only nutrition lookup and SOAP export require connectivity

## 4. Project-Wide Scope

**In scope (v1):**
- Photo-based food recognition (on-device) with manual search fallback
- Calorie + macro (protein/carb/fat) tracking against a daily goal
- Local offline-first logging with background sync
- Backend API following MVC + Middleware pattern
- Optional SOAP export feature (stretch goal, Phase 5)

**Out of scope (v1):**
- Multi-item plate segmentation (multiple foods in one photo)
- Barcode scanning
- Micronutrient (vitamin/mineral) tracking
- Multi-day trend graphs / analytics dashboard
- Full user account system with cloud sync across devices
- Social features (sharing, friends, leaderboards)
- Any paid API or service — the entire external-data stack must remain free-tier-safe indefinitely

## 5. Known Limitations
- Nutrition estimates are approximations (USDA generic entries, not brand-specific or restaurant-specific)
- Portion size is user-estimated, not computer-vision-measured
- On-device recognition accuracy is lower than a large cloud model would be — an accepted trade-off for staying 100% free
- SOAP export feature has no real external system to integrate with in v1 — it's built and tested as a self-contained demonstration of the pattern

## 6. Future (v2+) Ideas — Not Committed
- Barcode scanning for packaged foods
- Weekly/monthly trend charts
- Optional paid-tier cloud recognition model for higher accuracy (opt-in, not required)
