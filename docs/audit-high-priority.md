# Audit: High Priority

Issues affecting correctness, type safety, or significant performance impact.

> **Note:** This API sits behind Azure APIM which provides response caching (2-15 min by endpoint),
> per-subscription rate limiting, and structured error formatting. Items below are scoped to the
> backend API itself -- APIM does not mitigate these.

---

## Performance Fixes

### 1. NpgsqlDataSource rebuilt per scope

**File:** `PreflightApi.API/Program.cs` (lines 135-157)

The `AddDbContext` factory creates a new `NpgsqlDataSourceBuilder().Build()` on every scoped
resolution (i.e., every HTTP request). `NpgsqlDataSource` is a heavyweight connection-pooling
object that should be built once.

```csharp
// Current -- runs per request
builder.Services.AddDbContext<PreflightApiDbContext>((serviceProvider, options) =>
{
    var dbSettings = serviceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value;
    var connectionString = dbSettings.GetConnectionString();

    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
    dataSourceBuilder.UseNetTopologySuite();
    dataSourceBuilder.EnableDynamicJson();

    options.UseNpgsql(dataSourceBuilder.Build(), npgsqlOptions => { ... });
}, ServiceLifetime.Scoped);
```

**Fix:** Build the `NpgsqlDataSource` once as a singleton and inject it into the DbContext factory:

```csharp
builder.Services.AddSingleton(sp =>
{
    var dbSettings = sp.GetRequiredService<IOptions<DatabaseSettings>>().Value;
    var dsBuilder = new NpgsqlDataSourceBuilder(dbSettings.GetConnectionString());
    dsBuilder.UseNetTopologySuite();
    dsBuilder.EnableDynamicJson();
    return dsBuilder.Build();
});

builder.Services.AddDbContext<PreflightApiDbContext>((sp, options) =>
{
    var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
    options.UseNpgsql(dataSource, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(3);
        npgsqlOptions.CommandTimeout(30);
        npgsqlOptions.UseNetTopologySuite();
    });
});
```

---

Double check that there's not a reason were doing this like for integration/unit testing, and if so consider how we can adjust that so we still
get the benefits of a singleton datasource in production while allowing for test isolation.

### 2. MagneticVariationService cache is per-request

**Files:**
- `PreflightApi.Infrastructure/Services/MagneticVariationService.cs` (line 28)
- `PreflightApi.API/Program.cs` (line 189 -- registered as `Scoped`)

The service creates `new MemoryCache(new MemoryCacheOptions())` in its constructor. Because
it's registered as Scoped, the cache is destroyed after every request. Every navlog computation
hits the NOAA magnetic variation API N times (once per leg) with zero cache benefit.

**Fix:** Inject `IMemoryCache` (from `AddMemoryCache()`) instead of creating a private instance.
Also consider changing the registration to Singleton or at minimum ensuring the cache outlives the
request scope.

---

## Missing CancellationToken Propagation

10 service interfaces (~31 endpoint methods) do not accept `CancellationToken`, meaning client
disconnects cannot cancel in-flight database queries or HTTP calls.

