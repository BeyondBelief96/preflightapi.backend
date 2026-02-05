# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PreflightApi is a .NET 8 aviation data platform that provides weather information, airport data, airspace information, and flight planning services for VFR (Visual Flight Rules) pilots. The system consists of two main applications:

1. **PreflightApi.API** - ASP.NET Core Web API serving client applications
2. **PreflightApi.Azure.Functions** - Azure Functions app for scheduled data synchronization (cron jobs)

## Architecture

This is a Clean Architecture solution with the following layers:

- **PreflightApi.Domain**: Core domain entities, value objects, enums, and exceptions (no external dependencies)
- **PreflightApi.Infrastructure**: Data access (EF Core), external service integrations, repositories, DTOs, and mappers
- **PreflightApi.API**: REST API controllers and authentication handlers
- **PreflightApi.Azure.Functions**: Timer-triggered functions for data synchronization
- **PreflightApi.Tests**: xUnit test project with FluentAssertions, NSubstitute, and Testcontainers

### Key Architectural Patterns

**Database**: PostgreSQL with PostGIS extension for spatial data (NetTopologySuite for geometry types)

**Entity Framework Core**: Code-first approach with migrations in PreflightApi.Infrastructure/Migrations. All entity configurations are in PreflightApi.Infrastructure/Data/Configurations using IEntityTypeConfiguration.

**Dependency Injection**: Services registered in Program.cs files. The API registers web services (Metar, Taf, Airport, etc.), while Azure Functions registers cron services.

**External Data Sources**:
- NOAA Aviation Weather API for METAR, TAF, PIREP, AIRMET/SIGMET data
- FAA NASR data for airports and communication frequencies
- ArcGIS services for airspace boundaries (via ArcGisBaseService)
- Azure Blob Storage for storing airport diagrams and chart supplements (via ICloudStorageService)

**Cloud Storage**: Uses `ICloudStorageService` abstraction with `AzureBlobStorageService` implementation. Supports:
- SAS token URL generation for secure blob access
- Blob upload, delete, and batch delete operations
- Container management
- Managed Identity authentication (production) or connection string (development)

**Value Objects**: Complex weather data structures (sky conditions, turbulence, icing) are modeled as value objects owned by entities (see PreflightApi.Domain/ValueObjects).

**Authentication**: Auth0 JWT Bearer authentication with conditional bypass in development via ConditionalAuthHandler.

## Common Development Commands

### Build and Test

```bash
# Build the entire solution
dotnet build PreflightApi.sln

# Build specific platform (x64)
dotnet build PreflightApi.sln -c Debug --arch x64

# Run all tests
dotnet test PreflightApi.Tests/PreflightApi.Tests.csproj

# Run specific test
dotnet test PreflightApi.Tests/PreflightApi.Tests.csproj --filter "FullyQualifiedName~NavlogServiceTests"
```

### Database Migrations

```bash
# Add new migration (run from solution root)
dotnet ef migrations add MigrationName --project PreflightApi.Infrastructure --startup-project PreflightApi.API

# Update database
dotnet ef database update --project PreflightApi.Infrastructure --startup-project PreflightApi.API

# Remove last migration
dotnet ef migrations remove --project PreflightApi.Infrastructure --startup-project PreflightApi.API
```

### Running Locally

**With Docker Compose** (recommended for full stack including PostgreSQL):
```bash
docker-compose -f docker-compose.local.yml up
```

This starts:
- PostgreSQL with PostGIS (port 5432)
- API with hot reload (port 7014)

Note: The Docker container connects to Azure Blob Storage directly using the connection string in docker-compose.local.yml.

**API Only** (requires separate database):
```bash
cd PreflightApi.API
dotnet run
```

**Azure Functions Only** (requires separate database and Azure Storage):
```bash
cd PreflightApi.Azure.Functions
func start
```

## Important Implementation Details

### DbContext and Spatial Data

The PreflightApiDbContext requires NetTopologySuite for PostGIS geometry types. When creating the DbContext:
- Use NpgsqlDataSourceBuilder with `.UseNetTopologySuite()`
- Enable retry on failure (3 retries, 30s timeout)
- Entity configurations are auto-discovered via `ApplyConfigurationsFromAssembly`

### Azure Functions Cron Schedule Pattern

Timer triggers use cron expressions (6-field format):
- `"0 */10 * * * *"` = every 10 minutes (weather data)
- Functions inherit database connection and service configuration from Program.cs
- Requires `AzureWebJobsStorage` connection string for timer state tracking

### Cloud Storage Service

The `ICloudStorageService` interface provides cloud-agnostic blob storage operations:

```csharp
// Key methods:
Task<string> GeneratePresignedUrlAsync(string containerName, string blobName, TimeSpan expiration);
Task UploadBlobAsync(string containerName, string blobName, Stream content, string contentType);
Task DeleteBlobAsync(string containerName, string blobName);
Task DeleteBlobsAsync(string containerName, IEnumerable<string> blobNames); // 256 per batch for Azure
Task<List<string>> ListBlobsAsync(string containerName, string? prefix = null);
```

Configuration is via `CloudStorageSettings`:
- `ConnectionString`: Full Azure Storage connection string (for local dev)
- `AccountName`: Storage account name (for Managed Identity)
- `UseManagedIdentity`: Enable for production
- `ChartSupplementsContainerName`: Container for FAA chart supplements
- `AirportDiagramsContainerName`: Container for airport diagrams

### NASR Data Synchronization

The INasrEntity interface marks entities that sync from FAA NASR data. These entities use special cron services (AirportCronService, CommunicationFrequencyCronService) that parse fixed-width text files from the FAA.

### ArcGIS Service Pattern

Airspace services (AirspaceCronService, SpecialUseAirspaceCronService) inherit from ArcGisBaseService and use paginated queries against ArcGIS REST endpoints. The HttpClient for ArcGIS has an extended 10-minute timeout configured in Program.cs.

### Flight Planning

The NavlogService calculates navigation logs for VFR cross-country flights:
- Uses MagneticVariationService (NOAA magnetic model API)
- Uses WindsAloftService (forecast winds aloft data)
- Calculates bearing, distance, headings, and fuel for each leg
- Returns NavlogLeg value objects containing all computed flight planning data

### Testing Patterns

Tests use:
- **NSubstitute** for mocking interfaces
- **FluentAssertions** for readable assertions
- **Testcontainers.PostgreSql** for integration tests requiring real database
- **MockHttp** (RichardSzalay.MockHttp) for HTTP client testing

## Configuration

Both applications use hierarchical configuration:

**API**: `api.appsettings.json` → `api.appsettings.{Environment}.json` → Environment Variables

**Azure Functions**: `local.settings.json` → `appsettings.json` → Environment Variables

Key settings sections:
- `Database`: Connection parameters (Host, Database, Username, Password, Port)
- `CloudStorage`: Azure Blob Storage configuration (ConnectionString, AccountName, UseManagedIdentity, container names)
- `NOAASettings`: API key for weather services
- `Auth0Settings`: JWT authentication configuration
- `StripeSettings`: Payment subscription configuration

### Azure Functions Required Settings

For local development, `local.settings.json` must include:
- `AzureWebJobsStorage`: Azure Storage connection string (for timer trigger state)
- `CloudStorage__ConnectionString`: Azure Blob Storage connection string
- `CloudStorage__ChartSupplementsContainerName`: Container name for chart supplements
- `CloudStorage__AirportDiagramsContainerName`: Container name for airport diagrams

## Git Workflow

Main branch: `main`

The solution supports both AnyCPU and x64 platforms. CI/CD is configured via `.github/workflows/main-ci-cd.yml`.
