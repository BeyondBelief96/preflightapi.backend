# Audit: Medium Priority

Consistency issues, moderate performance concerns, and maintainability improvements.

> **Note:** This API sits behind Azure APIM which provides:
> - **Response caching** (2–15 min depending on endpoint category, internal APIM cache)
> - **Rate limiting** (10–500 req/min per subscription tier)
> - **Quota enforcement** (5K–2M calls/month per tier)
>
> Items below are annotated where APIM coverage applies.

---

## DTO Style Consistency

Several DTOs use inconsistent patterns (class vs record, set vs init). The convention across
most DTOs is `record` with `init` setters.

| DTO | Current | Target | File |
|---|---|---|---|
| `AirspaceDto` | `class` + `set` | `record` + `init` | `Dtos/AirspaceDto.cs` (line 8) |
| `SpecialUseAirspaceDto` | `class` + `set` | `record` + `init` | `Dtos/AirspaceDto.cs` (line 79) |
| `NavlogResponseDto` | `record` + `set` | `record` + `init` | `Dtos/Navlog/NavlogResponseDto.cs` |
| `NavigationLegDto` | `record` + `set` | `record` + `init` | `Dtos/Navlog/NavigationLegDto.cs` |
| `MagneticVariationResponseDto` | `class` | `record` + `init` | `Dtos/Navlog/MagneticVariationResponseDto.cs` |
| `MagneticVariationResultDto` | `class` | `record` + `init` | `Dtos/Navlog/MagneticVariationResultDto.cs` |
| `GeoJsonGeometry` | `class` + `set` | `record` + `init` | `Dtos/GeoJsonGeometry.cs` |

**DTO naming convention:** Some types use `Dto` suffix, others use `Response`/`Request`. Pick
one convention and standardize. Recommendation: `Dto` for data transfer objects, `Request`/`Response`
only for top-level API request/response wrappers.

---

## Controller Consistency

### `NotamController` — parameter types and validation

**File:** `PreflightApi.API/Controllers/NotamController.cs`

- `GetNotamsByRadius` (line 237) and `SearchNotams` (line 417) use `double latitude/longitude`.
  Other spatial controllers (e.g., `AirportController`) use `decimal lat/lon` with `ValidationHelpers`.
- All validation is inline `throw new ValidationException(...)` instead of using `ValidationHelpers`.

**Fix:** Change to `decimal lat/lon` and use `ValidationHelpers.ValidateCoordinates()` for
consistency with other controllers.

### `NotamController.GetNotamsByNumber` — return type

Change `List<T>` return → `IEnumerable<T>` for consistency with other endpoints.

### `AirportController` — namespace style

**File:** `PreflightApi.API/Controllers/AirportController.cs` (line 9)

Uses block-scoped `namespace PreflightApi.API.Controllers { ... }`. Other controllers may use
file-scoped `namespace PreflightApi.API.Controllers;`. Standardize to file-scoped.

### `NavaidController` — type filtering

Unify the type filtering pattern to match the single approach used across other controllers.

---

## Performance (Moderate)

### WindsAloftService — no caching

**File:** `PreflightApi.Infrastructure/Services/WeatherServices/WindsAloftService.cs`

Registered as `Scoped` (`Program.cs` line 190). No caching at all — every `NavlogService` call
fires a live HTTP request to `aviationweather.gov`. Winds aloft forecasts update every 6 hours.

> **APIM note:** APIM caches the `/navlog` *response* for 300s, but the backend still makes
> uncached outbound calls to aviationweather.gov on every cache miss. An in-memory cache on this
> service (keyed by forecast cycle) would eliminate redundant outbound HTTP calls.

**Fix:** Inject `IMemoryCache`, cache parsed winds data keyed by forecast cycle with ~30 min TTL.
Consider changing registration to Singleton.

### SigmetService.GetSigmetsByHazardType — full table materialization

**File:** `PreflightApi.Infrastructure/Services/WeatherServices/SigmetService.cs` (lines 56–58)

