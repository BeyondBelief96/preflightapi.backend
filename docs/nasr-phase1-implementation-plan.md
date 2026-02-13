# Phase 1 Implementation Plan: NAV, FIX, and AWOS NASR Datasets

## Context

The PreflightApi currently ingests 2 NASR datasets (APT for airports, FRQ for communication frequencies). This plan adds 3 new NASR datasets — **Navigation Aids (NAV)**, **Fixes/Reporting Points (FIX)**, and **Weather Stations (AWOS)** — following the established `FaaNasrBaseService<T>` pattern. All sub-files within each dataset will be mapped (not just base files), mirroring the Airport pattern where supplementary CSV files add properties to the same entity.

## NASR Data Prioritization (Full Analysis)

### What We Already Support
| Dataset | Source |
|---------|--------|
| Airports (APT) | NASR CSV |
| Runways / Runway Ends | NASR CSV |
| Communication Frequencies (FRQ) | NASR CSV |
| Controlled Airspace (B/C/D) | ArcGIS |
| Special Use Airspace (MOAs, Restricted) | ArcGIS |
| Obstacles (>200ft AGL) | FAA DOF |
| NOTAMs | FAA NMS API |
| Weather (METAR/TAF/PIREP/AIRMET/SIGMET/G-AIRMET) | NOAA |
| Airport Diagrams & Chart Supplements | FAA AeroNav |

### Available NASR CSV Datasets (Prioritized for VFR Pilots)

#### Tier 1 — High Value (Phase 1 — This Plan)
1. **NAV** — Navigation Aids (VOR, NDB, TACAN, DME). Essential for VFR navigation. Complements Navlog service.
2. **FIX** — Fixes/Reporting Points (~74,000 waypoints). Enhances flight planning, referenced in NOTAMs and on charts.
3. **AWOS** — Weather Stations. Ties into existing weather coverage with station frequencies and phone numbers.

#### Tier 2 — Safety-Critical (Phase 2)
4. **PJA** — Parachute Jump Areas. Safety hazard for VFR pilots. Small dataset, quick win.
5. **MTR** — Military Training Routes. High-speed military jet routes. Multi-point geometry.

#### Tier 3 — Enhanced Airport Data (Phase 3)
6. **TWR** — ATC Tower Data. Tower hours, radar capabilities, ATIS. Complements FRQ data.
7. **ILS** — Instrument Landing Systems. Useful for VFR pilots transitioning to IFR.
8. **HPF** — Holding Patterns. IFR-oriented but useful for training.

#### Tier 4 — IFR Expansion (Phase 4)
9. **AWY** — Airways (Victor/Jet routes)
10. **STARDP** — SID/STAR procedures
11. **COM/PFR/CDR/FSS/WXL/ARB/LID/MAA/MIL_OPS/RDR** — Administrative/niche

---

## Step 0: Discover CSV File Structure

Before writing any code, download the actual CSV ZIPs to determine exact file names and column headers.

```bash
# Download and inspect NAV, FIX, AWOS CSV ZIPs
# URL pattern: https://nfdc.faa.gov/webContent/28DaySub/extra/{DD_Mmm_YYYY}_{TYPE}_CSV.zip
# Check current publication date from FaaPublicationCycleService logic
```

**Action:** Download the 3 ZIPs, extract, list CSV files, and capture the header rows. This determines the exact `ClassMap` column names. The plan below uses inferred names based on layout files and existing conventions — adjust if actual CSV headers differ.

**Expected CSV files per ZIP (verify):**

| ZIP | Expected CSV Files |
|-----|-------------------|
| NAV | NAV_BASE.csv, NAV_RMK.csv, NAV_FIX.csv, NAV_HPF.csv, NAV_FAN.csv, NAV_CHK.csv |
| FIX | FIX_BASE.csv, FIX_NAV.csv, FIX_ILS.csv, FIX_RMK.csv, FIX_CHT.csv |
| AWOS | AWOS.csv, AWOS_RMK.csv |

**Key decision for one-to-many sub-files:** NAV sub-files (FIX, HPF, FAN, CHK) contain multiple rows per NAVAID. Unlike APT_ATT/APT_CON which add scalar properties, these are child collections. Options:
- **Supplementary text merge**: Concatenate remarks into a text field on the parent entity (for RMK files)
- **Separate child entities**: Model as separate DB tables with FK back to parent (for FIX, HPF, FAN, CHK files — similar to Runway → Airport)

---

