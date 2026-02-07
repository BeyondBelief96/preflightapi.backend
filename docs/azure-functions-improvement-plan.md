# Azure Functions Infrastructure Improvement Plan

> Generated: 2026-02-06
> Status: In Progress (14 of 26 items completed)

This document captures the comprehensive analysis and improvement plan for the Azure Functions infrastructure — the core data synchronization pipeline of PreflightApi.

---

## Current State Summary

- **12 Azure Functions** (5 weather polling, 7 daily FAA data sync)
- **13 Cron Services** in the Infrastructure layer
- **143 tests** — none covering functions or cron services
- **Polly retry** only on ArcGIS integration; all other HTTP calls have zero resilience
- **Application Insights** wired up but with sampling enabled and no alerting

---

## P0 — Critical Bugs

### 1. PirepFunction Swallows Exceptions
- **File**: `PreflightApi.Azure.Functions/Functions/PirepFunction.cs`
- **Issue**: Exception is caught and logged but NOT re-thrown — the only function with this bug
- **Impact**: Azure Functions runtime considers every invocation successful. No failure tracking, no alerting, no retries. PIREPs are pilot-reported turbulence/icing — safety-critical weather data.
- **Fix**: Add `throw;` after the `LogError` call, matching all other functions

### 2. AirportFunction Cron Expression Is Wrong
- **File**: `PreflightApi.Azure.Functions/Functions/AirportFunction.cs`
- **Issue**: Uses 5-field `"0 0 * * *"` while Azure Functions expects 6-field format. In 6-field context, this parses as `second=0, minute=0, hour=*, day=*, month=*` — fires **every hour**, not daily at midnight.
- **Fix**: Change to `"0 0 0 * * *"` (6-field: daily at midnight UTC)

---

## P1 — Resilience & Core Testing

### 3. Add Polly Retry Policies to All HTTP Integrations
- **Affected Services**: MetarCronService, TafCronService, PirepCronService, AirsigmetCronService, GAirmetCronService, FaaNasrBaseService, ObstacleCronService, AirportDiagramCronService, ChartSupplementCronService
- **Issue**: All use bare `HttpClient` with zero retry. If NOAA/FAA returns a transient 429/502/504, data goes stale until next timer fires.
- **Pattern**: Reuse the proven Polly pattern from `ArcGisBaseService` — exponential backoff, 3 retries, handle transient HTTP errors + `HttpIOException` + `TaskCanceledException`
- **Approach**: Register named HttpClients with Polly policies in `Program.cs` via `AddHttpClient().AddPolicyHandler()` rather than per-request policies in service code

### 4. Make ObstacleCronService Atomic
- **File**: `PreflightApi.Infrastructure/Services/CronJobServices/ObstacleCronService.cs`
- **Issue**: Does `ExecuteDeleteAsync()` (wipes all obstacles) then batch inserts. Crash mid-insert = partially empty table.
- **Fix**: Wrap in a transaction, or use a staging table pattern (insert new → swap → delete old)

### 5. Make AirportDiagramCronService Safer
- **File**: `PreflightApi.Infrastructure/Services/CronJobServices/AirportDiagramCronService.cs`
- **Issue**: Deletes ALL existing blobs before uploading new ones. Failure after delete = empty storage.
- **Fix**: Upload new files first, then delete old ones that aren't in the new set

### 6. Add Per-Record Error Handling in Weather Cron Services
- **Affected**: MetarCronService, TafCronService, PirepCronService, AirsigmetCronService, GAirmetCronService
- **Issue**: One malformed XML element halts the entire sync
- **Fix**: Wrap individual record processing in try/catch, log warning, continue with remaining records

### 7. Unit Tests for Cron Service Parsing
- **Priority targets**: XML parsing (weather services), CSV parsing (NASR services), fixed-width parsing (ObstacleCronService)
- **Tools available**: MockHttp, NSubstitute, MockQueryable, Bogus — all already in test project
- **Approach**: Mock HTTP responses with sample data files, verify parsed entities

### 8. Unit Tests for Azure Function Classes
- **All 12 functions**: Verify service invocation, error propagation, logging
- **Key assertion**: All functions must re-throw exceptions (catch the PirepFunction pattern)

---

## P2 — Performance & Observability

### 9. Batch SaveChangesAsync in MetarCronService and TafCronService
- **Files**: `MetarCronService.cs`, `TafCronService.cs`
- **Issue**: Call `SaveChangesAsync()` per record in a foreach loop — thousands of DB round trips per invocation
- **Fix**: Batch changes and call `SaveChangesAsync()` once or in batches (matching PirepCronService pattern)

### 10. Fix String Interpolation in Logger Calls
- **Affected files**: PirepFunction.cs, AirportFunction.cs, FrequencyFunction.cs, AirspaceFunction.cs, SpecialUseAirspaceFunction.cs, AirportDiagramFunction.cs, ChartSupplementFunction.cs, FaaNasrBaseService.cs
- **Issue**: `$"..."` string interpolation in `ILogger` calls defeats structured logging — Application Insights can't index parameters
- **Fix**: Convert to message templates: `_logger.LogInformation("Function executed at: {Time}", DateTime.UtcNow)`

### 11. Fix PirepFunction DateTime.Now
- **File**: `PirepFunction.cs`
- **Issue**: Uses `DateTime.Now` (local time) while all other functions use `DateTime.UtcNow`