Loads all SIGMETs into memory then filters by hazard type because the hazard is stored as a JSON
column that EF can't translate to SQL. Active SIGMETs are typically dozens to low hundreds, so
this is acceptable for now but doesn't scale.

**Potential fix:** Denormalize `HazardType` into a top-level indexed column, or use a raw SQL
`WHERE` clause with JSON path operators (`->>`).

### MetarService.GetMetarsByStates — correlated subquery

**File:** `PreflightApi.Infrastructure/Services/WeatherServices/MetarService.cs` (lines 159–168)

Translates to `WHERE EXISTS (SELECT 1 FROM airports WHERE ...)` with complex string operations
(`StartsWith("K")`, `Substring(1)`). This is slow at scale.

**Potential fix:** Add a `StateCode` column to the `Metars` table (populated during sync), or
use a materialized join table.

### N+1 Query Patterns

Three methods execute one database query per route point/airport:

| Service | Method | Pattern |
|---|---|---|
| `ObstacleService` | `GetAirportVicinityObstaclesAsync` (line 291) | `foreach` airport → 1 spatial query each |
| `NotamService` | `GetNotamsForRoutePointsAsync` (line 249) | `foreach` route point → 1 query each |
| `NavlogService` | Sequential HTTP per leg | 1 NOAA API call per leg for magnetic variation |

The `NavlogService` N+1 is partially mitigated by fixing the `MagneticVariationService` cache
(high-priority item #2). The DB-level N+1s could be consolidated into single queries using
multi-point geometries or `ST_DWithin` with `ANY()`.

> **Note:** These methods already comment that `DbContext` is not thread-safe, so parallelization
> isn't an option without separate scopes. Batching into a single query is the correct fix.

### Missing `AsNoTracking()` on read-only queries

Multiple read-only service methods omit `AsNoTracking()`, wasting EF change-tracker overhead:

- `AirportService`: `GetAirports`, `GetAirportByIcaoCodeOrIdent`, `GetAirportsByIcaoCodesOrIdents`
- `AirspaceService`: `GetByClasses`, `GetByCities`, `GetByStates`, `GetByTypeCodes`
- `RunwayService`: `GetRunwaysByAirportAsync`
- `MetarService`: `GetMetarForAirport`
- `TafService`: `GetTafByIcaoCode`

**Fix:** Add `.AsNoTracking()` to all read-only queries. Consider configuring
`QueryTrackingBehavior.NoTracking` as the DbContext default since no service writes via the
API's DbContext.

### Response Compression

No `AddResponseCompression()` / `UseResponseCompression()` is configured.

> **APIM note:** APIM itself does not configure compression in the current policies either.
> Compression could be added at APIM level (preferred — one config point) or at the API level.
> Weather/NOTAM JSON responses can be large and compress well (typically 70–80% reduction).

**Recommendation:** Add compression at the APIM policy level (`<outbound><set-header name="Content-Encoding">`) or enable ASP.NET Core response compression as a fallback.

### HTTP Cache Headers

No `Cache-Control` headers are set on any API responses.

> **APIM note:** APIM handles caching internally and does not forward `Cache-Control` to clients
> based on these policies. However, setting `Cache-Control` on backend responses is still good
> practice — it enables client-side caching, CDN caching, and serves as documentation of data
> freshness expectations. It also means if APIM caching is ever disabled, clients still benefit.

**Recommendation:** Add `[ResponseCache]` attributes to controllers matching APIM cache durations
(e.g., `Duration = 120` on weather endpoints, `Duration = 900` on airport endpoints). Low effort,
meaningful benefit.

---

## Interface Filename Typo

**File:** `PreflightApi.Infrastructure/Interfaces/ICommuncationFrequencyService.cs`

Filename has typo: `ICommuncationFrequencyService.cs` (missing 'i' in Communication). The
interface name *inside* the file is spelled correctly (`ICommunicationFrequencyService`). C#
resolves by type name so it compiles fine, but the filename mismatch is confusing.

**Fix:** Rename file to `ICommunicationFrequencyService.cs`.
