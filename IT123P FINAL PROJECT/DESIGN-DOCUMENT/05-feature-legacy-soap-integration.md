# Feature: Legacy Diet Log Export (SOAP Web Service)

**Pattern used:** SOAP web service (via CoreWCF)

## Why This Feature Exists
This is a stretch/optional feature whose main purpose is to demonstrate SOAP interoperability, framed around a realistic use case: exporting a user's diet history to an older third-party system (e.g. a dietitian's clinic software) that only accepts SOAP/XML, not REST/JSON. This pattern is still common in healthcare and enterprise software that hasn't modernized.

## Why SOAP Instead of REST Here
- The (hypothetical) receiving clinic system only understands SOAP/WSDL contracts
- SOAP enforces a strict, strongly-typed contract via WSDL/XSD — useful when interoperating with a system where both sides need a formal, versioned agreement on data shape
- Confirmed as of mid-2026: **CoreWCF** (the actively maintained .NET port of WCF) supports .NET 8, 9, and 10, so this can be hosted alongside the ASP.NET Core MVC backend rather than needing a separate legacy stack

## Service Contract
```
Service: DietExportService
Operation: ExportDietLog(string userId, DateTime from, DateTime to) : DietLogExportXml
Binding: BasicHttpBinding (over HTTPS)
```

`DietLogExportXml` is a strongly-typed XML document containing date, food name, portion, calories, and macros for each logged entry in the requested range.

## Where It Lives in the Architecture
- Hosted in the same ASP.NET Core backend project as the MVC controllers, using `CoreWCF.Http` + `AddServiceModel`
- Exposed at its own endpoint (e.g. `/soap/DietExportService.svc`) — kept separate from the REST API routes so the two contract styles (JSON/REST vs XML/SOAP) don't mix
- Triggered from the client via `ExportController.RequestExport()` (a normal REST endpoint) which internally calls the SOAP service — the MAUI app itself never has to speak SOAP directly

## Error Handling
- `ServiceBehavior(IncludeExceptionDetailInFaults = false)` — never leak internal exception details in a SOAP fault to an external system
- Custom `FaultException<ExportFault>` returned for expected failure cases (e.g. no data in range, invalid date range)
- Timeouts configured deliberately short (this is a batch export, not a live user-facing call) with a clear failure state surfaced back through the REST `ExportController`

## Scope & Limitations
- This is explicitly a stretch/optional feature (Phase 5) — the core app works fully without it
- No real external clinic system exists to integrate with; this is built as a self-contained SOAP service + a test client, to demonstrate the pattern rather than a live third-party integration
- One-way export only (diet log → external system) — no import path back into FoodLens
