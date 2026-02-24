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
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PreflightApi.Infrastructure.HealthChecks;
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
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready" })
    .AddInfrastructureHealthChecks();

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
app.UseDataFreshness();

app.UseOpenApi();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUi();
}

app.UseHttpsRedirection();
app.MapControllers();
// Liveness probe — is the process alive? Always returns 200.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthResponse
});

// Readiness probe — can the API serve traffic? Checks database + blob storage.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    },
    ResponseWriter = WriteHealthResponse
});

// Data freshness detail endpoint
app.MapGet("/health/data-freshness", async (IDataSyncStatusService svc, CancellationToken ct) =>
{
    var freshness = await svc.GetAllFreshnessAsync(ct);
    return Results.Ok(new { checkedAt = DateTime.UtcNow, dataTypes = freshness });
});

// Full status page — all checks including external dependencies.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    },
    ResponseWriter = WriteHealthResponse
});

async Task WriteHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var result = new
    {
        status = report.Status.ToString(),
        version = assemblyVersion,
        totalDuration = report.TotalDuration.TotalMilliseconds,
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            duration = e.Value.Duration.TotalMilliseconds,
            description = e.Value.Description,
            tags = e.Value.Tags,
            exception = app.Environment.IsDevelopment() ? e.Value.Exception?.Message : null
        })
    };
    await context.Response.WriteAsJsonAsync(result);
}
await app.RunAsync();
