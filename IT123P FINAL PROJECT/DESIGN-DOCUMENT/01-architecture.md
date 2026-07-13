# Architecture — Patterns Used & Why

This file explains each required pattern (MVC, MVVM, Middleware, Web Service & SOAP), why it's used, and exactly where it lives in the FoodLens project.

## 1. MVVM (Model-View-ViewModel) — used in the MAUI client

**Why MVVM here:** MAUI is built around data-binding (XAML `{Binding}`), and MVVM is the pattern .NET's binding engine is designed for. It keeps UI code (View) separate from state/logic (ViewModel), so the UI can be tested and changed without touching business logic.

**Structure:**
- **Model** — plain data classes: `FoodItem`, `DietGoal`, `LogEntry`
- **View** — XAML pages: `CameraPage.xaml`, `ResultsPage.xaml`, `DietSummaryPage.xaml`
- **ViewModel** — `CameraViewModel`, `ResultsViewModel`, `DietSummaryViewModel` — hold `ObservableProperty` state and `RelayCommand`s, call services, expose bindable results

**Example flow:** `CameraPage` binds a button to `CameraViewModel.CapturePhotoCommand` → ViewModel calls `IFoodRecognitionService` → updates an `ObservableCollection<FoodItem>` → `ResultsPage` re-renders automatically via binding.

**Library:** `CommunityToolkit.Mvvm` (source generators for `[ObservableProperty]`, `[RelayCommand]`) — reduces boilerplate.

## 2. MVC (Model-View-Controller) — used in the backend API

**Why MVC here:** The backend is an ASP.NET Core Web API. ASP.NET Core's Web API project template is built on MVC's Controller pattern (routing → Controller action → returns data). There's no traditional "View" (no HTML rendering) — the API returns JSON — but the Controller/Model separation and routing conventions are pure MVC.

**Structure:**
- **Model** — `FoodDto`, `NutritionResult`, `DietComparisonResult`
- **Controller** — `NutritionController`, `RecognitionController`, `DietController` — each exposes REST endpoints (e.g. `GET /api/nutrition/{foodId}`)
- **"View"** — replaced by JSON serialization (the API's output contract)

**Why a backend at all, instead of calling USDA directly from the phone?**
- Keeps the free USDA API key off the client (avoids embedding secrets in the app)
- Lets you swap/upgrade the nutrition source later without an app update
- Central place to apply the Middleware pipeline below

## 3. Middleware — used in the backend API pipeline

**Why middleware here:** Cross-cutting concerns (things every request needs) shouldn't be repeated in every Controller. ASP.NET Core's middleware pipeline lets you handle these once, in order, for every request.

**Pipeline order (each request passes through in this order):**
1. **Exception-handling middleware** — catches unhandled errors, returns a clean JSON error response instead of a raw stack trace
2. **Logging middleware** — logs method, path, response time, status code
3. **Rate-limiting middleware** — protects the free USDA API quota from being burned by abuse/retries
4. **Authentication middleware** (lightweight — e.g. simple API key or anonymous device ID, since this is a hobby project, not enterprise auth)
5. **Routing → Controller**

**Why in this order:** exception handling must wrap everything (so it can catch errors from later middleware too); logging goes early so failed requests are still logged; rate-limiting before auth avoids doing auth work on requests you're about to reject anyway.

## 4. Web Service & SOAP — legacy interoperability

**Why SOAP at all in 2026:** SOAP is legacy, but still genuinely used in healthcare, insurance, and enterprise systems that haven't modernized. It's included here specifically to demonstrate integration with such a system: an "export my diet log to my dietitian's clinic system" feature, where the hypothetical clinic system only accepts SOAP/XML.

**How it's implemented:** Hosted using **CoreWCF** (the actively-maintained .NET port of WCF), which as of mid-2026 supports .NET 8, 9, and 10. It provides SOAP, WSDL, and `BasicHttpBinding` support on top of ASP.NET Core, so it can live in the same backend project as the MVC controllers.

**Contract:** `IDietExportService` with a `ExportDietLog(string userId, DateTime from, DateTime to)` operation, returning an XML-serialized `DietLogExport` document.

**Why this is a separate concern from the REST API:** SOAP has different serialization (XML, not JSON), a WSDL contract instead of OpenAPI, and different client tooling. It's deliberately isolated in its own file — see `05-feature-legacy-soap-integration.md` — instead of mixed into the MVC controllers.

## 5. Backend Database (MySQL & SQLite) — persistent storage

**Why SQLite and MySQL:** To simplify local development and testing, SQLite is used for the development backend database. MySQL is used in production as the persistent system of record. Local client-side SQLite still exists on the client, but purely as a local offline cache that syncs to the server-side database.

**What lives in the Server-Side Database:**
- `Users` — device/user identity, diet goal settings
- `FoodLogs` — every logged meal (food name, portion, macros, timestamp) — full CRUD, see `04-feature-backend-api.md`
- `FoodCache` — cached USDA lookups, to avoid re-querying the same food repeatedly

**How it connects:** The ASP.NET Core backend uses a Repository/Service pattern. Under Entity Framework Core, it dynamically configures either the SQLite provider (`Microsoft.EntityFrameworkCore.Sqlite`) or the MySQL provider (`Pomelo.EntityFrameworkCore.MySql`) depending on the environment (Development vs. Production). Controllers never touch the database directly.

## 6. XML — the second data-exchange format

**Why XML matters here:** the assignment requires both JSON and XML data exchange, not just REST/JSON. This project satisfies that two ways:
- The **SOAP export service** (`05-feature-legacy-soap-integration.md`) is entirely XML-based (SOAP envelopes, WSDL/XSD contracts) — this is the primary place XML lives
- The REST API can optionally content-negotiate XML too (ASP.NET Core supports `Accept: application/xml` via `AddXmlSerializerFormatters()`), so the same `FoodLogs` CRUD endpoints can return either JSON or XML depending on what the caller asks for

## 7. How the Patterns Fit Together

| Layer | Pattern | Reason |
|---|---|---|
| MAUI client | MVVM | Native fit for XAML data binding |
| Backend API | MVC | ASP.NET Core Web API convention, clean routing |
| Backend pipeline | Middleware | Cross-cutting concerns handled once |
| Backend storage | MySQL (Prod) / SQLite (Dev) | Persistent server DB (SQLite for dev, MySQL for prod) |
| Legacy export | SOAP (CoreWCF) | Interop with older systems that require SOAP/XML |
