# Data Staleness Detection & Notification — Phase 2

## Overview

Phase 1 added the `data_sync_status` table, `IDataSyncStatusService`, response headers on data endpoints, a `/health/data-freshness` endpoint, and a `DataFreshnessHealthCheck`. Phase 2 builds on this foundation with four features:

1. **Email alerting** — Resend-based email notifications on sustained staleness, with quiet periods and severity escalation
2. **Alert tracking** — Two new columns on `data_sync_status` for deduplication
3. **Response body wrapping** — Opt-in `Accept-Warnings: stale-data` header that wraps JSON responses with warning metadata
4. **Frontend status page** — Data Sync Freshness section on the `/status` page with real-time auto-refresh

## What Was Built

### 1. Alert Tracking Columns

Two nullable columns added to `data_sync_status` for alert deduplication:

| Column | Type | Purpose |
|--------|------|---------|
| `last_alert_sent_utc` | `timestamptz?` | When the last alert email was sent for this type |
| `last_alert_severity` | `varchar(20)?` | Severity level of the last alert sent |

These enable the alert function to avoid re-alerting during the quiet period, detect severity escalation, and identify recovered types.

### 2. Clerk User Service

`IClerkUserService` / `ClerkUserService` fetches email recipients dynamically from Clerk rather than using a static list.

- Uses `ClerkApiClient` from the Clerk.Net SDK
- Paginates with `Limit = 500` / `Offset` until exhausted
- Extracts the primary email address from each user (`PrimaryEmailAddressId` → `EmailAddresses`)
- Results cached in `IMemoryCache` for 1 hour
- Degrades gracefully — logs a warning and returns an empty list on any failure

**NuGet package**: `Clerk.Net.DependencyInjection` (v1.15.0)

### 3. Email Notification Service

`IEmailNotificationService` / `ResendEmailNotificationService` sends two types of emails:

**Staleness Alert** (`SendStalenessAlertAsync`):
- Subject: `[PreflightApi] Data staleness alert — {count} type(s) stale`
- HTML body: table with sync type, severity (color-coded), last synced timestamp, and message
- Sent individually to each recipient via `IResend.EmailSendAsync`

**Recovery Notice** (`SendRecoveryNoticeAsync`):
- Subject: `[PreflightApi] Data recovered — {types}`
- HTML body: bulleted list of recovered sync types

Both methods:
- Check `ResendSettings.Enabled` — no-op if `false` (safe default)
- Fetch recipients from `IClerkUserService`
- Use per-recipient try/catch — one failed send doesn't block others
- Never throw exceptions to the caller

**NuGet package**: `Resend` (v0.2.1)

### 4. Resend Settings

`ResendSettings` configures the email notification behavior:

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `ApiToken` | `string` | `""` | Resend API key |
| `FromAddress` | `string` | `alerts@contact.preflightapi.io` | Sender address (must be on a verified Resend domain) |
| `ReplyToAddress` | `string?` | `bberisford@preflightapi.io` | Reply-to address (routes replies to business inbox) |
| `Enabled` | `bool` | `false` | Master on/off switch |
| `QuietPeriodMinutes` | `int` | `60` | Minimum interval between repeated alerts for the same type |

**Email domain setup**: The `FromAddress` uses the `contact.preflightapi.io` subdomain which is verified in Resend with its own DKIM key and SPF record. The root domain `preflightapi.io` is owned by Google Workspace (DMARC `p=reject`), so sending from `@preflightapi.io` via Resend would fail authentication. The `ReplyToAddress` points to the Google Workspace inbox so replies from recipients are delivered correctly.

### 5. `DataFreshnessAlertFunction`

Timer-triggered Azure Function running every 5 minutes (`0 */5 * * * *`).

**Logic**:
1. Fetch all 14 freshness results via `GetAllFreshnessAsync()`
2. Identify types needing alerts (severity >= `warning`):
   - **Skip types that have never synced** (`LastSuccessfulSync == null`) — prevents false alerts on fresh deployments before cron jobs have run
   - No prior alert sent (`LastAlertSentUtc == null`)
   - OR severity escalated (e.g. `warning` → `critical`)
   - OR quiet period elapsed (`now - LastAlertSentUtc > QuietPeriodMinutes`)
3. Identify recovered types: had a prior alert (`LastAlertSeverity != null`) but now fresh (`IsFresh == true`)
4. Send staleness alert → update `LastAlertSentUtc` / `LastAlertSeverity` per type
5. Send recovery notice → clear `LastAlertSentUtc` / `LastAlertSeverity` per type
6. Log if no action needed

**First-deploy safety**: On initial deployment, all 14 `data_sync_status` rows are seeded with `LastSuccessfulSyncUtc = null`. The alert function skips these entirely — no emails are sent until a sync type has successfully synced at least once and then subsequently goes stale. This prevents a flood of "14 types critically stale" emails before the cron jobs have had a chance to populate data.

