# PreflightApi MCP Server

An MCP (Model Context Protocol) server that exposes PreflightApi's aviation services to AI models, enabling natural language flight planning.

## Quick Start

### Prerequisites

**PreflightApi running locally** (the MCP server calls the API over HTTP):
```bash
# From repo root
docker-compose -f docker-compose.local.yml up
```
This starts PostgreSQL and the API on `https://localhost:7014`.

### Build

```bash
cd PreflightApi.MCP
dotnet build
```

## Command-Line Testing

### Option 1: MCP Inspector (Recommended)

The MCP Inspector provides an interactive UI to test tools:

```bash
npx @modelcontextprotocol/inspector dotnet run --project /path/to/PreflightApi.MCP
```

This opens a browser UI where you can:
- See all available tools
- Call tools with parameters
- View JSON responses

### Option 2: Claude Code

Add to your Claude Code MCP settings (`~/.claude/settings.json`):

```json
{
  "mcpServers": {
    "preflight": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/PreflightApi.MCP"]
    }
  }
}
```

Then use Claude Code normally - it will have access to the flight planning tools.

### Option 3: Direct stdio Testing

Send JSON-RPC messages directly via stdin:

```bash
cd PreflightApi.MCP
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list"}' | dotnet run
```

Example tool call:
```bash
echo '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"SearchAirports","arguments":{"query":"Dallas","limit":5}}}' | dotnet run
```

## Available Tools

| Tool | Description |
|------|-------------|
| `SearchAirports` | Search by name, city, or code |
| `GetAirport` | Get airport details by ICAO/FAA code |
| `GetMetar` | Current weather observation |
| `GetTaf` | Terminal forecast |
| `AssessVfrWeather` | VFR GO/CAUTION/NO-GO assessment |
| `ValidateFlightPlanInputs` | Check for missing data |
| `CalculateNavlog` | Full navigation log calculation |
| `GetRouteBriefing` | Combined weather + safety briefing |

## Example: "Build me a flight plan from Dallas to Denver"

The tools would be called in this sequence:

```bash
# 1. Search for Dallas airports
SearchAirports(query: "Dallas", limit: 5)
# Returns: KDFW, KDAL, KADS, etc. with AmbiguousInput uncertainty

# 2. Search for Denver airports
SearchAirports(query: "Denver", limit: 5)
# Returns: KDEN, KAPA, KBJC, etc.

# 3. Validate inputs (will identify missing performance data)
ValidateFlightPlanInputs(departureAirport: "KDFW", destinationAirport: "KDEN", ...)
# Returns: MissingRequiredField uncertainties for cruise speed, fuel burn, etc.

# 4. After user provides data, calculate navlog
CalculateNavlog(departureAirport: "KDFW", destinationAirport: "KDEN",
                cruiseTas: 120, cruiseFuelBurn: 8.5, ...)
# Returns: Full navigation log with legs, headings, fuel, time

# 5. Get weather assessment
AssessVfrWeather(airportCodes: "KDFW,KDEN")
# Returns: GO/CAUTION/NO-GO for each airport
```

## Configuration

Edit `appsettings.json`:

```json
{
  "PreflightApi": {
    "BaseUrl": "https://localhost:7014",
    "GatewaySecret": ""
  }
}
```

## Troubleshooting

**"Connection refused" errors**
- Ensure PreflightApi is running: `curl -k https://localhost:7014/health`

**"No METAR/TAF found"**
- Small airports may not have weather reporting
- Try larger airports (KDFW, KDEN, KATL)