| Interface | File | Methods Missing CT |
|---|---|---|
| `IAirportService` | `Interfaces/IAirportService.cs` | All 4: `GetAirports`, `GetAirportByIcaoCodeOrIdent`, `GetAirportsByIcaoCodesOrIdents`, `SearchNearby` |
| `IAirspaceService` | `Interfaces/IAirspaceService.cs` | 7 of 9: `GetByClasses`, `GetByCities`, `GetByStates`, `GetByTypeCodes`, `GetByIcaoOrIdents`, `GetByGlobalIds`, `GetSpecialUseByGlobalIds` |
| `IMetarService` | `Interfaces/IMetarService.cs` | All 3: `GetMetarForAirport`, `GetMetarsForAirports`, `GetMetarsByStates` |
| `ITafService` | `Interfaces/ITafService.cs` | All 2: `GetTafByIcaoCode`, `GetTafsForAirports` |
| `IRunwayService` | `Interfaces/IRunwayService.cs` | All 3: `GetRunwaysByAirportAsync`, `GetRunways`, `SearchNearby` |
| `IObstacleService` | `Interfaces/IObstacleService.cs` | 4 of 5: `GetByOasNumber`, `GetByOasNumbers`, `SearchNearby`, `GetByState`, `GetByBoundingBox` |
| `INavaidService` | `Interfaces/INavaidService.cs` | All 4: `GetNavaids`, `GetNavaidsByIdentifier`, `GetNavaidsByIdentifiers`, `SearchNearby` |
| `IChartSupplementService` | `Interfaces/IChartSupplementService.cs` | 1: `GetChartSupplementsByAirportCode` |
| `ITerminalProcedureService` | `Interfaces/ITerminalProcedureService.cs` | 1: `GetTerminalProceduresByAirportCode` |
| `ICommunicationFrequencyService` | `Interfaces/ICommuncationFrequencyService.cs` | 1: `GetFrequenciesByServicedFacility` |

**Fix:** Add `CancellationToken cancellationToken = default` to each interface method, propagate
through implementations to all `ToListAsync()`, `FirstOrDefaultAsync()`, and `ToPaginatedAsync()`
calls, and accept `CancellationToken` in corresponding controller actions.

---

## Missing Batch Size Validation

**File:** `PreflightApi.API/Controllers/AirportController.cs` (line 128)

`GetAirportsBatch` accepts an unbounded comma-separated `ids` parameter with no size cap. All
other batch endpoints enforce limits (100-1000). A caller can pass thousands of ICAO codes in a
single request.

**Fix:** Add validation capping the array size (e.g., 100) after splitting, consistent with other
batch endpoints.

---

## Enum Safety for DTO Type Contracts

### Design Principles

1. **Entities store strings in the database.** Entity properties that map to constrained values
   (e.g., `SiteTypeCode`, `Classification`, `FlightCategory`) remain `string?` in domain entities
   and PostgreSQL columns. This ensures we never lose data from the FAA source, even when the FAA
   adds new values we haven't seen yet.

2. **DTOs expose enums on the API contract.** DTO properties use enum types to communicate the
   known set of values to API consumers. This provides compile-time safety, self-documenting
   Swagger/OpenAPI docs, and version-controlled data contracts.

3. **Nullable enums as the fallback strategy.** Enum properties on DTOs should be **nullable**
   (`EnumType?`). When a mapper encounters a value that doesn't match any enum member, it returns
   `null` and **logs a warning** including the unexpected value and the field name. This means:
   - Enum types only contain **actual known values** from the FAA/NOAA source -- no synthetic
     `Unknown` member polluting the enum definition
   - `null` cleanly represents both "source left this blank" and "we got a value we couldn't parse"
     -- API consumers already handle nullable fields
   - The rest of the record still parses correctly -- we never drop an entire NOTAM, METAR, or
     airport record because one field has an unexpected value
   - We get alerted to data drift via logged warnings

   **Why not `Unknown`?** An `Unknown` enum member conflates two different meanings: "the FAA
   didn't provide this value" vs. "the FAA sent a value we don't recognize." For some fields
   (e.g., `AirportFacilityUse`), "Unknown" is actually a valid FAA value meaning the facility
   use couldn't be determined -- it doesn't mean we failed to parse. Using `null` for parse
   failures keeps enum values clean for actual source values only.

   **Required vs. optional fields:** For fields that are **always** present in the source data
   and have a well-defined, stable value set (e.g., `FlightCategory` on a METAR), the enum
   property can be non-nullable (`EnumType` not `EnumType?`). But this should be the exception
   -- most fields should be nullable to handle missing data gracefully.

