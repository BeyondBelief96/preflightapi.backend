# Data Staleness Detection & Notification — Phase 1

## Overview

Cron jobs sync weather and FAA data on intervals. If they fail silently, users receive stale data with no indication. Phase 1 adds a `data_sync_status` table that every cron job writes to on success/failure, a service to evaluate freshness, response headers on API responses, a dedicated `/health/data-freshness` endpoint, and a health check integrated into the existing `/health` pipeline.

## What Was Built

### 1. Database Table: `data_sync_status`

A single table tracks the last sync outcome for each of the 14 data sync types. The migration seeds all 14 rows on first run.

| Column | Type | Purpose |
|--------|------|---------|
| `sync_type` (PK) | `varchar(50)` | Identifier (e.g. `Metar`, `Airport`) |
| `staleness_mode` | `varchar(20)` | `TimeBased` or `CycleBased` |
| `staleness_threshold_minutes` | `int?` | Max age before stale (time-based only) |
| `publication_type` | `varchar(50)?` | Links to `faa_publication_cycle` (cycle-based only) |
| `last_successful_sync_utc` | `timestamptz?` | When data was last successfully refreshed |
| `last_attempted_sync_utc` | `timestamptz?` | When the last sync attempt occurred |
| `last_sync_succeeded` | `bool` | Whether the most recent attempt succeeded |
| `consecutive_failures` | `int` | Running count of sequential failures |
| `last_error_message` | `varchar(2000)?` | Error from most recent failure |
| `last_successful_record_count` | `int` | Records processed in last success |
| `updated_at` | `timestamptz` | Row last modified |

### 2. Sync Types & Thresholds

14 sync types are tracked, split into two staleness modes:

**Time-based** (weather / real-time data — stale if age > threshold):

| Sync Type | Threshold | Cron Interval |
|-----------|-----------|---------------|
| Metar | 50 min | Every 10 min |
| Taf | 120 min | Every 30 min |
| Pirep | 30 min | Every 5 min |
| Sigmet | 120 min | Every 30 min |
| GAirmet | 120 min | Every 30 min |
| NotamDelta | 15 min | Every 3 min |
| ObstacleDailyChange | 2880 min (48h) | Daily 10:30 UTC |

**Cycle-based** (FAA publication data — stale if not synced since current cycle started):

| Sync Type | Publication Type | Cycle Length |
|-----------|-----------------|--------------|
| Airport | NasrSubscription_Airport | 28 days |
| Frequency | NasrSubscription_Frequencies | 28 days |
| Airspace | Airspaces | 56 days |
| SpecialUseAirspace | SpecialUseAirspaces | 56 days |
| Obstacle | Obstacles | 56 days |
| ChartSupplement | ChartSupplement | 56 days |
| TerminalProcedure | TerminalProcedure | 28 days |

### 3. Severity Levels

**Time-based** severity is based on `age / threshold` ratio:

| Ratio | Severity | Meaning |
|-------|----------|---------|
| < 1.0x | `none` | Fresh |
| 1.0x–1.5x | `info` | Approaching staleness |
| 1.5x–2.0x | `warning` | Stale |
| >= 2.0x | `critical` | Critically stale |

**Cycle-based** severity is based on days past the current cycle date without a successful sync:

| Days Past Cycle | Severity |
|-----------------|----------|
| < 1 day | `info` |
| 1–2 days | `warning` |
| >= 2 days | `critical` |

Never synced = `critical` for both modes.

### 4. Service Interface

`IDataSyncStatusService` provides three methods:

- **`RecordSuccessAsync(syncType, recordCount)`** — Called by Azure Functions after successful sync. Resets `ConsecutiveFailures`, sets `LastSuccessfulSyncUtc`, clears error message.
- **`RecordFailureAsync(syncType, errorMessage)`** — Called in the `catch` block. Increments `ConsecutiveFailures`, stores the error message.
- **`GetAllFreshnessAsync()`** — Reads all 14 rows, joins with `faa_publication_cycle` for cycle-based types, and returns `DataFreshnessResult` DTOs with computed severity.

Both `RecordSuccessAsync` and `RecordFailureAsync` use internal try/catch — they log warnings but never throw, so sync tracking failures cannot break the actual data sync.

### 5. API Response Headers

The `DataFreshnessMiddleware` adds headers to 2xx responses on data endpoints:

| Header | Example Value | Description |
|--------|---------------|-------------|
| `X-Data-Freshness` | `fresh` or `stale:warning` | Overall freshness status |
| `X-Data-Last-Updated` | `2026-02-24T14:30:00.0000000Z` | ISO 8601 timestamp of last successful sync |
| `X-Data-Sync-Age-Minutes` | `12.5` | Minutes since last successful sync (time-based only) |

**Route mapping** (middleware extracts the route segment from `/api/v1/{segment}/...`):

| Route Segment | Sync Type(s) |
|---------------|-------------|
| `metars` | Metar |
| `tafs` | Taf |
| `pireps` | Pirep |
| `sigmets` | Sigmet |
| `g-airmets` | GAirmet |
| `notams` | NotamDelta |
| `airports` | Airport |
| `communication-frequencies` | Frequency |
| `airspaces` | Airspace, SpecialUseAirspace |
| `obstacles` | Obstacle |
| `chart-supplements` | ChartSupplement |
| `terminal-procedures` | TerminalProcedure |

Routes with no data backing (`e6b`, `navlog`, `briefing`) are excluded — no headers added.

When a route maps to multiple sync types (e.g. `airspaces`), the worst severity is used.

Freshness data is cached in `IMemoryCache` with a 2-minute TTL to avoid per-request DB hits.

### 6. Health Check Integration