## Step 1: Shared Infrastructure Changes

### 1a. Add NasrDataType enum values
**File:** `PreflightApi.Infrastructure/Enums/NasrDataType.cs`
```csharp
public enum NasrDataType
{
    APT,
    FRQ,
    NAV,   // Navigation Aids
    FIX,   // Fixes/Reporting Points
    AWOS   // Automated Weather Observing Systems
}
```

### 1b. Add PublicationType enum values
**File:** `PreflightApi.Domain/ValueObjects/FaaPublications/PublicationType.cs`
```csharp
NasrSubscription_NavigationalAids = 7,
NasrSubscription_Fixes = 8,
NasrSubscription_WeatherStations = 9,
```

### 1c. Seed publication cycles in DbInitializer
**File:** `PreflightApi.Infrastructure/Data/DbInitializer.cs`

Add 3 new entries to `expectedRecords`:
```csharp
{ PublicationType.NasrSubscription_NavigationalAids, (7, 28, new DateTime(2025, 1, 23, 0, 0, 0, DateTimeKind.Utc)) },
{ PublicationType.NasrSubscription_Fixes, (8, 28, new DateTime(2025, 1, 23, 0, 0, 0, DateTimeKind.Utc)) },
{ PublicationType.NasrSubscription_WeatherStations, (9, 28, new DateTime(2025, 1, 23, 0, 0, 0, DateTimeKind.Utc)) },
```

### 1d. Add DbSets to DbContext
**File:** `PreflightApi.Infrastructure/Data/PreflightApiDbContext.cs`
```csharp
public virtual DbSet<NavigationalAid> NavigationalAids => Set<NavigationalAid>();
public virtual DbSet<Fix> Fixes => Set<Fix>();
public virtual DbSet<WeatherStation> WeatherStations => Set<WeatherStation>();
```

---

## Step 2: NavigationalAid (NAV) Dataset

### Entity Properties (from NAV1 layout fields)
**New file:** `PreflightApi.Domain/Entities/NavigationalAid.cs`

| Property | Type | CSV Column (inferred) | Source |
|----------|------|----------------------|--------|
| Id | Guid (PK, auto) | — | Generated |
| NavAidId | string | NAV_ID or IDENT | NAV_BASE |
| FacilityType | string | FAC_TYPE | NAV_BASE |
| EffectiveDate | DateTime | EFF_DATE | NAV_BASE |
| Name | string | NAME | NAV_BASE |
| City | string | CITY | NAV_BASE |
| StateCode | string | STATE_CODE | NAV_BASE |
| StateName | string | STATE_NAME | NAV_BASE |
| FaaRegion | string | FAA_REGION | NAV_BASE |
| Country | string | COUNTRY | NAV_BASE |
| OwnerName | string | OWNER_NAME | NAV_BASE |
| OperatorName | string | OPERATOR_NAME | NAV_BASE |
| CommonSystemUsage | string | COMMON_SYSTEM_USAGE | NAV_BASE |
| PublicUse | string | PUBLIC_USE | NAV_BASE |
| NavAidClass | string | CLASS | NAV_BASE |
| HoursOfOperation | string | HOURS_OF_OPERATION | NAV_BASE |
| HighArtcc | string | HIGH_ARTCC | NAV_BASE |
| LowArtcc | string | LOW_ARTCC | NAV_BASE |
| LatitudeFormatted | string | LAT_FORMATTED | NAV_BASE |
| LongitudeFormatted | string | LONG_FORMATTED | NAV_BASE |
| LatDecimal | decimal? | LAT_DECIMAL | NAV_BASE |
| LongDecimal | decimal? | LONG_DECIMAL | NAV_BASE |
| SurveyAccuracy | string | SURVEY_ACCURACY | NAV_BASE |
| Elevation | decimal? | ELEV | NAV_BASE |
| MagneticVariation | decimal? | MAG_VAR | NAV_BASE |
| MagVarEpochYear | string | MAG_VAR_EPOCH_YEAR | NAV_BASE |
| SimultaneousVoice | string | SIMULTANEOUS_VOICE | NAV_BASE |
| PowerOutput | string | POWER_OUTPUT | NAV_BASE |
| FrequencyOrChannel | string | FREQ or CHANNEL | NAV_BASE |
| TransmittedIdentifier | string | TRANSMITTED_ID | NAV_BASE |
| RadioVoiceCall | string | RADIO_VOICE_CALL | NAV_BASE |
| MonitoringCategory | string | MONITORING_CATEGORY | NAV_BASE |
| NavAidStatus | string | STATUS | NAV_BASE |
| PitchFlag | string | PITCH_FLAG | NAV_BASE |
| CatchFlag | string | CATCH_FLAG | NAV_BASE |
| SuaAtcaaFlag | string | SUA_ATCAA_FLAG | NAV_BASE |
| RestrictionFlag | string | RESTRICTION_FLAG | NAV_BASE |
| HiwasFlag | string | HIWAS_FLAG | NAV_BASE |
| TwebRestriction | string | TWEB_RESTRICTION | NAV_BASE |
| Remarks | string | — | NAV_RMK (aggregated) |

