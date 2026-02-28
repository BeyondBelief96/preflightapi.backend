using System.Reflection;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.AzureAppServices;
using Microsoft.Extensions.Options;
using Npgsql;
using PreflightApi.API.Configuration;
using PreflightApi.API.Middleware;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services;
using PreflightApi.Infrastructure.Services.AirportInformationServices;
using PreflightApi.Infrastructure.Services.DocumentServices;
using PreflightApi.Infrastructure.Services.NotamServices;
using PreflightApi.Infrastructure.Services.WeatherServices;
using PreflightApi.Infrastructure.Settings;
using PreflightApi.Infrastructure.HealthChecks;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Utilities;

var builder = WebApplication.CreateBuilder(args);

// Determine if running in Docker
bool isRunningInDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true, reloadOnChange: true);

// Setup Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
if (!builder.Environment.IsDevelopment())
{
    builder.Logging.AddAzureWebAppDiagnostics();

    // Add Application Insights if connection string is configured
    var appInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
    if (!string.IsNullOrEmpty(appInsightsConnectionString))
    {
        builder.Services.AddApplicationInsightsTelemetry(options =>
        {
            options.ConnectionString = appInsightsConnectionString;
        });
    }
}

builder.Services.Configure<AzureFileLoggerOptions>(options =>
{
    options.FileName = "PreflightApi-api-log";
    options.FileSizeLimit = 10 * 1024 * 1024; // 10 MB
    options.RetainedFileCountLimit = 10;
});

// Setup Controller Json Serialization Handling
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new GeometryJsonConverter());
    options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// Transform model binding / validation errors into ApiErrorResponse format
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                e => e.Key,
                e => e.Value!.Errors.Select(err => err.ErrorMessage).ToList());

        var response = new PreflightApi.API.Models.ApiErrorResponse
        {
            Code = PreflightApi.Domain.Exceptions.ErrorCodes.ValidationError,
            Message = "One or more validation errors occurred.",
            ValidationErrors = errors,
            Timestamp = DateTime.UtcNow.ToString("o"),
            TraceId = context.HttpContext.TraceIdentifier,
            Path = context.HttpContext.Request.Path.Value
        };

        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(response);
    };
});

// Derive API URL version from assembly major version
var apiMajorVersion = Assembly.GetExecutingAssembly().GetName().Version?.Major ?? 1;

// Setup API Versioning
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

// Read assembly version for Swagger
var assemblyVersion = Assembly.GetExecutingAssembly()
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
    ?.InformationalVersion?.Split('+')[0] ?? "unknown";

// Setup Swagger
builder.Services.AddOpenApiDocument(options =>
{
    options.Title = "PreflightApi";
    options.Version = $"v{apiMajorVersion} ({assemblyVersion})";
    options.Description = "Aviation data API for VFR flight planning — weather, airports, airspace, NOTAMs, navigation, and E6B flight computer calculations.";
    options.DocumentProcessors.Add(new ControllerXmlDocProcessor());
    options.OperationProcessors.Add(new OperationXmlDocProcessor());
});

// Setup Environment Variable Settings
builder.Services.Configure<NOAASettings>(builder.Configuration.GetSection("NOAASettings"));
builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("Database"));
builder.Services.Configure<NmsSettings>(builder.Configuration.GetSection("NmsSettings"));

// Setup DB Context
builder.Services.AddDbContext<PreflightApiDbContext>((serviceProvider, options) =>
{
    var dbSettings = serviceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value;
    var connectionString = dbSettings.GetConnectionString();

    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
    dataSourceBuilder.UseNetTopologySuite();
    dataSourceBuilder.EnableDynamicJson();

    options.UseNpgsql(dataSourceBuilder.Build(),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(3);
            npgsqlOptions.CommandTimeout(30);
            npgsqlOptions.UseNetTopologySuite();
        });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
    }
}, ServiceLifetime.Scoped);

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PreflightApiDbContext>("database",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: new[] { "ready" })
    .AddInfrastructureHealthChecks();

// Background health monitor
builder.Services.Configure<HealthMonitorSettings>(
    builder.Configuration.GetSection("HealthMonitor"));
builder.Services.AddSingleton<HealthSnapshotStore>();
builder.Services.AddSingleton<IHealthSnapshotStore>(sp => sp.GetRequiredService<HealthSnapshotStore>());
builder.Services.AddHostedService<HealthMonitorService>();