`DataFreshnessHealthCheck` integrates with the existing ASP.NET health check pipeline:

- **`/health`** — Now includes `data-freshness` check alongside `database`, `blob-storage`, `noaa-weather`, `noaa-magvar`
- **`/health/ready`** — Also includes `data-freshness` (tagged `ready`)
- Returns `Degraded` (not `Unhealthy`) if any data is stale — the API is still functional, just serving older data

### 7. Dedicated Freshness Endpoint

`GET /health/data-freshness` returns a detailed JSON response:

```json
{
  "checkedAt": "2026-02-24T15:00:00Z",
  "dataTypes": [
    {
      "syncType": "Metar",
      "isFresh": true,
      "severity": "none",
      "stalenessMode": "TimeBased",
      "lastSuccessfulSync": "2026-02-24T14:55:00Z",
      "consecutiveFailures": 0,
      "lastErrorMessage": null,
      "ageMinutes": 5.0,
      "thresholdMinutes": 50,
      "currentCycleDate": null,
      "daysPastCycleWithoutUpdate": null,
      "message": "Metar is fresh (5m old, threshold 50m)."
    },
    {
      "syncType": "Airport",
      "isFresh": true,
      "severity": "none",
      "stalenessMode": "CycleBased",
      "lastSuccessfulSync": "2026-02-21T10:05:00Z",
      "consecutiveFailures": 0,
      "lastErrorMessage": null,
      "ageMinutes": null,
      "thresholdMinutes": null,
      "currentCycleDate": "2026-02-20T00:00:00Z",
      "daysPastCycleWithoutUpdate": 0,
      "message": "Airport is current for cycle 2026-02-20."
    }
  ]
}
```

### 8. Azure Functions Changes

All 14 data-syncing Azure Functions were updated to inject `IDataSyncStatusService` and wrap their sync logic:

**Weather functions** (Metar, Taf, Pirep, Sigmet, GAirmet, NotamDeltaSync, ObstacleDailyChange):
```
try {
    await service.PollWeatherDataAsync(ct);
    await syncStatusService.RecordSuccessAsync(SyncTypes.Xxx, ct);
} catch {
    try { await syncStatusService.RecordFailureAsync(SyncTypes.Xxx, ex.Message, ct); }
    catch { /* log warning, don't mask original error */ }
    throw;
}
```

**Cycle functions** (Airport, Frequency, Airspace, SpecialUseAirspace, Obstacle, ChartSupplement, TerminalProcedure): Same pattern but inside the `if (ShouldRunUpdate)` block, after `UpdateLastSuccessfulRunAsync`.

**Not modified**: `NotamInitialLoadFunction` (bootstrap-only), `CertificateRenewalFunction` (not data sync).

## Files Changed

### Created (8)
| File | Layer |
|------|-------|
| `PreflightApi.Domain/Entities/DataSyncStatus.cs` | Domain |
| `PreflightApi.Domain/Constants/SyncTypes.cs` | Domain |
| `PreflightApi.Infrastructure/Data/Configurations/DataSyncStatusConfiguration.cs` | Infrastructure |
| `PreflightApi.Infrastructure/Interfaces/IDataSyncStatusService.cs` | Infrastructure |
| `PreflightApi.Infrastructure/Services/DataSyncStatusService.cs` | Infrastructure |
| `PreflightApi.Infrastructure/Dtos/DataFreshnessResult.cs` | Infrastructure |
| `PreflightApi.Infrastructure/HealthChecks/DataFreshnessHealthCheck.cs` | Infrastructure |
| `PreflightApi.API/Middleware/DataFreshnessMiddleware.cs` | API |

### Modified (17)
| File | Change |
|------|--------|
| `PreflightApi.Infrastructure/Data/PreflightApiDbContext.cs` | Added `DbSet<DataSyncStatus>` |
| `PreflightApi.Infrastructure/HealthChecks/HealthCheckRegistrationExtensions.cs` | Registered `DataFreshnessHealthCheck` |
| `PreflightApi.API/Program.cs` | Registered service, middleware, `/health/data-freshness` endpoint |
| `PreflightApi.Azure.Functions/Program.cs` | Registered `IDataSyncStatusService` |
| 14 Azure Function files | Injected service + try/catch for success/failure recording |

### Migration
- `AddDataSyncStatus` — Creates `data_sync_status` table with 14 seeded rows

## Applying the Migration

```bash
dotnet ef database update --project PreflightApi.Infrastructure --startup-project PreflightApi.API
```

<<<<<<< HEAD
## Phase 2

All items originally deferred from Phase 1 have been implemented in Phase 2. See [data-staleness-phase2.md](data-staleness-phase2.md) for details:

- Resend email notifications on sustained staleness (with quiet periods, escalation, and recovery notices)
- `DataFreshnessAlertFunction` Azure Function for proactive alerting (every 5 min)
- Frontend `/status` page Data Sync Freshness section (auto-refreshing)
- `Accept-Warnings: stale-data` header opt-in for response body wrapping
- Enhanced `/health/data-freshness` endpoint with `overallStatus` and `summary`
- Shared `DataRouteMapping` extraction (deduplicated middleware + filter)
- Alert tracking columns (`last_alert_sent_utc`, `last_alert_severity`) on `data_sync_status`
=======
## Phase 2 (Out of Scope)

The following are deferred to a future iteration:

- Resend email notifications on sustained staleness
- `DataFreshnessAlertFunction` Azure Function for proactive alerting
- Frontend status page / dashboard integration
- `Accept-Warnings` header opt-in for response body wrapping (embedding staleness info in JSON responses)
>>>>>>> d4016c47889d9ed11b1cd84eb39e511940c14b13