**Table:** `navigational_aids`
**Unique key:** `NavAidId + FacilityType` (composite — same identifier can exist for different facility types)
**Indexes:** NavAidId, FacilityType, StateCode, (LatDecimal, LongDecimal)

### Supplementary Files Handling

| CSV File | Strategy | Notes |
|----------|----------|-------|
| NAV_RMK | Supplementary → merge `Remarks` text field | Concatenate multiple remark rows into single field |
| NAV_FIX | **Defer** — associated fixes will be derivable from the Fix entity | Cross-reference, not unique data |
| NAV_HPF | **Defer** — holding patterns are Phase 4 (IFR) | Would need child entity |
| NAV_FAN | **Skip** — fan markers are largely deprecated | Minimal user value |
| NAV_CHK | **Defer** — VOR checkpoints are niche | Would need child entity |

**Rationale:** NAV_RMK adds direct value as a text field. NAV_FIX is redundant once we have the Fix entity. NAV_HPF/FAN/CHK are child collections better suited for a follow-up when we implement holding patterns (Phase 4). The CronService's `CsvMappings` can be extended later to include these.

### Files to Create/Modify

| File | Action | Path |
|------|--------|------|
| NavigationalAid.cs | **Create** | `PreflightApi.Domain/Entities/` |
| NavigationalAidMap.cs | **Create** | `PreflightApi.Infrastructure/Services/CronJobServices/NasrServices/Mappings/` |
| NavigationalAidRemarkMap.cs | **Create** | Same directory (supplementary map for NAV_RMK) |
| NavigationalAidConfiguration.cs | **Create** | `PreflightApi.Infrastructure/Data/Configurations/` |
| NavigationalAidCronService.cs | **Create** | `PreflightApi.Infrastructure/Services/CronJobServices/NasrServices/` |
| INavigationalAidCronService.cs | **Create** | `PreflightApi.Infrastructure/Interfaces/` |
| NavigationalAidFunction.cs | **Create** | `PreflightApi.Azure.Functions/Functions/` |
| NavigationalAidDto.cs | **Create** | `PreflightApi.Infrastructure/Dtos/` |
| NavigationalAidMapper.cs | **Create** | `PreflightApi.Infrastructure/Dtos/Mappers/` |
| INavigationalAidService.cs | **Create** | `PreflightApi.Infrastructure/Interfaces/` |
| NavigationalAidService.cs | **Create** | `PreflightApi.Infrastructure/Services/AirportInformationServices/` |
| NavigationalAidController.cs | **Create** | `PreflightApi.API/Controllers/` |

### CronService Pattern
```csharp
public class NavigationalAidCronService : FaaNasrBaseService<NavigationalAid>, INavigationalAidCronService
{
    protected override NasrDataType DataType => NasrDataType.NAV;
    protected override string[] UniqueIdentifiers => new[] { "NavAidId", "FacilityType" };
    protected override PublicationType PublicationType => PublicationType.NasrSubscription_NavigationalAids;
    protected override IEnumerable<(string FileName, Type ClassMap, bool IsBaseData)> CsvMappings => new[]
    {
        ("NAV_BASE.csv", typeof(NavigationalAidMap), true),
        ("NAV_RMK.csv", typeof(NavigationalAidRemarkMap), false),
    };
    protected override bool UsesLegacySiteNoDeduplication => false;
}
```

### Azure Function
```csharp
[Function("NavigationalAidFunction")]
[TimerTrigger("0 0 2 * * *")]  // Daily at 2 AM UTC
```

### API Endpoints
```
GET /api/v1/navaids                           — List all (paginated, optional ?search=)
GET /api/v1/navaids/{identifier}              — By NAVAID identifier (e.g., DFW, BUJ)
GET /api/v1/navaids/type/{facilityType}       — Filter by type (VOR, VORTAC, NDB, TACAN, DME)
GET /api/v1/navaids/state/{stateCode}         — Filter by state
```

