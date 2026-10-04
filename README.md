# FoodLens 🥗📷

> **Smart AI-Powered Cross-Platform Nutrition & Diet Tracking System**  
> **Course:** IT123P Final Project — Mapúa Malayan Colleges Laguna (College of Computer and Information Science)  
> **Team:** Team Kogane  
> **Authors:** Jaime de la Peña, Danielle Heart Gonzales, Russell John Reinaldi Salvador, Gabriel Quenneth Vasquez  

---

## 📋 Table of Contents
- [Overview](#-overview)
- [Key Features](#-key-features)
- [System Architecture](#-system-architecture)
  - [Design Patterns & Principles](#design-patterns--principles)
  - [Data Model & ERD](#data-model--erd)
- [Tech Stack](#-tech-stack)
- [Project Structure](#-project-structure)
- [Getting Started](#-getting-started)
  - [Prerequisites](#prerequisites)
  - [1. Backend API Setup](#1-backend-api-setup)
  - [2. Mobile App Setup (.NET MAUI)](#2-mobile-app-setup-net-maui)
- [API & Integration Reference](#-api--integration-reference)
  - [REST API Endpoints](#rest-api-endpoints)
  - [SOAP / XML Web Service](#soap--xml-web-service)
  - [External Third-Party APIs](#external-third-party-apis)
- [Security & Engineering Practices](#-security--engineering-practices)
- [Team Kogane](#-team-kogane)

---

## 🌟 Overview

**FoodLens** is a modern, cross-platform nutrition tracking application engineered to remove the friction of manual diet logging. Traditional nutrition apps require tedious typing, barcode hunting, and manual macro calculation, often leading to tracking fatigue. 

With **FoodLens**, users can:
1. **Snap a photo** or upload an image of their meal.
2. Receive immediate dish classification powered by **LogMeal AI Vision**.
3. Automatically retrieve official nutritional data (calories, protein, carbs, fat) from the **USDA FoodData Central Database**.
4. Scale portion sizes dynamically in grams with instant feedback against their remaining daily calorie and macronutrient budget.
5. Consult an integrated **Google Gemini AI Dietitian** for conversational diet coaching, suggestions, and automated target adjustments.
6. Export nutritional histories via a legacy **CoreWCF SOAP/XML** web service for compatibility with enterprise clinical systems.

---

## ✨ Key Features

- **📸 AI-Powered Photo Recognition:** Capture meals using your device camera or pick from the gallery. Candidates are classified with confidence scores via LogMeal API.
- **🔍 USDA Database Search:** Automatic nutritional mapping for recognized foods, plus full manual text search against the USDA FoodData Central database.
- **⚖️ Dynamic Portion Scaling:** Adjust serving weights in grams or choose quick presets (*Small [100g]*, *Medium [200g]*, *Large [350g]*). Real-time indicators show whether the portion fits your remaining daily budget.
- **📊 Daily Dashboard & Analytics:** Real-time progress bars for daily calories and macronutrient ratios (Protein, Carbs, Fat), along with full meal management (logging and deletion).
- **🗓️ History & Progress Tracking:** Browse previous days' logs and monitor consecutive-day dietary trends.
- **🤖 Interactive Gemini AI Dietitian:** Chat with an AI coach that knows your daily goals and logs, offering meal tips and one-tap goal adjustments.
- **🏥 Enterprise SOAP / XML Export:** Built-in CoreWCF SOAP service (`/soap/DietExportService.svc`) allowing clinical export of daily diet summaries.
- **📴 Offline Resilience:** Local SQLite storage (`sqlite-net-pcl`) ensures full offline logging and caching, synchronizing seamlessly with the backend when internet access is restored.

---

## 🏗️ System Architecture

FoodLens utilizes a client-server architecture with separation of concerns across presentation, business logic, persistence, and external integrations:

```
┌────────────────────────────────────────────────────────┐
│                   .NET MAUI Client                     │
│   (Android, iOS, Windows) - MVVM Architecture          │
│   - Local SQLite Cache (sqlite-net-pcl)                │
└───────────────────────────┬────────────────────────────┘
                            │ REST / JSON (HTTP)
                            ▼
┌────────────────────────────────────────────────────────┐
│                 ASP.NET Core Web API                   │
│   - Custom Middleware Pipeline                         │
│     ├─ ExceptionHandlingMiddleware (Global Error JSON) │
│     ├─ RequestLoggingMiddleware (Audit Trails)         │
│     └─ RateLimiting (Fixed-Window IP Protection)       │
│   - Entity Framework Core (SQLite / MySQL)             │
│   - CoreWCF SOAP Endpoint (/soap/DietExportService.svc)│
└────────────┬──────────────┬──────────────┬─────────────┘
             │              │              │
             ▼              ▼              ▼
     ┌──────────────┐┌──────────────┐┌──────────────┐
     │ LogMeal API  ││   USDA API   ││  Gemini AI   │
     │(Food Vision) ││(Nutrition DB)││ (AI Coach)   │
     └──────────────┘└──────────────┘└──────────────┘
```

### Design Patterns & Principles
- **MVVM (Model-View-ViewModel):** The client cleanly decouples UI views (XAML) from business logic (ViewModels inheriting `ObservableObject` and using `[RelayCommand]` code generation).
- **Dependency Injection & Abstraction:** Services implement clean interfaces (`IApiService`, `IDatabaseService`, `IUsdaClient`, `IFoodLogRepository`, `IDietGoalRepository`) facilitating testability and polymorphism.
- **Repository Pattern:** Database access on the backend is abstracted through repository interfaces, decoupling EF Core data models from controllers.
- **Middleware Pipeline:** Extensible HTTP pipeline handling security, logging, and global exception safety.

### Data Model & ERD

The backend database maintains three primary relational entities:

1. **`DietGoals`:** Stores device/user nutrition targets (`DailyCalorieLimit`, `ProteinPercentage`, `CarbsPercentage`, `FatPercentage`, `UpdatedAt`).
2. **`FoodLogs`:** Stores logged meals (`FoodName`, `FdcId`, `Calories`, `ProteinG`, `CarbsG`, `FatG`, `Grams`, `LoggedAt`). Linked logically via `DeviceId`.
3. **`FoodCache`:** Caches USDA food items and their nutrition values per 100g by `FdcId` to minimize redundant external network queries and preserve API quotas.

---

## 💻 Tech Stack

| Component | Technologies & Libraries |
|---|---|
| **Mobile Client** | .NET 9, .NET MAUI, C# 13, XAML |
| **MVVM Framework** | `CommunityToolkit.Mvvm`, `CommunityToolkit.Maui` |
| **Local Client Storage** | SQLite (`sqlite-net-pcl`) |
| **Backend API** | ASP.NET Core Web API (.NET 9), C# |
| **ORM / Data Access** | Entity Framework Core (`Microsoft.EntityFrameworkCore.Sqlite`, MySQL support) |
| **Legacy Integration** | CoreWCF (`CoreWCF.Primitives`, `CoreWCF.Http`) |
| **External Integrations** | LogMeal API v2, USDA FoodData Central REST API, Google Gemini API (`gemini-3.1-flash-lite`) |

---

## 📁 Project Structure

```text
IT123P-FINAL-PROJECT/
├── FoodLens.Api/                    # ASP.NET Core Web API Backend
│   ├── Controllers/                 # REST Controllers (Diet, FoodLog, Nutrition, Recognition, Chat, Export)
│   ├── Data/                        # AppDbContext, EF Core Migrations
│   ├── DTOs/                        # Data Transfer Objects for API requests/responses
│   ├── Middleware/                  # ExceptionHandlingMiddleware, RequestLoggingMiddleware
│   ├── Models/                      # Server Entities (DietGoal, FoodLog, FoodCacheItem)
│   ├── Services/                    # USDA Client, DietGoalRepository, CoreWCF SOAP Service
│   ├── appsettings.json             # API Keys & Database connection configuration
│   └── Program.cs                   # Application entry point, DI, & Middleware pipeline setup
│
├── IT123P FINAL PROJECT/            # .NET MAUI Mobile Client
│   ├── Converters/                  # XAML Value Converters (e.g. InverseBoolConverter)
│   ├── Models/                      # Client Models (FoodCandidate, FoodLogEntry, DietGoal)
│   ├── Resources/                   # App Icons, Splash, Raw SVGs (camera, bowl, chart, trash icons)
│   ├── Services/                    # ApiService (REST client), DatabaseService (SQLite cache)
│   ├── ViewModels/                  # MVVM ViewModels (DietSummary, Camera, Portion, History, Chat, Settings)
│   ├── Views/                       # XAML Views & Pages (DietSummaryPage, CameraPage, PortionPage, etc.)
│   ├── AppShell.xaml                # App Shell navigation & tab routing
│   └── MauiProgram.cs               # Client dependency injection & HttpClient base URL setup
│
├── Lessons/                         # Academic lesson documentation & reference materials
└── README.md                        # Project documentation
```

---

## 🚀 Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Visual Studio 2022 (v17.12+)](https://visualstudio.microsoft.com/) with the following workloads:
  - **.NET Multi-platform App UI development (.NET MAUI)**
  - **ASP.NET and web development**
- Android SDK & Android Emulator (or an Android device with USB Debugging enabled).

---

### 1. Backend API Setup

1. Open a terminal and navigate to the API directory:
   ```bash
   cd FoodLens.Api
   ```

2. Configure your API keys in `appsettings.json` (or via user secrets / environment variables):
   ```json
   {
     "Usda": {
       "ApiKey": "YOUR_USDA_API_KEY",
       "BaseUrl": "https://api.nal.usda.gov/fdc/v1"
     },
     "LogMeal": {
       "ApiUrl": "https://api.logmeal.com/v2/image/recognition/dish",
       "UserToken": "YOUR_LOGMEAL_USER_TOKEN"
     },
     "Gemini": {
       "ApiKey": "YOUR_GEMINI_API_KEY",
       "Model": "gemini-3.1-flash-lite"
     }
   }
   ```
   > *Note: USDA API keys can be obtained for free at [fdc.nal.usda.gov](https://fdc.nal.usda.gov/api-key-signup.html).*

3. Run the backend service:
   ```bash
   dotnet run --launch-profile http
   ```
   The backend API will start on `http://localhost:5000` (and `https://localhost:5001`). Database migrations for SQLite (`foodlens.db`) will apply automatically on startup.

---

### 2. Mobile App Setup (.NET MAUI)

1. Open `IT123P FINAL PROJECT.slnx` (or the project folder) in **Visual Studio 2022**.

2. **Network Base Address Configuration:**  
   In `MauiProgram.cs`, the base URL is configured as follows:
   - **Android Emulator:** Automatically targets `http://10.0.2.2:5000/` (special Android loopback to host PC localhost).
   - **Windows Machine:** Targets `https://localhost:5001/`.
   - **Physical Android Device (USB Debugging):** If debugging on a physical phone connected via Wi-Fi/USB, update `client.BaseAddress` in `MauiProgram.cs` to your development machine's local LAN IP address (e.g., `http://192.168.1.XX:5000/`).

3. Select your target (e.g. `Android Emulator - Pixel 5` or `Windows Machine`) and press **F5** to start debugging.

---

## 📡 API & Integration Reference

### REST API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/foodlogs?date={yyyy-MM-dd}` | Get logged meals for a specific date |
| `POST` | `/api/foodlogs` | Create a new food log entry |
| `DELETE` | `/api/foodlogs/{id}` | Delete a food log entry |
| `GET` | `/api/diet/goal` | Retrieve current daily calorie & macro targets |
| `PUT` | `/api/diet/goal` | Update daily calorie & macro targets |
| `GET` | `/api/diet/compare?proposedCalories={c}&todayLogged={l}` | Check if a meal portion fits today's remaining budget |
| `GET` | `/api/nutrition/search?q={query}&limit={n}` | Search USDA FoodData Central database |
| `GET` | `/api/nutrition/{fdcId}?grams={g}` | Fetch scaled nutrition for an FdcId and portion weight |
| `POST` | `/api/recognition/classify` | Upload photo multipart data for LogMeal AI vision classification |
| `POST` | `/api/chat` | Send conversational context to Google Gemini AI Dietitian |
| `POST` | `/api/export` | Export diet history as XML payload |

### SOAP / XML Web Service

- **Endpoint:** `http://localhost:5000/soap/DietExportService.svc`
- **Protocol:** SOAP 1.1 (`BasicHttpBinding`)
- **Service Contract:** `IDietExportService.ExportDietLog(userId, fromDate, toDate)`
- **Metadata (WSDL):** Accessible via standard browser GET request at `/soap/DietExportService.svc?wsdl`

### External Third-Party APIs

1. **LogMeal API:** Performs computer vision analysis on uploaded meal photos, returning dish predictions and probabilities.
2. **USDA FoodData Central:** Provides accurate, verified nutrient metrics per 100 grams.
3. **Google Gemini API:** Provides natural-language nutrition coaching, meal advice, and automated target recommendations.

---

## 🛡️ Security & Engineering Practices

- **IP-Based Rate Limiting:** Enforces a fixed-window limit of 60 requests per minute per IP to prevent DoS attacks and protect external API quotas.
- **Global Error Handling:** Custom `ExceptionHandlingMiddleware` intercepts all unhandled runtime exceptions, suppressing internal database details and stack traces while returning standardized JSON error envelopes.
- **Payload Validation:** Strict input checking on uploaded image payloads (non-zero byte length, valid MIME types) before forwarding to external AI services.
- **Safe Fallbacks:** Comprehensive offline fallbacks on the client allow users to continue logging meals and viewing cached data even when offline or during server restarts.

---

## 👥 Team Kogane

Developed for **IT123P** at **Mapúa Malayan Colleges Laguna**:

- **Jaime de la Peña**
- **Danielle Heart Gonzales**
- **Russell John Reinaldi Salvador**
- **Gabriel Quenneth Vasquez**

---
*Built with ❤️ using .NET MAUI & ASP.NET Core.*