4. **Logging pattern for unrecognized values.** Every mapper that converts `string -> enum` must
   log when it hits the `_ =>` default arm:
   ```csharp
   _ => {
       logger.LogWarning("Unrecognized {FieldName} value: '{RawValue}' for {EntityType} {EntityId}",
           nameof(SiteTypeCode), code, nameof(Airport), airport.SiteNo);
       return null;
   }
   ```
   This requires mappers to accept `ILogger` (or a shared `EnumParseHelper` that logs centrally).

### Existing Pattern (Needs Migration)

The `AirportMapper` already implements string-to-enum mapping for ~12 NASR fields. The NASR data
types (Airport, Runway, RunwayEnd, Navaid, Obstacle) already have enums in
`PreflightApi.Domain/Enums/` with `[JsonConverter(typeof(JsonStringEnumConverter))]`. However,
most of these enums currently have an `Unknown` member that needs to be removed per Design
Principle #3 above.

**Migration steps for existing enums:**
1. Remove the `Unknown` member from each enum (26 enums total -- see list below)
2. Change DTO properties from `EnumType` to `EnumType?` (nullable)
3. Update mapper `_ =>` arms from `return EnumType.Unknown` to `return null` with warning log
4. Verify no downstream code relies on `== EnumType.Unknown` checks (replace with `== null`)

**Enums that already follow the correct nullable pattern (no changes needed):**
- `GAirmetHazardType` -- used as `GAirmetHazardType?` on `GAirmetDto.Hazard`, no `Unknown` member
- `SigmetHazardType` -- used as `SigmetHazardType?` on `SigmetHazardDto.Type`, no `Unknown` member
- `VorServiceVolume` -- used as `VorServiceVolume?` on `NavaidDto.AltCode`, no `Unknown` member
- `DmeServiceVolume` -- used as `DmeServiceVolume?` on `NavaidDto.DmeSsv`, no `Unknown` member

**Enums with `Unknown` member to remove (migrate to nullable):**

| Enum | DTO Property | Currently Non-Nullable? |
|---|---|---|
| `AirportSiteType` | `AirportDto.SiteType` | Yes |
| `OwnershipType` | `AirportDto.OwnershipType` | Yes |
| `AirportFacilityUse` | `AirportDto.FacilityUse` | Yes |
| `ArptStatus` | `AirportDto.ArptStatus` | Yes |
| `SurveyMethod` | `AirportDto.PositionSurveyMethod`, `.ElevationSurveyMethod` | Yes |
| `InspectionMethod` | `AirportDto.InspectionMethod` | Yes |
| `InspectorAgency` | `AirportDto.InspectorAgency` | Yes |
| `AirframeRepairService` | `AirportDto.AirframeRepairService` | Yes |
| `PowerPlantRepairService` | `AirportDto.PowerPlantRepairService` | Yes |
| `OxygenType` | `AirportDto.BottledOxygenType`, `.BulkOxygenType` | Yes |
| `BeaconLensColor` | `AirportDto.BeaconLensColor` | Yes |
| `SegmentedCircleMarker` | `AirportDto.SegmentedCircleMarker` | Yes |
| `WindIndicator` | `AirportDto.WindIndicator` | Yes |
| `SurfaceType` | `RunwayDto.SurfaceType` | Yes |
| `SurfaceTreatment` | `RunwayDto.SurfaceTreatment` | Yes |
| `EdgeLightIntensity` | `RunwayDto.EdgeLightIntensity` | Yes |
| `ApproachType` | `RunwayEndDto.ApproachType` | Yes |
| `MarkingsType` | `RunwayEndDto.MarkingsType` | Yes |
| `MarkingsCondition` | `RunwayEndDto.MarkingsCondition` | Yes |
| `VisualGlideSlopeIndicator` | `RunwayEndDto.VisualGlideSlopeIndicator` | Yes |
| `RunwayVisualRangeEquipment` | `RunwayEndDto.RunwayVisualRangeEquipment` | Yes |
| `ApproachLightSystem` | `RunwayEndDto.ApproachLightSystem` | Yes |
| `ControllingObjectMarking` | `RunwayEndDto.ControllingObjectMarking` | Yes |
| `ObstacleLighting` | `ObstacleDto.Lighting` | Yes |
| `HorizontalAccuracy` | `ObstacleDto.HorizontalAccuracy` | Yes |
| `VerticalAccuracy` | `ObstacleDto.VerticalAccuracy` | Yes |
| `ObstacleMarking` | `ObstacleDto.Marking` | Yes |
| `VerificationStatus` | `ObstacleDto.VerificationStatus` | Yes |
| `NavaidType` | `NavaidDto.NavType` | Yes |