// Configure Services
builder.Services.AddMemoryCache();
builder.Services.AddCloudStorageServices(builder.Configuration);
builder.Services.AddScoped<IMetarService, MetarService>();
builder.Services.AddScoped<IPirepService, PirepService>();
builder.Services.AddScoped<ITafService, TafService>();
builder.Services.AddScoped<ISigmetService, SigmetService>();
builder.Services.AddScoped<IGAirmetService, GAirmetService>();
builder.Services.AddScoped<ITerminalProcedureService, TerminalProcedureService>();
builder.Services.AddScoped<IChartSupplementService, ChartSupplementService>();  
builder.Services.AddScoped<IAirportService, AirportService>();
builder.Services.AddScoped<IRunwayService, RunwayService>();
builder.Services.AddScoped<ICommunicationFrequencyService, CommunicationFrequencyService>();
builder.Services.AddScoped<IAirspaceService, AirspaceService>();
builder.Services.AddScoped<IObstacleService, ObstacleService>();
builder.Services.AddScoped<INavaidService, NavaidService>();
builder.Services.AddScoped<IMagneticVariationService, MagneticVariationService>();
builder.Services.AddScoped<IWindsAloftService, WindsAloftService>();
builder.Services.AddScoped<INavlogService, NavlogService>();
builder.Services.AddScoped<IE6bCalculatorService, E6bCalculatorService>();

// NOTAM Services (DB-backed, synced by Azure Functions)
builder.Services.AddScoped<INotamService, NotamService>();

// Briefing Services
builder.Services.AddScoped<IBriefingService, BriefingService>();

// Data Sync Status
builder.Services.AddScoped<IDataSyncStatusService, DataSyncStatusService>();

builder.Services.AddResilientHttpClients();

var app = builder.Build();

// Initialize Azure Blob Storage resources on startup
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var cloudStorageInitService = scope.ServiceProvider.GetRequiredService<ICloudStorageInitializationService>();

    logger.LogInformation("Initializing Azure Blob Storage resources during startup...");
    try
    {
        await cloudStorageInitService.InitializeAsync(CancellationToken.None);
        logger.LogInformation("Azure Blob Storage resources initialized successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize Azure Blob Storage resources");
    }
}

app.UseGlobalExceptionHandling();
app.UseGatewaySecretValidation();
app.UseApiVersionHeader();
app.UseDataCurrency();

app.UseOpenApi();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUi();
}

app.UseHttpsRedirection();
app.MapControllers();

// Cached health status — served from background monitor snapshot.
app.MapGet("/health", (IHealthSnapshotStore snapshotStore) =>
{
    var snapshot = snapshotStore.Current;

    if (snapshot is null)
    {
        var starting = new HealthCheckResponse
        {
            Status = "Starting",
            Version = assemblyVersion,
            TotalDuration = 0,
            LastCheckedAt = null,
            Checks = []
        };
        return Results.Json(starting, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var response = new HealthCheckResponse
    {
        Status = snapshot.OverallStatus.ToString(),
        Version = assemblyVersion,
        TotalDuration = snapshot.TotalDuration,
        LastCheckedAt = snapshot.LastCheckedAt,
        Checks = snapshot.Entries.Select(e => new HealthCheckEntry
        {
            Name = e.Name,
            Status = e.Status.ToString(),
            Duration = e.Duration,
            Description = e.Description,
            Tags = e.Tags,
            Exception = app.Environment.IsDevelopment() ? e.Exception : null
        })
    };

    var statusCode = snapshot.OverallStatus == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy
        ? StatusCodes.Status503ServiceUnavailable
        : StatusCodes.Status200OK;

    return Results.Json(response, statusCode: statusCode);
});

// Data freshness detail endpoint
app.MapGet("/health/data-currency", async (IDataSyncStatusService svc, CancellationToken ct) =>
{
    var freshness = await svc.GetAllCurrencyAsync(ct);
    var staleCount = freshness.Count(f => !f.IsFresh);
    var overallStatus = staleCount == 0 ? "healthy"
        : freshness.Any(f => f.Severity == "critical") ? "critical"
        : freshness.Any(f => f.Severity == "warning") ? "degraded"
        : "info";

    return Results.Ok(new DataCurrencyResponse
    {
        CheckedAt = DateTime.UtcNow,
        OverallStatus = overallStatus,
        Summary = new DataCurrencySummary
        {
            Total = freshness.Count,
            Fresh = freshness.Count(f => f.IsFresh),
            Stale = staleCount,
            BySeverity = freshness.GroupBy(f => f.Severity)
                .ToDictionary(g => g.Key, g => g.Count())
        },
        DataTypes = freshness
    });
});

await app.RunAsync();
