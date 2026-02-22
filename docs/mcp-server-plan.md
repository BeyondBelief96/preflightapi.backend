# MCP Server Plan for PreflightApi

**PreflightApi Aviation Data Platform**

**February 2026**

---

## Overview

Expose the PreflightApi as a remote [Model Context Protocol](https://modelcontextprotocol.io) (MCP) server so that AI agents (Claude, Copilot, custom agents) can use aviation data tools — weather briefings, airport lookups, E6B calculations, flight planning — grounded in real FAA/NOAA data instead of hallucinating.

### Why MCP?

- **Grounding** — Aviation is safety-critical. Tools that call real APIs eliminate hallucinated weather, NOTAMs, and airport data.
- **Multi-step reasoning** — A good pre-flight briefing chains 5-6 data sources. MCP lets agents orchestrate that naturally.
- **Structured data** — The API already returns typed DTOs that map cleanly to MCP tool schemas.

### Target Consumers

- AI-powered EFB (Electronic Flight Bag) apps with conversational interfaces
- Flight school platforms building AI tutors
- Developers using Claude Code / Copilot to query production aviation data
- Part 135 dispatch tools for quick route assessments

---

## Table of Contents

- [Architecture Decision](#architecture-decision)
- [Technology Stack](#technology-stack)
- [Tool Design](#tool-design)
- [Authentication](#authentication)
- [Infrastructure & Deployment](#infrastructure--deployment)
- [Implementation Plan](#implementation-plan)
- [Future Considerations](#future-considerations)

---

## Architecture Decision

### Option A: Separate ASP.NET Core Project (Recommended)

Add a new `PreflightApi.Mcp` project to the solution that references `PreflightApi.Infrastructure` and reuses the existing service layer.

```
PreflightApi.sln
├── PreflightApi.Domain
├── PreflightApi.Infrastructure
├── PreflightApi.API              ← existing REST API
├── PreflightApi.Azure.Functions  ← existing cron jobs
├── PreflightApi.Mcp              ← NEW: MCP server
└── PreflightApi.Tests
```

**Advantages:**
- Independent deployment and scaling (MCP traffic patterns differ from REST)
- Can run on a separate App Service or even as an Azure Container App
- No risk of breaking existing REST API consumers
- Clean separation of MCP tool definitions from REST controllers

### Option B: Integrate into Existing API

Add `MapMcp()` alongside the existing REST endpoints in `PreflightApi.API`.

**Advantages:** Simpler, fewer moving parts, shared infrastructure.
**Disadvantages:** Couples MCP and REST lifecycles; MCP SDK is still in preview and could introduce instability.

**Decision: Option A** — The MCP C# SDK is `0.8.0-preview` and may have breaking changes. Isolating it protects the production REST API.

---

## Technology Stack

| Component | Choice | Notes |
|---|---|---|
| MCP SDK | [`ModelContextProtocol.AspNetCore`](https://www.nuget.org/packages/ModelContextProtocol.AspNetCore) 0.8.x | Official Anthropic + Microsoft C# SDK |
| Transport | Streamable HTTP | Single endpoint, replaces deprecated SSE transport |
| Runtime | .NET 8 | Matches existing solution |
| Hosting | Azure App Service (or Container App) | Can share the existing App Service Plan initially |
| Auth | API key via APIM (Phase 1), OAuth 2.1 (Phase 2) | Matches existing auth pattern |

### NuGet Packages

```xml
<PackageReference Include="ModelContextProtocol.AspNetCore" Version="0.8.0-preview.1" />
```

---

## Tool Design

### Design Principles

MCP tools should be **higher-level than REST endpoints**. A 1:1 mapping of REST → MCP tools wastes tokens and forces agents to orchestrate multi-call workflows manually. Instead, design tools around **pilot intent**.

### Tool Catalog

#### Tier 1: High-Level Composite Tools (Agent-Optimized)

These combine multiple API calls into single, intent-driven tools that minimize agent round-trips.

| Tool | Description | Wraps |
|---|---|---|
| `get_route_briefing` | Complete weather briefing for a route: METARs, TAFs, PIREPs, SIGMETs, G-AIRMETs, NOTAMs along the corridor | `POST /briefing/route` |
| `plan_flight` | Full navlog calculation with per-leg headings, groundspeed, fuel, winds, and airspace alerts | `POST /navlog/calculate` |
| `get_airport_info` | Comprehensive airport data: details, runways, frequencies, current METAR, density altitude, NOTAMs, diagram URLs | Multiple airport + weather + E6B endpoints |

#### Tier 2: Focused Query Tools

Single-purpose lookups that agents use for specific questions.

| Tool | Description | Wraps |
|---|---|---|
| `get_metar` | Current weather observation for one or more airports | `GET /metars/{id}`, `GET /metars/batch` |
| `get_taf` | Terminal forecast for one or more airports | `GET /tafs/{id}`, `GET /tafs/batch` |
| `get_pireps_nearby` | Pilot reports near a location or airport | `GET /pireps/nearby`, `GET /pireps/airport/{id}` |
| `get_sigmets` | Active SIGMETs, optionally filtered by hazard type or location | `GET /sigmets/*` |
| `get_gairmets` | Active G-AIRMETs, optionally filtered by product/hazard/location | `GET /g-airmets/*` |
| `get_notams` | NOTAMs for an airport, location radius, or route | `GET /notams/{id}`, `GET /notams/radius`, `POST /notams/route` |
| `get_obstacles` | Obstacles near an airport or location | `GET /obstacles/airport/{id}`, `GET /obstacles/search` |
| `search_airports` | Search airports by name, identifier, state, or proximity | `GET /airports/*` |
| `get_airspace` | Airspace boundaries by class, identifier, or location | `GET /airspaces/*` |
| `get_winds_aloft` | Forecast winds at altitude for flight planning | `GET /navlog/winds-aloft/{forecast}` |

#### Tier 3: E6B Calculator Tools

Calculation tools that can use live data or manual inputs.

| Tool | Description | Wraps |
|---|---|---|
| `calculate_crosswind` | Crosswind/headwind components for an airport (live METAR) or manual inputs | `GET /e6b/crosswind/{id}`, `POST /e6b/crosswind/calculate` |
| `calculate_density_altitude` | Density altitude for an airport (live METAR) or manual inputs | `GET /e6b/density-altitude/{id}`, `POST /e6b/density-altitude/calculate` |
| `calculate_wind_triangle` | Wind correction angle, heading, and groundspeed | `POST /e6b/wind-triangle/calculate` |
| `calculate_true_airspeed` | TAS from CAS, altitude, and temperature | `POST /e6b/true-airspeed/calculate` |
| `calculate_cloud_base` | Estimated cloud base from temp/dewpoint spread | `POST /e6b/cloud-base/calculate` |
| `calculate_pressure_altitude` | Pressure altitude from field elevation and altimeter | `POST /e6b/pressure-altitude/calculate` |

### Example Tool Definition (C#)

```csharp
[McpServerToolType]
public class WeatherTools
{
    private readonly IMetarService _metarService;
    private readonly ITafService _tafService;

    public WeatherTools(IMetarService metarService, ITafService tafService)
    {
        _metarService = metarService;
        _tafService = tafService;
    }

    [McpServerTool, Description(
        "Get the current METAR weather observation for one or more airports. " +
        "Returns decoded wind, visibility, sky conditions, temperature, dewpoint, " +
        "altimeter setting, and flight category (VFR/MVFR/IFR/LIFR).")]
    public async Task<string> GetMetar(
        [Description("ICAO code (e.g. KJFK) or FAA identifier (e.g. JFK). " +
                     "Comma-separated for multiple (max 100).")]
        string airports,
        CancellationToken ct)
    {
        var ids = airports.Split(',', StringSplitOptions.TrimEntries);
        if (ids.Length == 1)
        {
            var metar = await _metarService.GetMetarByIcaoCodeAsync(ids[0], ct);
            return JsonSerializer.Serialize(metar);
        }
        var metars = await _metarService.GetMetarsByIcaoCodesAsync(ids, ct);
        return JsonSerializer.Serialize(metars);
    }
}
```

### Example Composite Tool (C#)

```csharp
[McpServerToolType]
public class AirportTools
{
    private readonly IAirportService _airportService;
    private readonly IRunwayService _runwayService;
    private readonly ICommunicationFrequencyService _frequencyService;
    private readonly IMetarService _metarService;
    private readonly IE6bCalculatorService _e6bService;
    private readonly INotamService _notamService;

    // Constructor with DI...

    [McpServerTool, Description(
        "Get comprehensive information about an airport including details, " +
        "runways, communication frequencies, current weather (METAR), " +
        "density altitude, and active NOTAMs. Use this instead of making " +
        "multiple separate calls when you need a full airport picture.")]
    public async Task<string> GetAirportInfo(
        [Description("ICAO code (e.g. KJFK) or FAA identifier (e.g. JFK)")]
        string airport,
        [Description("Include NOTAMs in the response (default: true)")]
        bool includeNotams = true,
        CancellationToken ct = default)
    {
        // Parallel fetch of airport, runways, frequencies, METAR, NOTAMs
        // Combine into a single comprehensive response
        // ...
    }
}
```

### Response Formatting

Tool responses should be **concise and LLM-optimized**:
- Strip null/empty fields from JSON
- Use abbreviations pilots understand (e.g., `VFR`, `IFR`, `kt`, `nm`)
- Include units in field names where ambiguous (e.g., `visibility_sm`, `altitude_ft_msl`)
- For composite tools, use clear section headers in the JSON structure

---

## Authentication

### Phase 1: API Key via APIM (Launch)

Route the MCP endpoint through the existing Azure API Management instance. APIM handles auth via subscription keys, same as the REST API.

```
Agent → APIM (api key validation) → MCP Server → Service Layer → PostgreSQL
```

- MCP clients pass the API key via a custom header or query param
- APIM validates and forwards to the MCP backend
- No changes to existing auth infrastructure

### Phase 2: OAuth 2.1 (Future)

The MCP specification defines an OAuth 2.1 authorization flow with Protected Resource Metadata discovery. When needed:

1. Serve `/.well-known/oauth-protected-resource` metadata from the MCP server
2. Integrate with Clerk (existing auth provider) as the authorization server
3. Use `ModelContextProtocol.AspNetCore`'s built-in `.AddMcp()` auth scheme
4. Support per-tool scopes (e.g., `mcp:weather:read`, `mcp:navlog:write`)

---

## Infrastructure & Deployment

### Initial Setup (Shared Infrastructure)

Deploy the MCP server to the **existing App Service Plan** as a separate App Service. This avoids new infrastructure costs while validating the concept.

```
App Service Plan (B1)
├── preflight-api        ← existing REST API
└── preflight-mcp        ← new MCP server
```

APIM Configuration:
- Add a new API: `preflight-mcp`
- Backend: `https://preflight-mcp.azurewebsites.net`
- Route: `POST /mcp` (single Streamable HTTP endpoint)
- Policy: Same subscription key validation as existing API

### Scaling Path

| Stage | Hosting | Trigger |
|---|---|---|
| Prototype | Same App Service Plan | N/A |
| Early adoption | Separate B1 App Service | MCP traffic impacts REST API performance |
| Growth | Azure Container App | Need per-request scaling, cost efficiency |

### CI/CD

Add to the existing `develop-ci-cd.yml` workflow:

```yaml
- name: Build MCP Server
  run: dotnet publish PreflightApi.Mcp -c Release -o ./mcp-publish

- name: Deploy MCP Server
  uses: azure/webapps-deploy@v3
  with:
    app-name: preflight-mcp
    package: ./mcp-publish
```

---

## Implementation Plan

### Phase 1: Foundation (MVP)

1. **Create `PreflightApi.Mcp` project**
   - `dotnet new web -n PreflightApi.Mcp`
   - Add to solution, reference `PreflightApi.Infrastructure`
   - Install `ModelContextProtocol.AspNetCore`

2. **Configure `Program.cs`**
   - Register shared services (DbContext, service layer) from Infrastructure
   - Configure MCP server with HTTP transport
   - Map the `/mcp` endpoint

3. **Implement Tier 2 weather tools**
   - `GetMetar`, `GetTaf`, `GetPirepsNearby`, `GetSigmets`, `GetGAirmets`
   - One tool class per domain area

4. **Implement Tier 2 airport/airspace tools**
   - `SearchAirports`, `GetNotams`, `GetObstacles`, `GetAirspace`, `GetWindsAloft`

5. **Implement Tier 3 E6B tools**
   - All six calculator tools

6. **Local testing**
   - Test with Claude Desktop or Claude Code MCP client config
   - Verify tool discovery, invocation, and response quality

### Phase 2: Composite Tools

7. **Implement Tier 1 composite tools**
   - `GetRouteBriefing` — wraps briefing service
   - `PlanFlight` — wraps navlog service
   - `GetAirportInfo` — combines airport + weather + frequencies + NOTAMs

8. **Optimize response payloads**
   - Strip null fields, add units, test with real agent conversations
   - Tune tool descriptions based on agent behavior (do agents pick the right tool?)

### Phase 3: Deploy & Secure

9. **Deploy to Azure**
   - Create App Service, configure APIM routing
   - Add to CI/CD pipeline

10. **Add authentication**
    - APIM subscription key validation
    - Rate limiting policy

### Phase 4: Polish

11. **Add MCP Resources** (optional)
    - Expose static reference data (airport list, airspace classes, hazard types) as MCP Resources rather than tools
    - Agents can read these for context without tool calls

12. **Monitoring & observability**
    - Log tool invocations, latency, error rates
    - Application Insights integration

---

## Future Considerations

- **OAuth 2.1 with Clerk** — When MCP clients need user-scoped access (e.g., personal flight plans)
- **MCP Prompts** — Pre-built prompt templates like "VFR pre-flight briefing" or "student cross-country planning" that agents can offer to users
- **Streaming responses** — For large result sets (e.g., nationwide NOTAM searches), use SSE streaming within the Streamable HTTP transport
- **Azure APIM native MCP export** — Microsoft is adding the ability to export any APIM-managed REST API as an MCP server directly; evaluate when GA
- **SDK stability** — The C# SDK is `0.8.0-preview`; pin the version and plan for breaking changes until 1.0