> **Special case: `AirportFacilityUse`** -- The FAA NASR data includes a legitimate "Unknown"
> facility use value (not a parse failure). If we want to represent this as an enum member, name
> it something distinct like `Undetermined` to differentiate it from a parse failure (which is
> `null`). Same treatment for any other enum where the FAA explicitly uses "Unknown" as a valid
> value.

**Entities with enums already on their DTOs (need logging + nullable migration):**

| DTO | Enum Properties |
|---|---|
| `AirportDto` | `SiteType`, `OwnershipType`, `FacilityUse`, `ArptStatus`, `PositionSurveyMethod`, `ElevationSurveyMethod`, `InspectionMethod`, `InspectorAgency`, `AirframeRepairService`, `PowerPlantRepairService`, `BottledOxygenType`, `BulkOxygenType`, `BeaconLensColor`, `SegmentedCircleMarker`, `WindIndicator` |
| `RunwayDto` | `SurfaceType`, `SurfaceTreatment`, `EdgeLightIntensity` |
| `RunwayEndDto` | `ApproachType`, `MarkingsType`, `MarkingsCondition`, `VisualGlideSlopeIndicator`, `RunwayVisualRangeEquipment`, `ApproachLightSystem`, `ControllingObjectMarking` |
| `ObstacleDto` | `Lighting`, `HorizontalAccuracy`, `VerticalAccuracy`, `Marking`, `VerificationStatus` |
| `NavaidDto` | `NavType`, `AltCode` (VorServiceVolume), `DmeSsv` (DmeServiceVolume) |
| `GAirmetDto` | `Product` (GAirmetProduct), `Hazard` (GAirmetHazardType) |
| `SigmetHazardDto` | `Type` (SigmetHazardType) |

### New Enums Needed

These DTO properties currently use `string?` but represent constrained, documented value sets. Create
enums and update the mappers.

| Enum to Create | Values | Used By | Source Authority |
|---|---|---|---|
| `FlightCategory` | `VFR`, `MVFR`, `IFR`, `LIFR` | `MetarDto.FlightCategory` | NOAA AvWx |
| `PirepReportType` | `UA`, `UUA` | `PirepDto.ReportType` | NOAA AvWx |
| `SkyCover` | `SKC`, `CLR`, `FEW`, `SCT`, `BKN`, `OVC`, `OVX` | `MetarSkyConditionDto.SkyCover`, `PirepSkyCondition.SkyCover`, `TafSkyCondition.SkyCover` | NOAA AvWx |
| `TerminalProcedureChartCode` | `IAP`, `DP`, `STAR`, `APD`, `MIN`, `HOT` | `TerminalProcedureDto.ChartCode` | FAA d-TPP |
| `HazardSeverity` | `LGT`, `LT_MOD`, `MOD`, `MOD_SEV`, `SEV` | `SigmetHazardDto.Severity`, `GAirmetDto.HazardSeverity` | NOAA AvWx |
| `NotamClassification` | `INTERNATIONAL`, `MILITARY`, `LOCAL_MILITARY`, `DOMESTIC`, `FDC` | `NotamDetailDto.Classification` | FAA NMS API |
| `NotamType` | `N` (New), `R` (Replace), `C` (Cancel) | `NotamDetailDto.Type` | FAA NMS API |
| `NotamFeature` | `RWY`, `TWY`, `APRON`, `AD`, `OBST`, `NAV`, `COM`, `SVC`, `AIRSPACE`, `ODP`, `SID`, `STAR`, `CHART`, `DATA`, `DVA`, `IAP`, `VFP`, `ROUTE`, `SPECIAL`, `SECURITY` | `NotamDetailDto.Feature` | FAA NMS API |
| `NotamTraffic` | `I`, `V`, `IV` | `NotamDetailDto.Traffic` | FAA NMS API |
| `NotamScope` | `A` (Aerodrome), `E` (En-route), `W` (Warning) | `NotamDetailDto.Scope` | FAA NMS API |