---

## Step 3: Fix/Reporting Point (FIX) Dataset

### Entity Properties (from FIX1 layout fields)
**New file:** `PreflightApi.Domain/Entities/Fix.cs`

| Property | Type | CSV Column (inferred) | Source |
|----------|------|----------------------|--------|
| Id | Guid (PK, auto) | — | Generated |
| FixId | string | FIX_ID | FIX_BASE |
| StateName | string | STATE_NAME | FIX_BASE |
| IcaoRegion | string | ICAO_REGION | FIX_BASE |
| LatitudeFormatted | string | LAT_FORMATTED | FIX_BASE |
| LongitudeFormatted | string | LONG_FORMATTED | FIX_BASE |
| LatDecimal | decimal? | LAT_DECIMAL | FIX_BASE |
| LongDecimal | decimal? | LONG_DECIMAL | FIX_BASE |
| FixCategory | string | CATEGORY | FIX_BASE |
| FixUse | string | FIX_USE | FIX_BASE |
| NasIdentifier | string | NAS_ID | FIX_BASE |
| HighArtcc | string | HIGH_ARTCC | FIX_BASE |
| LowArtcc | string | LOW_ARTCC | FIX_BASE |
| Country | string | COUNTRY | FIX_BASE |
| PitchFlag | string | PITCH_FLAG | FIX_BASE |
| CatchFlag | string | CATCH_FLAG | FIX_BASE |
| SuaAtcaaFlag | string | SUA_ATCAA_FLAG | FIX_BASE |
| Published | string | PUBLISHED | FIX_BASE |
| PreviousName | string | PREVIOUS_NAME | FIX_BASE |
| ChartingInfo | string | CHARTING_INFO | FIX_BASE |
| NavAidComponent | string | — | FIX_NAV (supplementary) |
| IlsComponent | string | — | FIX_ILS (supplementary) |
| Remarks | string | — | FIX_RMK (aggregated) |
| ChartPublications | string | — | FIX_CHT (supplementary) |

**Table:** `fixes`
**Unique key:** `FixId + StateName` (composite — same fix name in different states)
**Indexes:** FixId, NasIdentifier, StateName, FixCategory, (LatDecimal, LongDecimal)

### Supplementary Files Handling

| CSV File | Strategy | Notes |
|----------|----------|-------|
| FIX_NAV | Supplementary → merge NavAidComponent field | NAVAID makeup text |
| FIX_ILS | Supplementary → merge IlsComponent field | ILS makeup text |
| FIX_RMK | Supplementary → merge Remarks field | Concatenate multiple remark rows |
| FIX_CHT | Supplementary → merge ChartPublications field | Charting info text |

All FIX sub-files add textual properties to the same Fix entity — this follows the Airport supplementary pattern exactly.

### Files to Create/Modify

Same pattern as NAV (12 files): Entity, Map(s), Configuration, CronService, Interface, Azure Function, DTO, Mapper, API Service, API Interface, Controller.

### CronService Pattern
```csharp
public class FixCronService : FaaNasrBaseService<Fix>, IFixCronService
{
    protected override NasrDataType DataType => NasrDataType.FIX;
    protected override string[] UniqueIdentifiers => new[] { "FixId", "StateName" };
    protected override PublicationType PublicationType => PublicationType.NasrSubscription_Fixes;
    protected override IEnumerable<(string FileName, Type ClassMap, bool IsBaseData)> CsvMappings => new[]
    {
        ("FIX_BASE.csv", typeof(FixMap), true),
        ("FIX_NAV.csv", typeof(FixNavMap), false),
        ("FIX_ILS.csv", typeof(FixIlsMap), false),
        ("FIX_RMK.csv", typeof(FixRemarkMap), false),
        ("FIX_CHT.csv", typeof(FixChartMap), false),
    };
    protected override bool UsesLegacySiteNoDeduplication => false;
}
```

### Azure Function
```csharp
[Function("FixFunction")]
[TimerTrigger("0 0 3 * * *")]  // Daily at 3 AM UTC
```

### API Endpoints
```
GET /api/v1/fixes                             — List all (paginated, optional ?search=)
GET /api/v1/fixes/{identifier}                — By fix identifier (NAS ID)
GET /api/v1/fixes/state/{stateCode}           — Filter by state
GET /api/v1/fixes/category/{category}         — Filter by category (MIL, FIX)
```

