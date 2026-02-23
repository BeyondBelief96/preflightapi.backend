# PreflightApi

A .NET 8 aviation data platform providing weather, airport, airspace, NOTAM, and flight planning data for VFR (Visual Flight Rules) pilots.

## Table of Contents

- [Overview](#overview)
- [Technology Stack](#technology-stack)
- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
  - [1. Clone the Repository](#1-clone-the-repository)
  - [2. Environment Setup](#2-environment-setup)
  - [3. Docker Setup (Recommended)](#3-docker-setup-recommended)
  - [4. Running Without Docker](#4-running-without-docker)
- [Configuration](#configuration)
- [Database Migrations](#database-migrations)
- [Testing](#testing)
- [API Documentation](#api-documentation)
- [Project Structure](#project-structure)
- [Infrastructure](#infrastructure)
- [Contributing](#contributing)

## Overview

PreflightApi is the server-side component of the PreflightApi aviation platform. It consists of two main applications:

1. **PreflightApi.API** - ASP.NET Core Web API serving client applications (behind Azure API Management)
2. **PreflightApi.Azure.Functions** - Azure Functions app for scheduled data synchronization (timer-triggered cron jobs)

All aviation data is synced to a local PostgreSQL database by the Azure Functions cron jobs. The API serves data exclusively from the database — no external API calls are made at request time.

## Technology Stack

| Category | Technology |
|----------|------------|
| Framework | .NET 8, ASP.NET Core 8 |
| Database | PostgreSQL 15 with PostGIS |
| ORM | Entity Framework Core 9 |
| Spatial Data | NetTopologySuite |
| API Gateway | Azure API Management (authentication, rate limiting, subscriptions) |
| API Versioning | Asp.Versioning.Mvc (URL segment: `/api/v1/...`) |
| Cloud Storage | Azure Blob Storage |
| Serverless | Azure Functions (isolated worker) |
| API Docs | NSwag (OpenAPI/Swagger) |
| Monitoring | Application Insights |
| IaC | Azure Bicep |
| Testing | xUnit, FluentAssertions, NSubstitute, Testcontainers, Bogus, MockHttp |
| Containerization | Docker, Docker Compose |

## Architecture

This solution follows **Clean Architecture** principles:

```
┌─────────────────────────────────────────────────────────────────┐
│                     Azure API Management                        │
│           (Authentication, Rate Limiting, Subscriptions)        │
├─────────────────────────────────────────────────────────────────┤
│                        Presentation                             │
│  ┌─────────────────────┐    ┌─────────────────────────────────┐ │
│  │  PreflightApi.API   │    │ PreflightApi.Azure.Functions    │ │
│  │   (Controllers)     │    │  (Timer Triggers)               │ │
│  └─────────────────────┘    └─────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────────┤
│                      Infrastructure                             │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │              PreflightApi.Infrastructure                    ││
│  │  • Services  • Cron Jobs  • EF Core  • External APIs       ││
│  │  • Cloud Storage  • DTOs/Mappers  • Settings               ││
│  └─────────────────────────────────────────────────────────────┘│
├─────────────────────────────────────────────────────────────────┤
│                         Domain                                  │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                PreflightApi.Domain                          ││
│  │  • Entities  • Value Objects  • Enums  • Exceptions         ││
│  │  • Utilities (unit conversions)                             ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

### Data Flow

1. **Azure Functions** run on cron schedules, fetching data from external sources (NOAA, FAA, ArcGIS) and upserting it into PostgreSQL
2. **API** serves all data from the local PostgreSQL database (no external API calls at request time)
3. **APIM** sits in front of the API, handling authentication, rate limiting, and subscription tier enforcement

## Prerequisites

Before you begin, ensure you have the following installed:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Git](https://git-scm.com/downloads)
- (Optional) [Azure Functions Core Tools v4](https://docs.microsoft.com/azure/azure-functions/functions-run-local) - for running Azure Functions locally
- (Optional) [Azure Storage Emulator/Azurite](https://docs.microsoft.com/azure/storage/common/storage-use-azurite) - for local Azure Storage emulation

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/BeyondBelief96/PreflightApi
cd PreflightApi
```

### 2. Environment Setup

#### Create Environment File

Copy the example environment file and configure your values:

```bash
cp .env.example .env
```

Edit `.env` with your actual values:

```env
# Database password for the PostgreSQL container
DB_PASSWORD=your_secure_password_here

# Azure Blob Storage connection string
# Get this from Azure Portal > Storage Account > Access Keys
CLOUD_STORAGE_CONNECTION_STRING=DefaultEndpointsProtocol=https;AccountName=YOUR_ACCOUNT;AccountKey=YOUR_KEY;EndpointSuffix=core.windows.net

# FAA NMS API credentials (for NOTAM data sync)
# Apply at: https://api.faa.gov/
NMS_CLIENT_ID=your-client-id
NMS_CLIENT_SECRET=your-client-secret
```

#### Required Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `DB_PASSWORD` | PostgreSQL database password | Yes |
| `CLOUD_STORAGE_CONNECTION_STRING` | Azure Blob Storage connection string | Yes |
| `NMS_CLIENT_ID` | FAA NMS API client ID | No* |
| `NMS_CLIENT_SECRET` | FAA NMS API client secret | No* |

*These are only required if you need NOTAM synchronization via Azure Functions.

### 3. Docker Setup (Recommended)

The recommended way to run the backend locally is using Docker Compose, which sets up both PostgreSQL with PostGIS and the API with hot-reload.

#### Start the Services

```bash
docker-compose -f docker-compose.local.yml up
```

This starts:
- **PostgreSQL with PostGIS** on port `5432`
- **PreflightApi API** on port `7014` with hot-reload enabled

The API will automatically:
1. Restore NuGet packages
2. Run database migrations
3. Start with `dotnet watch` for hot-reload

#### Verify the Setup

Once running, verify the API is working:

```bash
# Health check
curl http://localhost:7014/health

# Swagger UI
# Open in browser: http://localhost:7014/swagger
```

#### Stop the Services

```bash
docker-compose -f docker-compose.local.yml down
```

To also remove the database volume (fresh start):

```bash
docker-compose -f docker-compose.local.yml down -v
```

#### Rebuild After Changes

If you modify the Dockerfile or need a fresh build:

```bash
docker-compose -f docker-compose.local.yml up --build
```

### 4. Running Without Docker

If you prefer to run without Docker, you'll need a local PostgreSQL instance with PostGIS.

#### Install PostgreSQL with PostGIS

**Windows (using installer):**
1. Download PostgreSQL from https://www.postgresql.org/download/windows/
2. During installation, use Stack Builder to add PostGIS

**macOS (using Homebrew):**
```bash
brew install postgresql@15 postgis
brew services start postgresql@15
```

**Linux (Ubuntu/Debian):**
```bash
sudo apt install postgresql-15 postgresql-15-postgis-3
```

#### Create the Database

```bash
# Connect to PostgreSQL
psql -U postgres

# Create user and database
CREATE USER preflightapi_development_user WITH PASSWORD 'your_password';
CREATE DATABASE preflightapi_development_database OWNER preflightapi_development_user;

# Connect to the database and enable PostGIS
\c preflightapi_development_database
CREATE EXTENSION postgis;

\q
```

#### Configure User Secrets (Alternative to appsettings)

For sensitive configuration, use .NET User Secrets:

```bash
cd PreflightApi.API

# Initialize user secrets
dotnet user-secrets init

# Set database password
dotnet user-secrets set "Database:Password" "your_password"

# Set cloud storage connection string
dotnet user-secrets set "CloudStorage:ConnectionString" "your_connection_string"

# Set NMS API credentials (only needed for Azure Functions NOTAM sync)
dotnet user-secrets set "NmsSettings:ClientId" "your_client_id"
dotnet user-secrets set "NmsSettings:ClientSecret" "your_client_secret"
```

#### Run Database Migrations

```bash
dotnet ef database update --project PreflightApi.Infrastructure --startup-project PreflightApi.API
```

#### Start the API

```bash
cd PreflightApi.API
dotnet run
```

The API will be available at `https://localhost:7014` or `http://localhost:5014`.

## Configuration

### appsettings Files

Configuration is loaded hierarchically:

1. `appsettings.json` - Base configuration
2. `appsettings.{Environment}.json` - Environment-specific overrides
3. Environment variables - Highest priority
4. User Secrets - Development only (not committed to source control)

### Configuration Sections

#### Database

```json
{
  "Database": {
    "Host": "localhost",
    "Database": "preflightapi_development_database",
    "Username": "preflightapi_development_user",
    "Password": "your_password",
    "Port": 5432
  }
}
```

#### Cloud Storage

```json
{
  "CloudStorage": {
    "ConnectionString": "your_connection_string",
    "UseManagedIdentity": false,
    "ChartSupplementsContainerName": "preflightapi-chart-supplements-centralus-test",
    "TerminalProceduresContainerName": "preflightapi-terminal-procedures-centralus-test"
  }
}
```

For production, set `UseManagedIdentity` to `true` and provide `AccountName` instead of `ConnectionString`.

#### NMS API (NOTAMs — used by Azure Functions only)

```json
{
  "NmsSettings": {
    "BaseUrl": "https://api-staging.cgifederal-aim.com/nmsapi",
    "AuthBaseUrl": "https://api-staging.cgifederal-aim.com",
    "ClientId": "your_client_id",
    "ClientSecret": "your_client_secret",
    "CacheDurationMinutes": 5,
    "DefaultRouteCorridorRadiusNm": 25,
    "RequestTimeoutSeconds": 120,
    "DeltaSyncIntervalMinutes": 3
  }
}
```

### Azure Functions Configuration

For Azure Functions, configuration is in `local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AZURE_FUNCTIONS_ENVIRONMENT": "Development",
    "DOTNET_ENVIRONMENT": "Development"
  }
}
```

To disable specific functions during development, add `"AzureWebJobs.<FunctionName>.Disabled": "true"` entries to the `Values` section:

```json
{
  "Values": {
    "AzureWebJobs.MetarFunction.Disabled": "true",
    "AzureWebJobs.TafFunction.Disabled": "true"
  }
}
```

## Database Migrations

### Add a New Migration

```bash
dotnet ef migrations add MigrationName --project PreflightApi.Infrastructure --startup-project PreflightApi.API
```

### Update Database

```bash
dotnet ef database update --project PreflightApi.Infrastructure --startup-project PreflightApi.API
```

### Remove Last Migration

```bash
dotnet ef migrations remove --project PreflightApi.Infrastructure --startup-project PreflightApi.API
```

### View Migration SQL

```bash
dotnet ef migrations script --project PreflightApi.Infrastructure --startup-project PreflightApi.API
```

## Testing

### Run All Tests

```bash
dotnet test PreflightApi.Tests/PreflightApi.Tests.csproj
```

### Run Unit Tests Only (excludes integration tests that require Docker)

```bash
dotnet test PreflightApi.Tests/PreflightApi.Tests.csproj --filter "FullyQualifiedName!~IntegrationTests&FullyQualifiedName!~BriefingTests.BriefingServiceTests"
```

### Run Specific Tests

```bash
dotnet test PreflightApi.Tests/PreflightApi.Tests.csproj --filter "FullyQualifiedName~NavlogServiceTests"
```

### Run with Coverage

```bash
dotnet test PreflightApi.Tests/PreflightApi.Tests.csproj --collect:"XPlat Code Coverage"
```

### Testing Tools

- **xUnit** - Test framework
- **FluentAssertions** - Readable assertions
- **NSubstitute** - Mocking framework
- **Testcontainers.PostgreSql** - Integration tests with real PostgreSQL + PostGIS database
- **RichardSzalay.MockHttp** - HTTP client mocking
- **Bogus** - Fake data generation
- **MockQueryable.NSubstitute** - LINQ queryable mocking

### Integration Tests

Integration tests use Testcontainers to spin up a real PostgreSQL + PostGIS container. They are marked with `[Collection("Integration")]` and inherit from `PostgreSqlTestBase`. These tests require Docker to be running.

## API Documentation

When the API is running, full interactive documentation is available via Swagger:

- **Swagger UI**: `http://localhost:7014/swagger`
- **OpenAPI JSON**: `http://localhost:7014/swagger/v1/swagger.json`

All endpoints use URL-segment versioning: `/api/v1/...`

## Project Structure

```
PreflightApi/
├── PreflightApi.Domain/                 # Core domain layer (no dependencies)
│   ├── Entities/                        # Domain entities
│   ├── Enums/                           # Domain enumerations
│   ├── Exceptions/                      # Domain exceptions
│   ├── ValueObjects/                    # Value objects (grouped by domain area)
│   └── Utilities/                       # Unit conversions
│
├── PreflightApi.Infrastructure/         # Data access and external services
│   ├── Data/
│   │   ├── PreflightApiDbContext.cs     # EF Core DbContext (PostGIS enabled)
│   │   └── Configurations/             # Entity type configurations
│   ├── Migrations/                      # EF Core migrations
│   ├── Services/
│   │   ├── WeatherServices/            # Weather data query services
│   │   ├── AirportInformationServices/ # Airport, runway, airspace, frequency, obstacle services
│   │   ├── NotamServices/              # NOTAM query service, NMS API client, parsers
│   │   ├── DocumentServices/           # Chart supplement, terminal procedure services
│   │   ├── CronJobServices/            # Background sync services (organized by data source)
│   │   ├── CloudStorage/               # Azure Blob Storage service
│   │   ├── CertificateRenewal/         # SSL certificate renewal
│   │   └── Telemetry/                  # Application Insights sync telemetry
│   ├── Dtos/                            # Data transfer objects and mappers
│   ├── Interfaces/                      # Service interfaces
│   └── Settings/                        # Configuration model classes
│
├── PreflightApi.API/                    # ASP.NET Core Web API
│   ├── Controllers/                     # API controllers
│   ├── Configuration/                   # Service registration
│   ├── Middleware/                       # Gateway secret, global exceptions, API version header
│   ├── Models/                          # API request/response models
│   └── Program.cs                       # Application entry point
│
├── PreflightApi.Azure.Functions/        # Azure Functions (timer-triggered data sync)
│   ├── Functions/                       # Timer-triggered sync functions
│   └── Program.cs                       # Functions host configuration
│
├── PreflightApi.Tests/                  # Test project
│   ├── IntegrationTests/               # Database integration tests (Testcontainers)
│   └── */                              # Unit tests organized by service area
│
├── infra/                               # Azure infrastructure (Bicep templates)
│   ├── main.bicep                       # Main deployment template
│   ├── modules/                         # Bicep modules (APIM, App Service, Functions, PostgreSQL, etc.)
│   └── parameters/                      # Environment parameter files
│
├── apim-policies/                       # Azure API Management policies
├── docs/                                # Documentation
├── .github/workflows/                   # CI/CD pipelines
│   ├── develop-ci-cd.yml               # Develop branch: build, test, deploy to staging
│   ├── main-ci-cd.yml                  # Main branch: build, test, release, deploy to prod
│   └── pr-validation.yml              # Pull request validation
├── docker-compose.local.yml             # Local development compose
├── Dockerfile.local                     # Development Dockerfile
├── .env.example                         # Environment template
└── PreflightApi.sln                     # Solution file
```

## Infrastructure

The project includes full Azure infrastructure-as-code using Bicep templates in the `infra/` directory:

- **Azure App Service** - Hosts the API
- **Azure Functions** (Flex Consumption) - Runs cron job sync functions
- **Azure PostgreSQL Flexible Server** - Database with PostGIS
- **Azure Blob Storage** - Document PDF storage
- **Azure API Management** - API gateway with subscription tiers and rate limiting
- **Azure Key Vault** - Secret management
- **Application Insights** - Monitoring and telemetry

CI/CD is handled by GitHub Actions with OIDC authentication to Azure. See `docs/infrastructure-setup.md` for the full setup guide.

## Contributing

### Development Workflow

1. Create a feature branch from `develop`:
   ```bash
   git checkout develop
   git pull origin develop
   git checkout -b feature/your-feature-name
   ```

2. Make your changes and ensure tests pass:
   ```bash
   dotnet build PreflightApi.sln
   dotnet test PreflightApi.Tests/PreflightApi.Tests.csproj
   ```

3. Commit with meaningful messages:
   ```bash
   git commit -m "Add feature description"
   ```

4. Push and create a Pull Request to `develop`:
   ```bash
   git push origin feature/your-feature-name
   ```

### Code Style

- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Write unit tests for new functionality

### Branch Strategy

- `main` - Production-ready code
- `develop` - Integration branch for features
- `feature/*` - Feature branches
- `bugfix/*` - Bug fix branches

## License

[Add your license information here]

## Support

For issues and feature requests, please use the [GitHub Issues](https://github.com/BeyondBelief96/PreflightApi/issues) page.