**NOTE on NOTAM enums:** The NOTAM pipeline stores the full GeoJSON Feature as a JSONB column
(`feature_json`), then deserializes it back to `NotamDto` at query time. There is no traditional
entity-to-DTO mapper -- the round-trip is: `NotamDto -> serialize to JSONB -> store -> deserialize
back to NotamDto`. This means enum conversion must happen **after deserialization**, either:
- In a post-processing step in `NotamService` before returning results, or
- By using a custom `JsonConverter` on the DTO enum properties that returns `null` (with warning
  log) for unrecognized string values instead of throwing a `JsonException`

The GeoJSON source data uses string values. Whichever approach we use, we must never fail to parse
a NOTAM just because the FAA added a new classification or feature code.

**Additional enum candidates (lower priority):**
- `RunwayDto.SurfaceCondition` -- values: `EXCELLENT`, `GOOD`, `FAIR`, `POOR`, `FAILED` (currently `string?`)
- `GAirmetDto.GeometryType` -- values: `AREA`, `LINE` (currently `string?`)
- `NotamTranslationDto.Type` -- values: `LOCAL_FORMAT`, `ICAO` (currently `string?`)

**Properties that should NOT become enums** (too many values, free-form, or not a true enumeration):
- `NotamDetailDto.Series` -- 14 values but rarely useful for filtering; leave as string
- `AirspaceDto.Class` -- comes from ArcGIS, values vary; leave as string
- `AirspaceDto.TypeCode` -- free-form from ArcGIS; leave as string
- `SpecialUseAirspaceDto.TypeCode` -- free-form from ArcGIS; leave as string
- `AirspaceDto.UpperUom`/`LowerUom`/`UpperCode`/`LowerCode` -- only FT/FL/MSL/AGL but comes
  from ArcGIS JSON with no guarantee of normalization; leave as string

### Implementation Notes

- All new enums go in `PreflightApi.Domain/Enums/` with `[JsonConverter(typeof(JsonStringEnumConverter))]`.
- **Do NOT add an `Unknown` member.** Enums should only contain actual values from the source data.
  Use nullable enum properties (`EnumType?`) on DTOs, with `null` as the fallback for both
  missing and unrecognized values.
- Mappers (in `PreflightApi.Infrastructure/Dtos/Mappers/`) convert string -> enum during
  entity-to-DTO mapping. The `_ =>` arm must log a warning and return `null`.
- Consider creating a shared `EnumParseHelper<TEnum>` to reduce boilerplate:
  ```csharp
  public static TEnum? Parse<TEnum>(string? value, ILogger logger, string fieldName,
      string entityType, string entityId, Dictionary<string, TEnum> mapping) where TEnum : struct, Enum
  ```
- For weather DTOs (Metar, Taf, Pirep) where the DTO doesn't currently go through a mapper
  (the `MetarMapper.ToDto()` does direct property copying), add enum parsing in the mapper.
- Ensure OpenAPI docs reflect the new enum types and their possible values.
- Update schema validation manifests if any validated fields now use enums.
- When removing `Unknown` from existing enums, search for any code that checks
  `== EnumType.Unknown` and replace with `== null` / `is null` checks.

---

## Type Consistency: Numeric Types Across Entities and DTOs