---

## Step 4: Weather Station (AWOS) Dataset

### Entity Properties (from AWOS1 layout fields)
**New file:** `PreflightApi.Domain/Entities/WeatherStation.cs`

| Property | Type | CSV Column (inferred) | Source |
|----------|------|----------------------|--------|
| Id | Guid (PK, auto) | — | Generated |
| WxSensorIdent | string | WX_SENSOR_IDENT | AWOS |
| WxSensorType | string | WX_SENSOR_TYPE | AWOS |
| CommissioningStatus | string | COMMISSIONING_STATUS | AWOS |
| CommissioningDate | DateTime? | COMMISSIONING_DATE | AWOS |
| NavaidFlag | string | NAVAID_FLAG | AWOS |
| LatitudeFormatted | string | LAT_FORMATTED | AWOS |
| LongitudeFormatted | string | LONG_FORMATTED | AWOS |
| LatDecimal | decimal? | LAT_DECIMAL | AWOS |
| LongDecimal | decimal? | LONG_DECIMAL | AWOS |
| Elevation | decimal? | ELEV | AWOS |
| SurveyMethod | string | SURVEY_METHOD | AWOS |
| Frequency1 | string | FREQ1 or STATION_FREQ | AWOS |
| Frequency2 | string | FREQ2 or SECOND_STATION_FREQ | AWOS |
| Telephone1 | string | PHONE1 or STATION_PHONE | AWOS |
| Telephone2 | string | PHONE2 or SECOND_STATION_PHONE | AWOS |
| AssociatedAirportSiteNo | string | SITE_NO or ASSOC_LDG_FAC | AWOS |
| StationCity | string | CITY | AWOS |
| StationStateCode | string | STATE_CODE | AWOS |
| EffectiveDate | DateTime | EFF_DATE | AWOS |
| Remarks | string | — | AWOS_RMK (aggregated) |

**Table:** `weather_stations`
**Unique key:** `WxSensorIdent + WxSensorType` (composite)
**Indexes:** WxSensorIdent, WxSensorType, StationStateCode, AssociatedAirportSiteNo, (LatDecimal, LongDecimal)

### Supplementary Files Handling

| CSV File | Strategy | Notes |
|----------|----------|-------|
| AWOS_RMK | Supplementary → merge Remarks field | Free-form text about the station |

### Files to Create/Modify

Same pattern (12 files): Entity, Map(s), Configuration, CronService, Interface, Azure Function, DTO, Mapper, API Service, API Interface, Controller.

### CronService Pattern
```csharp
public class WeatherStationCronService : FaaNasrBaseService<WeatherStation>, IWeatherStationCronService
{
    protected override NasrDataType DataType => NasrDataType.AWOS;
    protected override string[] UniqueIdentifiers => new[] { "WxSensorIdent", "WxSensorType" };
    protected override PublicationType PublicationType => PublicationType.NasrSubscription_WeatherStations;
    protected override IEnumerable<(string FileName, Type ClassMap, bool IsBaseData)> CsvMappings => new[]
    {
        ("AWOS.csv", typeof(WeatherStationMap), true),
        ("AWOS_RMK.csv", typeof(WeatherStationRemarkMap), false),
    };
    protected override bool UsesLegacySiteNoDeduplication => false;
}
```

### Azure Function
```csharp
[Function("WeatherStationFunction")]
[TimerTrigger("0 0 4 * * *")]  // Daily at 4 AM UTC
```

### API Endpoints
```
GET /api/v1/weather-stations                           — List all (paginated, optional ?search=)
GET /api/v1/weather-stations/{identifier}              — By station identifier
GET /api/v1/weather-stations/type/{sensorType}         — Filter by type (ASOS, AWOS-1, AWOS-2, AWOS-3, etc.)
GET /api/v1/weather-stations/state/{stateCode}         — Filter by state
GET /api/v1/weather-stations/airport/{icaoCodeOrIdent} — By associated airport
```

---

## Step 5: DI Registration

### Azure Functions Program.cs
**File:** `PreflightApi.Azure.Functions/Program.cs`
```csharp
builder.Services.AddScoped<INavigationalAidCronService, NavigationalAidCronService>();
builder.Services.AddScoped<IFixCronService, FixCronService>();
builder.Services.AddScoped<IWeatherStationCronService, WeatherStationCronService>();
```