### 12. Add Custom Metrics and Duration Tracking
- **Scope**: All cron services
- **Add**: Records processed count, sync duration (Stopwatch), error counts
- **Use**: Application Insights custom metrics via `TelemetryClient.TrackMetric()` or `ILogger` with structured properties

### 13. Configure Application Insights Sampling Exclusions
- **File**: `host.json`
- **Issue**: Sampling is enabled — some telemetry may be dropped, hiding failures
- **Fix**: Exclude exceptions and failed requests from sampling, or use fixed-rate sampling

### 14. Add Health Check Endpoints
- **Scope**: Functions project (and API)
- **Checks**: Database connectivity, external API reachability (NOAA, FAA), blob storage access
- **Use**: Azure health probes, uptime monitoring

### 15. Add Circuit Breakers for External APIs
- **Scope**: NOAA weather API, FAA NASR/document APIs
- **Approach**: Polly circuit breaker policies — break after N consecutive failures, allow half-open probes

---

## P3 — Infrastructure & Deployment

### 16. Create Production CI/CD Workflow
- **Issue**: Only `develop-ci-cd.yml` exists, hardcoded to test resources
- **Fix**: Create `main-ci-cd.yml` targeting production resources

### 17. Unify EF Core Package Versions
- **Issue**: API uses EF Core 9.0.1/Npgsql 9.0.3; Functions uses 9.0.7/9.0.4
- **Fix**: Align all projects to the same versions

### 18. Add Deployment Slots for Zero-Downtime Deploys
- **Scope**: Both API and Functions
- **Approach**: Deploy to staging slot → warm up → swap

### 19. Extract Interfaces for NASR Cron Services
- **Affected**: AirportCronService, RunwayCronService, RunwayEndCronService, CommunicationFrequencyCronService
- **Issue**: Registered as concrete types — can't be mocked for testing
- **Fix**: Extract interfaces, register via interface in DI

### 20. Integration Tests with Testcontainers
- **Scope**: Upsert/purge logic in cron services
- **Infrastructure**: `PostgreSqlTestBase` already exists but is unused
- **Priority**: Weather data upsert, obstacle delete+insert, NASR batch processing

### 21. Eliminate Double-Logging
- **Issue**: Exceptions logged at both service level AND function level
- **Fix**: Remove catch/log/throw from function layer — let the service layer handle logging, functions just propagate

---

## P4 — Long-Term Improvements

### 22. Infrastructure-as-Code (Bicep/Terraform)
- All Azure resources currently provisioned manually via portal
- Creates drift risk, makes DR slow, makes environment replication error-prone

### 23. Alerting Rules
- Function failure rate thresholds
- Data staleness detection (e.g., no METAR update in 30+ minutes)
- Database connectivity alerts
- External API degradation alerts

### 24. Dead Letter / Retry Queue for Failed Syncs
- For daily functions: if a sync fails, queue it for immediate retry rather than waiting 24 hours

### 25. Distributed Tracing and Correlation IDs
- Propagate function invocation ID through service layer logs
- Enable cross-service correlation in Application Insights

### 26. Duplicate Using Statement Cleanup
- **File**: `PreflightApi.Azure.Functions/Program.cs` (lines 9-10)
- **Issue**: `using PreflightApi.Domain.Entities;` appears twice

---

## Progress Tracker

| # | Item | Status | Date |
|---|------|--------|------|
| 1 | Fix PirepFunction exception swallowing | **Done** | 2026-02-06 |
| 2 | Fix AirportFunction cron expression | **Done** | 2026-02-06 |
| 3 | Add Polly retry to all HTTP integrations | **Done** | 2026-02-06 |
| 4 | Make ObstacleCronService atomic | **Done** | 2026-02-06 |
| 5 | Make AirportDiagramCronService safer | **Done** | 2026-02-06 |
| 6 | Per-record error handling in weather services | **Done** | 2026-02-06 |
| 7 | Unit tests for cron service parsing | Pending | |
| 8 | Unit tests for Azure Function classes | Pending | |
| 9 | Batch SaveChangesAsync in Metar/Taf | **Done** | 2026-02-06 |
| 10 | Fix string interpolation in loggers | **Done** | 2026-02-06 |
| 11 | Fix PirepFunction DateTime.Now | **Done** | 2026-02-06 |
| 12 | Add custom metrics/duration tracking | **Done** | 2026-02-07 |
| 13 | Configure App Insights sampling exclusions | **Done** | 2026-02-06 |
| 14 | Add health check endpoints | Pending | |
| 15 | Add circuit breakers | Pending | |
| 16 | Production CI/CD workflow | Pending | |
| 17 | Unify EF Core package versions | **Done** | 2026-02-07 |
| 18 | Deployment slots | Pending | |
| 19 | Extract NASR service interfaces | **Done** | 2026-02-07 |
| 20 | Integration tests with Testcontainers | Pending | |
| 21 | Eliminate double-logging | **Done** | 2026-02-07 |
| 22 | Infrastructure-as-Code | Pending | |
| 23 | Alerting rules | Pending | |
| 24 | Dead letter / retry queue | Pending | |
| 25 | Distributed tracing / correlation IDs | Pending | |
| 26 | Duplicate using cleanup | **Done** | 2026-02-06 |
