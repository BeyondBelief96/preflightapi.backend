# API Versioning Guide

This document explains how API versioning works in PreflightApi and provides a step-by-step guide for introducing a new API version when breaking changes are needed.

## Current Setup (Single Version)

### How It Works Today

API versioning is driven by the **assembly major version** defined in `Directory.Build.props`:

```
Directory.Build.props (1.1.2) → Assembly major version (1) → API routes use v1
```

The key components:

| File | Role |
|------|------|
| `Directory.Build.props` | Sets `<Version>`, `<AssemblyVersion>`, and `<FileVersion>` |
| `Program.cs` (lines 93-112) | Reads assembly major version, configures `AddApiVersioning` |
| `AssemblyMajorVersionConvention.cs` | Auto-applies the major version to all controllers |
| `ApiVersionHeaderMiddleware.cs` | Adds `X-API-Version: 1.1.2` header to every response |
| Controller `[Route]` attributes | All use `api/v{version:apiVersion}/[endpoint]` |

**Request flow:** A request to `/api/v1/metars/KJFK` matches because:

1. `UrlSegmentApiVersionReader` extracts `1` from the URL
2. `AssemblyMajorVersionConvention` has registered all controllers as version `1.0`
3. `SubstituteApiVersionInUrl = true` resolves the `{version:apiVersion}` placeholder in Swagger

**Response headers on every request:**

```
X-API-Version: 1.1.2              (from ApiVersionHeaderMiddleware)
api-supported-versions: 1.0       (from Asp.Versioning)
```

### What Counts as a Breaking Change

You do **not** need a new API version for:
- Adding new endpoints
- Adding new optional query parameters
- Adding new fields to response DTOs
- Bug fixes to existing behavior
- Performance improvements

You **do** need a new API version for:
- Removing or renaming an endpoint
- Removing or renaming fields in response DTOs
- Changing the type of an existing response field
- Changing the meaning/behavior of an existing parameter
- Changing the structure of request/response bodies in incompatible ways
- Changing error response formats

## Introducing API v2 — Step by Step

When the time comes to introduce breaking changes, follow these steps.

### Step 1: Decouple API Version from Assembly Version

The assembly version (`Directory.Build.props`) represents your **build artifact version** and should continue incrementing with every release regardless of API version. The API version represents the **contract version** your consumers depend on and only changes when there are breaking changes.

**Remove `AssemblyMajorVersionConvention`** — it ties all controllers to the assembly major version, which prevents running v1 and v2 side by side.

In `Program.cs`, replace the versioning config (lines 93-112):

```csharp
// Before (coupled to assembly version)
var apiMajorVersion = Assembly.GetExecutingAssembly().GetName().Version?.Major ?? 1;

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(apiMajorVersion, 0);
    options.AssumeDefaultVersionWhenUnspecified = false;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddMvc(options =>
{
    options.Conventions.Add(new AssemblyMajorVersionConvention());
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
```

```csharp
// After (explicit version control)
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(2, 0);
    options.AssumeDefaultVersionWhenUnspecified = false;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddMvc()  // No convention — controllers declare their own versions
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
```

You can then delete `PreflightApi.API/Configuration/AssemblyMajorVersionConvention.cs`.

### Step 2: Add `[ApiVersion]` Attributes to All Controllers

Every controller needs explicit version declarations. There are three categories:

#### Controllers with NO breaking changes (most of them)

These support both v1 and v2 with the same implementation. Add both version attributes:

```csharp
[ApiVersion(1.0)]
[ApiVersion(2.0)]
[ApiController]
[Route("api/v{version:apiVersion}/metars")]
[Tags("Weather - METARs")]
public class MetarController(IMetarService metarService) : ControllerBase
{
    // All existing actions work for both v1 and v2
}
```

Apply this to all 15 controllers initially. Only split a controller when it actually has breaking changes.

**Full controller list:**
- `AirportController`
- `AirspaceController`
- `BriefingController`
- `ChartSupplementController`
- `CommunicationFrequencyController`
- `E6bController`
- `GAirmetController`
- `MetarController`
- `NavlogController`
- `NotamController`
- `ObstacleController`
- `PirepController`
- `SigmetController`
- `TafController`
- `TerminalProcedureController`

#### Controllers with breaking changes — split into v1 and v2

Create a separate controller for the old version. For example, if the airport response DTO changes:

```csharp
// V1 controller — preserves old behavior
[ApiVersion(1.0, Deprecated = true)]
[ApiController]
[Route("api/v{version:apiVersion}/airports")]
[Tags("Airports")]
public class AirportV1Controller(IAirportService airportService) : ControllerBase
{
    [HttpGet("{icaoCode}")]
    public async Task<ActionResult<AirportV1Dto>> GetByIcaoCode(string icaoCode)
    {
        // Return old DTO shape
    }
}

// V2 controller — new behavior
[ApiVersion(2.0)]
[ApiController]
[Route("api/v{version:apiVersion}/airports")]
[Tags("Airports")]
public class AirportController(IAirportService airportService) : ControllerBase
{
    [HttpGet("{icaoCode}")]
    public async Task<ActionResult<AirportDto>> GetByIcaoCode(string icaoCode)
    {
        // Return new DTO shape
    }
}
```