### Problem

There are three different numeric types used for similar data across the codebase:

| Type | Precision | Used For |
|---|---|---|
| `float` (32-bit) | ~7 significant digits (~11m at equator) | Weather entities: Metar, Taf, Pirep coordinates + temps |
| `decimal` (128-bit) | 28-29 significant digits (exact) | NASR entities: Airport, Runway, Navaid, Obstacle coordinates + elevations |
| `double` (64-bit) | ~15 significant digits (sub-mm) | Airspace entities, GAirmet value objects, some DTO-only types |

The core problem: **entity and DTO types must match** because the mapper copies values directly.
Changing only the DTO type (e.g., `float? -> double?`) without changing the entity gains zero
precision -- the data was already truncated when stored.

### Standardization Plan

**Target type: `double`** for all floating-point coordinates, elevations, temperatures, and
measurement values across both entities and DTOs. Rationale:
- `double` provides sub-millimeter precision for geographic coordinates -- more than sufficient
  for aviation
- `double` is the native type for PostGIS `geography`/`geometry` operations via
  NetTopologySuite (`Coordinate` uses `double`)
- `float` is insufficient for coordinates (11m error at equator)
- `decimal` is overkill for coordinates and causes friction with NetTopologySuite (which uses
  `double` internally); `decimal` is better suited for financial calculations where exact
  representation matters

**Exception: keep `decimal` for exact fixed-point FAA data** where the FAA explicitly defines
precision (e.g., latitude seconds as `decimal(6,2)`). The FAA NASR CSV data uses fixed-width
fields with defined decimal places. For these fields, `decimal` correctly preserves the exact
value from the source. See detailed analysis below.

### Detailed Changes

#### Weather Entities and DTOs: `float?` -> `double?`

These store data from NOAA XML where values are already floating-point. `float` is actively
harmful for coordinates.

**Metar entity** (`PreflightApi.Domain/Entities/Metar.cs`):
- `Latitude`, `Longitude`: `float?` -> `double?`
- `TempC`, `DewpointC`, `AltimInHg`, `SeaLevelPressureMb`: `float?` -> `double?`
- `ThreeHrPressureTendencyMb`, `MaxTC`, `MinTC`, `MaxT24hrC`, `MinT24hrC`: `float?` -> `double?`
- `PrecipIn`, `Pcp3hrIn`, `Pcp6hrIn`, `Pcp24hrIn`, `SnowIn`: `float?` -> `double?`
- `ElevationM`: `float?` -> `double?`

**MetarDto**: Update to match entity types.

**Taf entity** (`PreflightApi.Domain/Entities/Taf.cs`):
- `Latitude`, `Longitude`, `ElevationM`: `float?` -> `double?`

**TafForecast** (`PreflightApi.Domain/ValueObjects/Taf/TafForecast.cs`):
- `AltimInHg`: `float?` -> `double?`

**TafTemperature** (`PreflightApi.Domain/ValueObjects/Taf/TafTemperature.cs`):
- `SfcTempC`: `float?` -> `double?`

**TafDto**: Update to match entity types.

**Pirep entity** (`PreflightApi.Domain/Entities/Pirep.cs`):
- `Latitude`, `Longitude`, `TempC`: `float?` -> `double?`

**PirepDto**: Update to match entity types.

**SigmetPoint** (`PreflightApi.Domain/ValueObjects/Sigmets/SigmetPoint.cs`):
- `Latitude`, `Longitude`: `float` -> `double`

**SpecialUseAirspace entity** (`PreflightApi.Domain/Entities/SpecialUseAirspace.cs`):
- `ShapeArea`, `ShapeLength`: `float?` -> `double?`

**TafTemperature** (`PreflightApi.Domain/ValueObjects/Taf/TafTemperature.cs`):
- `MaxTempC`, `MinTempC`: currently `string?` -- should be `double?`. These are temperature values
  that the NOAA XML provides as numeric. Parse during XML deserialization.