Has `ExponentialBackoffRetry(3, "00:00:30", "00:05:00")` retry policy.

### 6. `Accept-Warnings` Response Body Wrapping

`DataFreshnessWarningFilter` is a global `IAsyncResultFilter` that optionally wraps API responses with staleness warnings.

**Activation**: Client sends `Accept-Warnings: stale-data` header.

**Behavior**:
- Only applies to `ObjectResult` with 2xx status on data endpoints
- Checks freshness from `IMemoryCache` (same cache key/TTL as middleware)
- If any relevant sync types are stale, wraps the response:

```json
{
  "data": { /* original response body */ },
  "warnings": [
    {
      "syncType": "Metar",
      "severity": "warning",
      "message": "Metar is stale (75m old, threshold 50m).",
      "lastSuccessfulSync": "2026-02-24T13:45:00Z"
    }
  ]
}
```

- If all relevant types are fresh, the response is returned unchanged (no wrapping)
- Without the `Accept-Warnings` header, behavior is identical to Phase 1 (headers only)

### 7. Shared Route Mapping

The route-to-sync-type mapping was extracted from `DataFreshnessMiddleware` into a shared `DataRouteMapping` static class. Both the middleware and the warning filter reference the same mapping, eliminating duplication.

| File | Purpose |
|------|---------|
| `PreflightApi.API/DataRouteMapping.cs` | `RouteToSyncTypes` dictionary + `ExtractDataRouteSegment()` |
| `PreflightApi.API/Middleware/DataFreshnessMiddleware.cs` | Refactored to use `DataRouteMapping` |
| `PreflightApi.API/Filters/DataFreshnessWarningFilter.cs` | Uses `DataRouteMapping` for route matching |

### 8. Enhanced `/health/data-freshness` Endpoint

The endpoint now returns a summary object alongside the per-type details:

```json
{
  "checkedAt": "2026-02-24T15:00:00Z",
  "overallStatus": "degraded",
  "summary": {
    "total": 14,
    "fresh": 12,
    "stale": 2,
    "bySeverity": {
      "none": 12,
      "warning": 1,
      "critical": 1
    }
  },
  "dataTypes": [ /* same as Phase 1, plus lastAlertSentUtc and lastAlertSeverity */ ]
}
```

**`overallStatus`** logic:
| Condition | Status |
|-----------|--------|
| All fresh | `healthy` |
| Any `critical` | `critical` |
| Any `warning` | `degraded` |
| Only `info` | `info` |

### 9. Frontend Status Page

The `/status` page now includes a **Data Sync Freshness** section below the existing health checks.

**Components**:
- **Summary banner** — Color-coded overall status (healthy/degraded/critical) with fresh/stale/total counts
- **Weather Data card** — All time-based sync types (Metar, Taf, Pirep, Sigmet, GAirmet, NotamDelta, ObstacleDailyChange)
- **FAA Publication Data card** — All cycle-based sync types (Airport, Frequency, Airspace, etc.)

**Per-entry display**:
- Severity dot (green/blue/yellow/red) with ping animation for fresh
- Sync type name (PascalCase converted to spaced words)
- Severity badge
- Relative time since last sync (e.g. "5m ago", "2h ago", "Never")
- Message and error details for stale types (collapsed for fresh)

**Data flow**:
- `fetchDataFreshness()` server function calls `GET /health/data-freshness`
- Uses `useQuery` with `healthKeys.dataFreshness()` query key
- Auto-refreshes every 30 seconds (same interval as system health)
- Gracefully handles missing `PREFLIGHT_API_BASE_URL` (returns null)

## Configuration

### Azure Functions (`local.settings.json`)

```json
{
  "Values": {
    "Resend:ApiToken": "<Resend API key (re_...)>",
    "Resend:FromAddress": "alerts@contact.preflightapi.io",
    "Resend:ReplyToAddress": "bberisford@preflightapi.io",
    "Resend:Enabled": "true",
    "Resend:QuietPeriodMinutes": "60",
    "Clerk:SecretKey": "<Clerk secret key (sk_test_... or sk_live_...)>"
  }
}
```

