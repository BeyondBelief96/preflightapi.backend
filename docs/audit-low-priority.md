# Audit: Low Priority

Minor optimizations, style nits, and nice-to-haves.

> **Note:** This API sits behind Azure APIM which provides response caching (2–15 min by endpoint),
> per-subscription rate limiting/quotas, and structured error formatting. Several "nice-to-have"
> items below are already covered by APIM and are noted accordingly.

---

## Minor Optimizations

### WindsAloftService.GetStationCoordinates — dictionary allocation

**File:** `PreflightApi.Infrastructure/Services/WeatherServices/WindsAloftService.cs` (line 398)

A ~150-entry `Dictionary<string, (float, float)>` is recreated on every call to the `private static`
method `GetStationCoordinates`. This method is called once per station line during parsing.

**Fix:** Extract to `private static readonly Dictionary<...>` field initialized once.

### PaginationExtensions — keySelector.Compile() per query

**File:** `PreflightApi.Infrastructure/Utilities/PaginationExtensions.cs` (lines 43, 90, 137)

`keySelector.Compile()` is called once per pagination query invocation across all three overloads
(string, Guid, int). Expression compilation invokes the CLR JIT at runtime.

**Fix:** Cache compiled delegates (e.g., `ConcurrentDictionary` keyed by expression string), or
accept both `Expression<Func<T, TKey>>` and `Func<T, TKey>` so callers can pre-compile.

### Azure Functions Program.cs — sync-over-async

**File:** `PreflightApi.Azure.Functions/Program.cs` (lines 160, 176)

Two `.GetAwaiter().GetResult()` calls for `InitializeAsync` during startup. This is a common
pattern in non-async `Main` but risks deadlock in certain synchronization contexts.

**Fix:** Use `async Main` or the async `IHostBuilder` startup pattern if the Functions host
supports it. Low risk as-is since it runs during startup with no sync context.

### GatewaySecretMiddleware — JsonSerializerOptions per-request

**File:** `PreflightApi.API/Middleware/GatewaySecretMiddleware.cs` (lines 60–65)

`new JsonSerializerOptions { ... }` is created on each 403 rejection. `JsonSerializerOptions`
is expensive to construct due to internal reflection cache building.

**Fix:** Extract to `private static readonly JsonSerializerOptions`. Note: this only fires on
rejected requests (gateway secret mismatch) so practical impact is minimal.

### Missing `AsNoTracking()` — additional methods

Beyond the medium-priority list, audit remaining services fnor any read-only queries missig
`AsNoTracking()`. Consider setting `QueryTrackingBehavior.NoTracking` as the DbContext default
in `OnConfiguring` since the API never writes via its DbContext.

---

## Style Nits

### PaginationParams nullability

Standardize `PaginationParams` (cursor, limit) to non-nullable with defaults across all
controllers. Currently some accept nullable params and some don't.

### Unused `using` statement

**File:** `PreflightApi.Infrastructure/Dtos/Navlog/WaypointDto.cs`

Unused `using PreflightApi.Infrastructure.Enums` — remove it.

---

## Nice-to-Haves

### ASP.NET Core Output Caching

> **APIM already provides this.** APIM caches responses for 2–15 min by endpoint category with
> sorted-query-string cache keys. Adding a second cache layer at the API level would add
> complexity for marginal benefit. Only consider this if APIM caching is ever removed or if
> internal service-to-service calls bypass APIM.

### Short in-memory cache on weather read services

> **Partially covered by APIM.** APIM caching prevents redundant backend calls for identical
> requests within the cache window. However, an in-memory cache on individual services (e.g.,
> `MetarService`, `TafService`) could still help when *different* API endpoints fetch overlapping
> data (e.g., briefing service aggregating METARs + TAFs for the same stations). Consider only
> if profiling shows redundant DB queries across endpoints within single requests.

### Expose additional Metar fields

Consider exposing `MetarType`, `VertVisFt`, and `ElevationM` from the `Metar` entity in
`MetarDto`. These are available in the domain model but not surfaced to API consumers.

### `CommunicationFacilityType` enum

The facility type field has a finite FAA-defined set of values, but the set is large. Creating
an enum would improve type safety but requires maintenance when the FAA adds types. Consider
only if consumers request it.

### Mapperly source generator

`AirportMapper` (146 lines) and `RunwayMapper` (460 lines) are hand-written. Mapperly could
auto-generate these with compile-time source generation, reducing boilerplate and improving
performance. Consider when mapper maintenance becomes a pain point.
