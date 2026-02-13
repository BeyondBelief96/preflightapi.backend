# E6B Performance Calculations Roadmap

## Disclaimer

These calculations are intended for **pre-flight planning purposes only** and must not be used for in-flight navigation. Results are approximations based on the ICAO Standard Atmosphere (ISA) model, which assumes mid-latitude atmospheric conditions. The actual tropopause altitude varies from ~26,000 ft at the poles to ~55,000 ft at the equator, and real atmospheric conditions differ from the ISA model. Always verify against certified instruments and official flight planning tools.

---

## Overview

The E6B controller provides flight computer calculations commonly used by VFR pilots. These are manual-input, stateless calculations — no database or external API calls required.

All E6B endpoints are gated to the **Commercial Pilot** tier via APIM policies.

---

## Phase 1 (Implemented)

### Crosswind Component
- **Airport-based** (`GET /e6b/crosswind/{icao}`) — uses live METAR wind data
- **Manual** (`POST /e6b/crosswind/calculate`) — user-provided wind/runway values

### Density Altitude
- **Airport-based** (`GET /e6b/density-altitude/{icao}`) — uses live METAR + airport elevation
- **Manual** (`POST /e6b/density-altitude/calculate`) — user-provided elevation, altimeter, temperature

### Wind Triangle
- `POST /e6b/wind-triangle/calculate`
- Inputs: true course, TAS, wind direction, wind speed
- Returns: true heading, ground speed, WCA, headwind/crosswind components

### True Airspeed (TAS)
- `POST /e6b/true-airspeed/calculate`
- Inputs: calibrated airspeed, pressure altitude, OAT
- Returns: TAS, density altitude, Mach number
- Uses the full compressible isentropic flow conversion (CAS → impact pressure → Mach → TAS), accurate from sea level through FL410+ including above the tropopause

### Cloud Base Estimation
- `POST /e6b/cloud-base/calculate`
- Inputs: surface temperature, dewpoint
- Returns: estimated cloud base AGL (spread x 400)

### Pressure Altitude
- `POST /e6b/pressure-altitude/calculate`
- Inputs: field elevation, altimeter setting
- Returns: pressure altitude, altimeter correction

---

## Phase 2 — Fuel & Endurance

### Fuel Burn Rate
- Inputs: fuel flow (gal/hr), ground speed, distance
- Returns: fuel required, time en route, fuel remaining given start fuel

### Endurance
- Inputs: usable fuel (gal), fuel flow (gal/hr)
- Returns: endurance (hours + minutes), maximum range at given ground speed

### Fuel Weight
- Inputs: fuel volume (gal), fuel type (100LL / Jet-A)
- Returns: fuel weight (lbs), using standard densities (6.0 lb/gal for 100LL, 6.7 for Jet-A)

---

## Phase 3 — Weight & Balance Helpers

### Weight & Balance Calculation
- Inputs: array of { weight, arm } items
- Returns: total weight, total moment, CG location
- No aircraft-specific data — the caller provides the station arms

### CG Envelope Check
- Inputs: total weight, CG, plus forward/aft CG limits at weight breakpoints
- Returns: whether CG is within the envelope, margins to forward/aft limits

---

## Phase 4 — Unit Conversions

### Temperature Conversion
- Inputs: value, from unit (C / F / K)
- Returns: converted values in all three units

### Distance Conversion
- Inputs: value, from unit (NM / SM / KM)
- Returns: converted values in all three units

### Speed Conversion
- Inputs: value, from unit (knots / MPH / KPH)
- Returns: converted values in all three units

### Altitude/Length Conversion
- Inputs: value, from unit (feet / meters)
- Returns: converted values in both units

### Pressure Conversion
- Inputs: value, from unit (inHg / hPa / mb)
- Returns: converted values in all three units

---

## Phase 5 — Advanced Calculations

### Holding Pattern Fuel
- Inputs: fuel flow, hold time (or number of turns at standard rate)
- Returns: fuel burned during hold

### Rate of Descent
- Inputs: altitude to lose, distance remaining (or ground speed + time)
- Returns: required descent rate (fpm), descent angle

### Top of Descent (TOD)
- Inputs: cruise altitude, target altitude, descent rate, ground speed
- Returns: distance from destination to begin descent

### VOR/DME Fix Calculations
- Inputs: two VOR radials (or radial + DME distance)
- Returns: fix position (lat/lon)