**Notes**:
- `Resend:Enabled` defaults to `false` — alerts are a no-op until explicitly enabled
- `Resend:FromAddress` must be on a domain verified in Resend (default: `contact.preflightapi.io`)
- `Resend:ReplyToAddress` routes replies to your Google Workspace inbox
- Recipients are fetched dynamically from Clerk (all users' primary emails)

### Production Environment Variables

Same keys, set as Azure Functions app settings:
- `Resend__ApiToken`
- `Resend__FromAddress`
- `Resend__ReplyToAddress`
- `Resend__Enabled`
- `Resend__QuietPeriodMinutes`
- `Clerk__SecretKey`

## Files Changed

### Created (10)

| File | Layer | Purpose |
|------|-------|---------|
| `PreflightApi.Infrastructure/Settings/ResendSettings.cs` | Infrastructure | Email configuration |
| `PreflightApi.Infrastructure/Interfaces/IClerkUserService.cs` | Infrastructure | User email fetching interface |
| `PreflightApi.Infrastructure/Services/ClerkUserService.cs` | Infrastructure | Clerk SDK integration |
| `PreflightApi.Infrastructure/Interfaces/IEmailNotificationService.cs` | Infrastructure | Email sending interface |
| `PreflightApi.Infrastructure/Services/ResendEmailNotificationService.cs` | Infrastructure | Resend SDK implementation |
| `PreflightApi.Infrastructure/Dtos/DataWarning.cs` | Infrastructure | Warning DTO for body wrapping |
| `PreflightApi.API/DataRouteMapping.cs` | API | Shared route-to-sync-type mapping |
| `PreflightApi.API/Filters/DataFreshnessWarningFilter.cs` | API | Global MVC filter for Accept-Warnings |
| `PreflightApi.Azure.Functions/Functions/DataFreshnessAlertFunction.cs` | Functions | Timer function for alerting |
| Migration: `AddAlertTrackingColumns` | Infrastructure | Adds 2 columns to `data_sync_status` |

### Modified (7)

| File | Change |
|------|--------|
| `PreflightApi.Domain/Entities/DataSyncStatus.cs` | Added `LastAlertSentUtc`, `LastAlertSeverity` |
| `PreflightApi.Infrastructure/Data/Configurations/DataSyncStatusConfiguration.cs` | Added `MaxLength(20)` for `LastAlertSeverity` |
| `PreflightApi.Infrastructure/Interfaces/IDataSyncStatusService.cs` | Added `UpdateAlertStateAsync`, `ClearAlertStateAsync` |
| `PreflightApi.Infrastructure/Services/DataSyncStatusService.cs` | Implemented alert methods, populated new DTO fields |
| `PreflightApi.Infrastructure/Dtos/DataFreshnessResult.cs` | Added `LastAlertSentUtc`, `LastAlertSeverity` fields |
| `PreflightApi.API/Program.cs` | Registered global filter, enhanced `/health/data-freshness` |
| `PreflightApi.API/Middleware/DataFreshnessMiddleware.cs` | Refactored to use shared `DataRouteMapping` |
| `PreflightApi.Azure.Functions/Program.cs` | Registered Resend SDK, Clerk SDK, alert services |

### Frontend (3 modified, in `preflightapi.frontend` repo)

| File | Change |
|------|--------|
| `src/types/health.ts` | Added `DataFreshnessStatus`, `DataFreshnessEntry` types |
| `src/lib/server/health.ts` | Added `fetchDataFreshness` server function |
| `src/lib/server/apim-queries.ts` | Added `healthKeys.dataFreshness()` query key |
| `src/routes/_marketing/status.tsx` | Added Data Sync Freshness section |

## Applying the Migration

```bash
dotnet ef database update --project PreflightApi.Infrastructure --startup-project PreflightApi.API
```

Adds two nullable columns to `data_sync_status`:
- `last_alert_sent_utc` (`timestamptz`, nullable)
- `last_alert_severity` (`varchar(20)`, nullable)

## Testing Locally

### Trigger the alert function manually

```bash
# Start Azure Functions
cd PreflightApi.Azure.Functions && func start

# Trigger on-demand (no need to wait 5 minutes)
curl -X POST http://localhost:7071/admin/functions/DataFreshnessAlertFunction \
  -H "Content-Type: application/json" -d '{}'
```

### Simulate stale data

```sql
-- Make Metar critically stale
UPDATE data_sync_status
SET last_successful_sync_utc = NOW() - INTERVAL '3 hours',
    last_sync_succeeded = false,
    consecutive_failures = 5,
    last_error_message = 'Simulated failure'
WHERE sync_type = 'Metar';
```

Trigger the function → verify alert is sent. Then reset:

```sql
UPDATE data_sync_status
SET last_successful_sync_utc = NOW(),
    last_sync_succeeded = true,
    consecutive_failures = 0,
    last_error_message = NULL
WHERE sync_type = 'Metar';
```

Trigger again → verify recovery notice is sent.

### Test response body wrapping

```bash
# Normal response (headers only)
curl -i http://localhost:7014/api/v1/metars/KDFW

# Wrapped response (if stale)
curl -i -H "Accept-Warnings: stale-data" http://localhost:7014/api/v1/metars/KDFW
```

### Test enhanced freshness endpoint

```bash
curl http://localhost:7014/health/data-freshness | jq .
```

### Test frontend

```bash
cd preflightapi.frontend && npm run dev
# Visit /status → verify Data Sync Freshness section
```