### API Program.cs
**File:** `PreflightApi.API/Program.cs`
```csharp
builder.Services.AddScoped<INavigationalAidService, NavigationalAidService>();
builder.Services.AddScoped<IFixService, FixService>();
builder.Services.AddScoped<IWeatherStationService, WeatherStationService>();
```

---

## Step 6: EF Core Migration

After all entities and configurations are created:
```bash
dotnet ef migrations add AddNavigationalAidFixWeatherStation --project PreflightApi.Infrastructure --startup-project PreflightApi.API
```

This creates tables: `navigational_aids`, `fixes`, `weather_stations` with appropriate indexes and constraints.

---

## Step 7: Tests

Follow existing test patterns. For each dataset, create:

### Unit Tests (NSubstitute mocking)
- CronService tests: Verify `DataType`, `UniqueIdentifiers`, `CsvMappings`, `PublicationType` are set correctly
- Entity tests: Verify `CreateUniqueKey()`, `UpdateFrom()`, `CreateSelectiveEntity()` methods
- API Service tests: Verify query logic, pagination, not-found handling
- Mapper tests: Verify DTO mapping completeness

### Integration Tests (if applicable)
- CSV parsing with sample data to verify ClassMap column mappings

**Test file locations:**
- `PreflightApi.Tests/Services/CronJobServices/NavigationalAidCronServiceTests.cs`
- `PreflightApi.Tests/Services/CronJobServices/FixCronServiceTests.cs`
- `PreflightApi.Tests/Services/CronJobServices/WeatherStationCronServiceTests.cs`
- `PreflightApi.Tests/Services/NavigationalAidServiceTests.cs`
- `PreflightApi.Tests/Services/FixServiceTests.cs`
- `PreflightApi.Tests/Services/WeatherStationServiceTests.cs`

---

## Step 8: Verification

1. **Build:** `dotnet build PreflightApi.sln`
2. **Tests:** `dotnet test PreflightApi.Tests/PreflightApi.Tests.csproj`
3. **Migration check:** Verify migration generates correct SQL with `dotnet ef migrations script --project PreflightApi.Infrastructure --startup-project PreflightApi.API`
4. **Local smoke test:** Run API locally, verify new endpoints return proper Swagger docs and empty results (before data sync)

---

## Implementation Order

1. Step 0 — Download CSV ZIPs, inspect file names and column headers
2. Step 1 — Shared infrastructure (enums, DbInitializer, DbContext)
3. Step 2 — NavigationalAid (entity → config → maps → cron → function → DTO/mapper → service → controller)
4. Step 3 — Fix (same sequence)
5. Step 4 — WeatherStation (same sequence)
6. Step 5 — DI registration (both Program.cs files)
7. Step 6 — EF Core migration
8. Step 7 — Tests
9. Step 8 — Build and verify

---

## Files Summary

**New files (~36):**
- 3 domain entities
- 3 EF Core configurations
- ~8 CsvHelper ClassMaps (3 base + 5 supplementary)
- 3 cron service interfaces
- 3 cron service implementations
- 3 Azure Functions
- 3 DTOs
- 3 DTO mappers
- 3 API service interfaces
- 3 API service implementations
- 3 API controllers
- ~6 test files

**Modified files (~6):**
- NasrDataType.cs (add 3 enum values)
- PublicationType.cs (add 3 enum values)
- DbInitializer.cs (add 3 seed records)
- PreflightApiDbContext.cs (add 3 DbSets)
- Azure Functions Program.cs (add 3 service registrations)
- API Program.cs (add 3 service registrations)

**Key reference files (existing patterns to follow):**
- `PreflightApi.Domain/Entities/CommunicationFrequency.cs` — standalone entity pattern
- `PreflightApi.Domain/Entities/Airport.cs` — multi-file entity with supplementary data
- `PreflightApi.Infrastructure/Services/CronJobServices/NasrServices/CommunicationFrequencyCronService.cs` — simplest cron service
- `PreflightApi.Infrastructure/Services/CronJobServices/NasrServices/Mappings/FrequencyMap.cs` — ClassMap pattern
- `PreflightApi.Infrastructure/Data/Configurations/CommunicationFrequencyConfiguration.cs` — EF Core config
- `PreflightApi.Azure.Functions/Functions/FrequencyFunction.cs` — Azure Function pattern
- `PreflightApi.Infrastructure/Services/AirportInformationServices/CommunicationFrequencyService.cs` — API service
- `PreflightApi.API/Controllers/CommunicationFrequencyController.cs` — controller pattern
