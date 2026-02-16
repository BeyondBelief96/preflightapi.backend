# PreflightApi MCP Server

An MCP (Model Context Protocol) server that exposes PreflightApi's aviation services to AI models, enabling natural language flight planning.

## Quick Start

### Prerequisites

1. **PreflightApi running locally** (the MCP server calls the API over HTTP)
   ```bash
   # From repo root
   docker-compose -f docker-compose.local.yml up
   ```
   This starts PostgreSQL and the API on `https://localhost:7014`.

2. **Claude Desktop** installed ([download](https://claude.ai/download))

### Configure Claude Desktop

Add the MCP server to `~/.claude/claude_desktop_config.json`:

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

Replace `/path/to/PreflightApi.MCP` with the actual path, e.g.:
```
/Users/jefferysummers/source/preflightapi.backend/PreflightApi.MCP
```

### Restart Claude Desktop

Quit and reopen Claude Desktop. You should see "preflight" in the MCP servers list (click the hammer icon).

## Example Usage

**Prompt:** "Build me a flight plan from Dallas to Denver"

Claude will use the MCP tools to:

1. **Search airports** - finds KDFW, KDAL, KADS (Dallas area) and KDEN, KAPA, KBJC (Denver area)
2. **Ask for clarification** - "Which Dallas airport? KDFW (DFW International) or KDAL (Love Field)?"
3. **Get weather** - retrieves METARs for departure/destination
4. **Validate inputs** - identifies missing aircraft performance data
5. **Ask for performance data** - "What is your cruise airspeed? Fuel burn rate?"
6. **Calculate navlog** - returns course, headings, fuel, time for each leg
7. **Assess weather** - GO/CAUTION/NO-GO recommendation

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

## Configuration

Edit `appsettings.json` to change the API URL:

```json
{
  "PreflightApi": {
    "BaseUrl": "https://localhost:7014",
    "GatewaySecret": ""
  }
}
```

## Testing Without Claude Desktop

Build and run directly to verify the server starts:

```bash
cd PreflightApi.MCP
dotnet build
dotnet run
```

The server runs via stdio (no HTTP port). It will wait for MCP protocol messages on stdin.

## Troubleshooting

**"Connection refused" errors**
- Ensure PreflightApi is running: `curl https://localhost:7014/health`
- Check `docker-compose` logs if using Docker

**Tools not appearing in Claude Desktop**
- Verify the path in `claude_desktop_config.json` is correct
- Check Claude Desktop logs: `~/Library/Logs/Claude/`
- Restart Claude Desktop completely (Quit, not just close window)

**"No METAR/TAF found"**
- Small airports may not have weather reporting
- Try a larger airport (KDFW, KDEN, KATL, etc.)
