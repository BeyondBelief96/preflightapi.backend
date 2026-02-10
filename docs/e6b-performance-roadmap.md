# E6B Performance Calculations Roadmap

## Overview

The Performance controller provides E6B flight computer calculations commonly used by VFR pilots. These are manual-input, stateless calculations — no database or external API calls required.

All performance endpoints are gated to the **Commercial Pilot** tier via APIM policies.

---

## Phase 1 (Implemented)

### Crosswind Component
- **Airport-based** (`GET /performance/crosswind/{icao}`) — uses live METAR wind data
- **Manual** (`POST /performance/crosswind/calculate`) — user-provided wind/runway values

### Density Altitude
- **Airport-based** (`GET /performance/density-altitude/{icao}`) — uses live METAR + airport elevation
- **Manual** (`POST /performance/density-altitude/calculate`) — user-provided elevation, altimeter, temperature

### Wind Triangle
- `POST /performance/wind-triangle/calculate`
- Inputs: true course, TAS, wind direction, wind speed
- Returns: true heading, ground speed, WCA, headwind/crosswind components

### True Airspeed (TAS)
- `POST /performance/true-airspeed/calculate`
- Inputs: calibrated airspeed, pressure altitude, OAT
- Returns: TAS, density altitude, Mach number

### Cloud Base Estimation
- `POST /performance/cloud-base/calculate`
- Inputs: surface temperature, dewpoint
- Returns: estimated cloud base AGL (spread x 400)

### Pressure Altitude
- `POST /performance/pressure-altitude/calculate`
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
- `GET /performance/pressure-altitude/{icao}`
- Uses airport elevation + latest METAR altimeter setting

### Airport Cloud Base
- `GET /performance/cloud-base/{icao}`
- Uses latest METAR temperature and dewpoint

### Airport TAS
- `GET /performance/true-airspeed/{icao}?cas=100`
- Uses airport-derived pressure altitude and METAR OAT

---

## Design Principles

1. **Manual-first**: Every calculation has a manual (`POST .../calculate`) variant that takes all inputs directly. Airport-aware variants are convenience wrappers.
2. **Echo inputs**: All responses include the input parameters used, so the caller can verify what was computed.
3. **Stateless**: No database reads for manual calculations — pure math, O(1) latency.
4. **Validation**: Input validation with clear error messages. Throw `ValidationException` for invalid inputs (e.g., dewpoint > temperature, CAS <= 0).
5. **Safety-critical**: Every calculation is unit-tested against known E6B reference values. Tolerances are tight enough to match a physical flight computer.