Both controllers share the same route template — `Asp.Versioning` routes to the correct one based on the version in the URL.

#### Individual action versioning with `[MapToApiVersion]`

If only one action in a controller has breaking changes, you can keep a single controller and use `[MapToApiVersion]` on specific actions:

```csharp
[ApiVersion(1.0)]
[ApiVersion(2.0)]
[ApiController]
[Route("api/v{version:apiVersion}/airports")]
public class AirportController(IAirportService airportService) : ControllerBase
{
    // Shared across v1 and v2
    [HttpGet("search")]
    public async Task<ActionResult<List<AirportDto>>> Search([FromQuery] string query) { ... }

    // v1 only
    [HttpGet("{icaoCode}")]
    [MapToApiVersion(1.0)]
    public async Task<ActionResult<AirportV1Dto>> GetByIcaoCodeV1(string icaoCode) { ... }

    // v2 only
    [HttpGet("{icaoCode}")]
    [MapToApiVersion(2.0)]
    public async Task<ActionResult<AirportDto>> GetByIcaoCodeV2(string icaoCode) { ... }
}
```

### Step 3: Configure Swagger for Multiple Versions

Replace the single `AddOpenApiDocument` call (Program.cs lines 114-127) with one document per version:

```csharp
var assemblyVersion = Assembly.GetExecutingAssembly()
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
    ?.InformationalVersion?.Split('+')[0] ?? "unknown";

// v1 Swagger doc
builder.Services.AddOpenApiDocument(options =>
{
    options.Title = "PreflightApi";
    options.Version = $"v1 ({assemblyVersion})";
    options.Description = "Aviation data API for VFR flight planning (v1 — deprecated).";
    options.ApiGroupNames = new[] { "v1" };
    options.DocumentProcessors.Add(new ControllerXmlDocProcessor());
    options.OperationProcessors.Add(new OperationXmlDocProcessor());
});

// v2 Swagger doc
builder.Services.AddOpenApiDocument(options =>
{
    options.Title = "PreflightApi";
    options.Version = $"v2 ({assemblyVersion})";
    options.Description = "Aviation data API for VFR flight planning.";
    options.ApiGroupNames = new[] { "v2" };
    options.DocumentProcessors.Add(new ControllerXmlDocProcessor());
    options.OperationProcessors.Add(new OperationXmlDocProcessor());
});
```

The Swagger UI version dropdown will now show both v1 and v2, each listing only the endpoints that belong to that version.

### Step 4: Update APIM Policies (if applicable)

If Azure API Management fronts the API, update the APIM policies to:
- Accept both `/api/v1/*` and `/api/v2/*` routes
- Optionally redirect v1 consumers to v2 with deprecation warnings
- Update any backend URL rewriting rules

### Step 5: Communicate Deprecation to Consumers

Once v2 is live, v1 responses will automatically include a deprecation header (from `Asp.Versioning`):

```
api-supported-versions: 1.0, 2.0
api-deprecated-versions: 1.0
```

Set a sunset timeline and communicate it to API consumers. A typical timeline:
1. **Launch v2** — both versions active, v1 marked deprecated
2. **Migration period** (3-6 months) — consumers migrate to v2
3. **Sunset v1** — remove v1 controllers and the `[ApiVersion(1.0)]` attributes

### Step 6: Eventually Remove v1

When v1 is no longer needed:
1. Remove all `[ApiVersion(1.0, Deprecated = true)]` attributes
2. Delete any v1-specific controller classes (e.g., `AirportV1Controller`)
3. Delete v1-specific DTOs
4. Remove the v1 `AddOpenApiDocument` call
5. Remove `[ApiVersion(1.0)]` from dual-version controllers (leaving only `[ApiVersion(2.0)]`)

## Project Organization for Versioned Controllers

When you have version-specific controllers, organize them in folders:

```
PreflightApi.API/
  Controllers/
    MetarController.cs              ← Supports both v1 and v2
    TafController.cs                ← Supports both v1 and v2
    V1/
      AirportV1Controller.cs        ← v1 only (deprecated)
    AirportController.cs            ← v2 only (or dual-version with MapToApiVersion)
```

Keep the `V1/` folder only for controllers that needed to be split. Don't move unchanged controllers.

## Summary of Changes Checklist

When introducing v2, the changes touch these files:

- [ ] `Program.cs` — Remove `AssemblyMajorVersionConvention`, set `DefaultApiVersion` to `2.0`, add second `AddOpenApiDocument`
- [ ] `AssemblyMajorVersionConvention.cs` — Delete this file
- [ ] All 15 controllers — Add `[ApiVersion(1.0)]` and `[ApiVersion(2.0)]` attributes
- [ ] Controllers with breaking changes — Split into v1/v2 classes or use `[MapToApiVersion]`
- [ ] New v1-specific DTOs (if response shapes changed) — Create in `Infrastructure/Dtos/`
- [ ] APIM policies — Update to route both versions
- [ ] Tests — Add tests for v2 endpoints; verify v1 endpoints still work

`Directory.Build.props` does **not** need to change for an API version bump. Continue incrementing it with your normal release cadence (`1.2.0`, `1.3.0`, etc.) independently of the API version.
