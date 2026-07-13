# Feature: Backend API (MVC + Middleware)

**Patterns used:** MVC (Controllers), Middleware (request pipeline)

## Why This Feature Exists
The MAUI app doesn't talk to USDA (or the SOAP legacy system) directly. A thin backend sits in between so:
- The USDA API key never ships inside the mobile app
- Cross-cutting rules (logging, rate limiting, error shaping) live in one place instead of being copy-pasted per feature
- The nutrition source can be swapped later (e.g. add a second provider) without an app store update

## MVC Structure
| Controller | Responsibility |
|---|---|
| `FoodLogController` | Full CRUD on logged meals (see below) |
| `NutritionController` | Wraps USDA FoodData Central lookups (read-only) |
| `RecognitionController` | (optional) proxies to a cloud recognition model if the on-device model is ever swapped for a server one |
| `DietController` | Diet goal CRUD + comparison math |
| `ExportController` | Triggers the SOAP export job (see `05-feature-legacy-soap-integration.md`) |

Each Controller is thin — it validates the request, calls a Service class (e.g. `IUsdaClient`, `IDietComparisonService`, `IFoodLogRepository`), and shapes the response. Business logic lives in Services, not Controllers, so it stays testable. All data-backed Controllers read/write through the backend database (SQLite in development, MySQL in production).

## CRUD Endpoints — `FoodLogController` (the core CRUD requirement)

| Operation | Endpoint | Description |
|---|---|---|
| **Create** | `POST /api/foodlogs` | Add a new logged meal (food, portion, macros, timestamp) to the backend database |
| **Read (all)** | `GET /api/foodlogs?date={date}` | Get all logged meals for a given day |
| **Read (one)** | `GET /api/foodlogs/{id}` | Get a single logged meal by ID |
| **Update** | `PUT /api/foodlogs/{id}` | Edit an existing entry (e.g. fix portion size, correct the food match) |
| **Delete** | `DELETE /api/foodlogs/{id}` | Remove a mis-logged or duplicate entry |

## CRUD Endpoints — `DietController` (diet goal management)

| Operation | Endpoint | Description |
|---|---|---|
| **Create** | `POST /api/dietgoal` | Set the user's diet goal (first-time setup) |
| **Read** | `GET /api/dietgoal` | Get the current goal |
| **Update** | `PUT /api/dietgoal` | Change calorie/macro targets |
| **Delete** | `DELETE /api/dietgoal` | Reset/clear the goal (back to no-goal state) |

Both sets of endpoints support `Accept: application/json` or `Accept: application/xml` (XML formatter enabled), satisfying the JSON + XML data-exchange requirement on the REST side, in addition to the fully-XML SOAP export feature.

## Middleware Pipeline (in order)
1. **`ExceptionHandlingMiddleware`** — wraps everything; converts unhandled exceptions into a consistent JSON error shape: `{ "error": { "code": "...", "message": "..." } }`
2. **`RequestLoggingMiddleware`** — logs method, path, status, duration for every request (helps debug the free USDA quota usage too)
3. **`RateLimitingMiddleware`** — built-in ASP.NET Core rate limiter, caps requests per device/IP to stay safely under USDA's free-tier limits
4. **`ApiKeyAuthMiddleware`** — lightweight per-device API key (not full user accounts in v1) so the endpoint isn't wide open to the public internet

## Why This Order Matters
- Exception handling must be outermost, or errors thrown by later middleware (e.g. rate limiter) won't be caught cleanly
- Logging before auth/rate-limiting means even rejected requests are logged (useful for spotting abuse)
- Rate limiting before auth avoids wasting auth-check work on requests that'll be rejected anyway

## Error Handling
- All Controllers return a consistent error envelope via the exception middleware — client code has exactly one shape to parse for errors
- Validation errors (e.g. missing `foodId`) → `400` with a field-level message
- External dependency failures (USDA down) → `502`/`503` with a retry-safe flag the client can check

## Scope & Limitations
- No full user-account system in v1 (device-based identity only) — real auth (OAuth, accounts) is a stretch goal, not required for the core feature to work
- Single backend instance assumed for v1 — no need for distributed rate-limiting/session state at hobby-project scale
- Backend is required infrastructure, not optional — the "100% free" constraint applies to the *external APIs* it calls, not to needing zero backend at all (a small ASP.NET Core API can be hosted free-tier on something like Azure App Service free tier or a similar free host)