#### NASR Entities: Keep `decimal` but ensure DTO matches

NASR entities use `decimal` with explicit `Column(TypeName = "decimal(N,M)")` annotations. These
represent exact fixed-point values from FAA CSV data. **Keep `decimal`** on both entity and DTO.

Verify entity/DTO type matches (currently all match -- `decimal?` on both sides):
- Airport: `LatDecimal`, `LongDecimal`, `LatSec`, `LongSec`, `Elev`, `MagVarn`, `DistCityToAirport` -- all `decimal?` on entity and DTO
- RunwayEnd: `Latitude`, `Longitude`, `Elevation`, `ThresholdCrossingHeight`, `VisualGlidePathAngle`, `RunwayGradient`, DMS seconds, displaced threshold coords, LAHSO coords -- all `decimal?` on entity and DTO
- Navaid: `Latitude`, `Longitude`, `Elevation`, `Frequency`, `TacanDmeLatitude`, `TacanDmeLongitude` -- all `decimal?` on entity and DTO
- Obstacle: `Latitude`, `Longitude` -- `decimal?` on entity and DTO
- CommunicationFrequency: `Latitude`, `Longitude` -- `decimal?` on entity and DTO

#### SpecialUseAirspaceDto: `string?` -> `double?` for altitude values

**File:** `PreflightApi.Infrastructure/Dtos/AirspaceDto.cs` (lines 92, 100)

`AirspaceDto` uses `double?` for `UpperVal`/`LowerVal` but `SpecialUseAirspaceDto` uses `string?`.
Both come from ArcGIS JSON where the values are numeric. Parse to `double?` in the mapper for
consistency. Check the entity type (`SpecialUseAirspace`) to confirm it stores these as a numeric
type or as string, and align the DTO accordingly.

#### Database Migration

Changing `float` -> `double` on entities that map to PostgreSQL columns will require a migration.
PostgreSQL `real` (float4) -> `double precision` (float8). This is a non-destructive widening
conversion -- existing data is preserved. Plan:
1. Update entity types
2. Generate EF Core migration
3. Verify migration SQL is `ALTER COLUMN ... TYPE double precision`
4. Apply in staging, verify data integrity, then production

---

## Type Fixes

### `NotamDetailDto.Estimated`: `string?` -> `bool?`

**File:** `PreflightApi.Infrastructure/Dtos/Notam/NotamDto.cs` (line 219)

Currently `string?` holding `"true"` or `"false"`. Per the NMS API GeoJSON sample data
(`nms-api.yaml` line 1160: `"estimated": "true"`), the source sends these as string literals.
Convert to `bool?` in the mapper when mapping from entity to DTO. The Notam entity column should
remain `string?` (or `bool?` if it's already been converted). Parse `"true"` -> `true`,
`"false"` -> `false`, null/empty -> `null`.

### `MetarQualityControlFlags`: 8 `string?` properties -> `bool?`

**Files:**
- Domain: `PreflightApi.Domain/ValueObjects/Metar/MetarQualityControlFlags.cs` (8 `string?` props)
- DTO: `PreflightApi.Infrastructure/Dtos/MetarDto.cs` (`MetarQualityControlFlagsDto`, line 49)

All 8 flags (`Corrected`, `Auto`, `AutoStation`, `MaintenanceIndicatorOn`, `NoSignal`,
`LightningSensorOff`, `FreezingRainSensorOff`, `PresentWeatherSensorOff`) are semantic booleans
stored as strings. Both the entity value object and the DTO should become `bool?`. The NOAA XML
source sends `"TRUE"` for present flags and omits them when absent, so:
- Entity: Change to `bool?`, parse `"TRUE"` -> `true` during XML deserialization
- DTO: Change to `bool?`, direct copy from entity in mapper
- Database migration: `text` -> `boolean` (or keep as `text` in DB and parse in the entity
  mapping -- evaluate which is cleaner)