### Magnetic Variation Lookup
- Inputs: latitude, longitude
- Returns: magnetic variation (already available internally via NOAA API — expose as standalone)

---

## Phase 6 — Airport-Aware Calculations

These combine manual E6B logic with live data from the database.

### Airport Pressure Altitude
- `GET /e6b/pressure-altitude/{icao}`
- Uses airport elevation + latest METAR altimeter setting

### Airport Cloud Base
- `GET /e6b/cloud-base/{icao}`
- Uses latest METAR temperature and dewpoint

### Airport TAS
- `GET /e6b/true-airspeed/{icao}?cas=100`
- Uses airport-derived pressure altitude and METAR OAT

---

## Design Principles

1. **Manual-first**: Every calculation has a manual (`POST .../calculate`) variant that takes all inputs directly. Airport-aware variants are convenience wrappers.
2. **Echo inputs**: All responses include the input parameters used, so the caller can verify what was computed.
3. **Stateless**: No database reads for manual calculations — pure math, O(1) latency.
4. **Validation**: Input validation with clear error messages. Throw `ValidationException` for invalid inputs (e.g., dewpoint > temperature, CAS <= 0).
5. **Safety-critical**: Every calculation is unit-tested against known E6B reference values. Tolerances are tight enough to match a physical flight computer.

---

## Formula Sources & References

### Standard Atmosphere
- **ICAO Doc 7488** — *Manual of the ICAO Standard Atmosphere (extended to 80 kilometres)*. Defines the ISA model: sea-level standard conditions (15°C, 1013.25 hPa / 29.92 inHg), temperature lapse rate (-1.98°C/1000ft in troposphere), tropopause at 36,089 ft, and stratosphere pressure model.
- **International Standard Atmosphere** — [SKYbrary](https://skybrary.aero/articles/international-standard-atmosphere-isa)

### True Airspeed (Compressible Flow)
The TAS calculation uses the full isentropic compressible flow conversion, not the simplified density ratio approximation. This is accurate at all subsonic speeds and altitudes including above the tropopause.

**Algorithm:**
1. **CAS → impact pressure**: `qc/P₀ = (1 + 0.2 × (CAS/a₀)²)^3.5 − 1`
2. **Pressure ratio at altitude (δ):**
   - Troposphere (PA ≤ 36,089 ft): `δ = (1 − 6.8756×10⁻⁶ × PA)^5.2559`
   - Stratosphere (PA > 36,089 ft): `δ = 0.22336 × e^(−4.80634×10⁻⁵ × (PA − 36089))`
3. **Mach number**: `M = √(5 × ((qc/P₀/δ + 1)^(2/7) − 1))`
4. **TAS**: `TAS = M × a₀ × √(OAT_K / 288.15)` where `a₀ = 661.47 kt`

**Sources:**
- [Aircraft Flight Mechanics — Airspeed Definitions](https://aircraftflightmechanics.com/AircraftPerformance/Airspeed.html) — derives TAS from isentropic flow, defines EAS/CAS/TAS relationships
- [AeroToolbox — Airspeed Conversions](https://aerotoolbox.com/airspeed-conversions/) — CAS/EAS/TAS/Mach conversion formulas with compressibility
- [Pilot Institute — 4 Types of Airspeed](https://pilotinstitute.com/airspeed-types/) — practical explanation and 2%/1000ft rule of thumb
- [SKYbrary — True Airspeed](https://skybrary.aero/articles/true-airspeed)

### Wind Triangle
Standard navigation wind triangle using vector decomposition. WCA = arcsin((WS × sin(WD − TC)) / TAS). Ground speed uses the corrected formula accounting for wind FROM direction.

### Cloud Base Estimation
Standard spread × 400 formula (equivalent to spread / 2.5 × 1000). This is the widely-used approximation based on the dry adiabatic lapse rate (~3°C/1000ft) vs dewpoint lapse rate (~0.5°C/1000ft), yielding ~2.5°C convergence per 1000ft.

### Pressure Altitude
`PA = field elevation + (29.92 − altimeter) × 1000`. Standard altimetry relationship from the ICAO standard atmosphere.

### Density Altitude
`DA = PA + 120 × (OAT − ISA_temp)`. Koch Chart approximation using ISA temperature deviation.

### Crosswind Component
Standard trigonometric decomposition: crosswind = wind_speed × sin(wind_angle), headwind = wind_speed × cos(wind_angle).
