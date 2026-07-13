# FoodLens — Project Design Document (Master Plan)

**Type:** .NET MAUI mobile app
**Purpose:** Take a photo of food, estimate its calories/macros, and check it against the user's daily diet plan.

## 1. Document Map

This design is split into multiple files so each concern can be read/updated independently:

| File | Covers |
|---|---|
| `01-architecture.md` | MVC, MVVM, Middleware, SOAP & Web Services — what each is, and where it's used in this project |
| `02-feature-photo-capture-recognition.md` | Camera capture + food recognition feature |
| `03-feature-nutrition-diet-tracking.md` | Nutrition lookup, diet goals, daily tracking |
| `04-feature-backend-api.md` | ASP.NET Core backend (MVC + Middleware pipeline) |
| `05-feature-legacy-soap-integration.md` | SOAP web service for exporting logs to a legacy system |
| `06-ui-ux-design.md` | Screens, layout, navigation flow |
| `07-error-handling-scope-limitations.md` | Cross-cutting error handling rules, scope & limitations |

## 2. Goal & Problem Statement

People want a fast way to log food without manual data entry. FoodLens lets a user snap a photo, get an estimated nutritional breakdown, and immediately see whether it fits their remaining daily calorie/macro budget.

## 3. High-Level System Overview

```
[MAUI App] --REST/JSON--> [ASP.NET Core Backend API]
     |                         |
     |                         |-- Middleware pipeline (auth, logging, error handling, rate limit)
     |                         |-- Controllers (MVC pattern, full CRUD)
     |                         |-- SQLite/MySQL (persistent storage: SQLite for dev, MySQL for prod)
     |                         |-- calls USDA FoodData Central (free, REST/JSON)
     |                         |-- SOAP/XML legacy export service (Web Service + XML requirement)
     |
     |--local SQLite (offline cache only, syncs to server DB)
```

- **Client (MAUI):** MVVM pattern.
- **Backend (ASP.NET Core Web API):** MVC pattern (Controllers) + custom Middleware pipeline.
- **Legacy interoperability:** SOAP web service (via CoreWCF), used for exporting a user's diet history to a hypothetical external system (e.g. a dietitian's older clinic software) — demonstrates SOAP integration in a place where it's realistically still used today (healthcare/enterprise systems often still run WCF/SOAP).
- **Free-tier constraint:** all external nutrition data comes from USDA FoodData Central (100% free, no cost ceiling). Food *recognition* uses an on-device or free-tier model — see `02-feature-photo-capture-recognition.md`.

## 4. Tech Stack

- **.NET MAUI** — cross-platform client (Android/iOS)
- **CommunityToolkit.Mvvm** — MVVM helpers (`ObservableObject`, `RelayCommand`)
- **CommunityToolkit.Maui** — camera/media picker helpers
- **SQLite (sqlite-net-pcl)** — local offline storage
- **ASP.NET Core Web API** — backend, MVC Controllers + Middleware
- **CoreWCF** — SOAP service hosting on .NET (confirmed actively supported on .NET 8/9/10 as of mid-2026)
- **MySQL / SQLite** — persistent server-side database (SQLite is used for local development, MySQL for production)
- **SQLite (sqlite-net-pcl)** — local on-device cache/offline store only, syncs to the server-side database via the REST API
- **USDA FoodData Central API** — free nutrition database, no rate-limit ceiling that would break a hobby project

## 5. Project Phases

1. **Phase 1 — Core MVVM client:** UI shells, navigation, local SQLite diet log (no network yet)
2. **Phase 2 — Nutrition lookup:** wire up USDA API through backend MVC controller
3. **Phase 3 — Photo recognition:** add camera capture + food classification
4. **Phase 4 — Diet comparison:** goals, daily budget, comparison logic
5. **Phase 5 — SOAP export (optional/stretch):** legacy export feature
6. **Phase 6 — Polish:** error handling pass, offline mode, UI theming

## 6. Out of Scope (project-wide)

- Real-time multi-user sync / social features
- Paid APIs of any kind
- Medical-grade accuracy — this is an *estimate* tool, not a clinical nutrition device
