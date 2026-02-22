using System.Diagnostics;
using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Services.CronJobServices;

public class ObstacleCronService : IObstacleCronService
{
    private readonly ILogger<ObstacleCronService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PreflightApiDbContext _dbContext;
    private readonly GeometryFactory _geometryFactory;
    private readonly ISyncTelemetryService _telemetry;

    private const int BatchSize = 5000;
    private const int HeaderLines = 4;

    public ObstacleCronService(
        ILogger<ObstacleCronService> logger,
        IHttpClientFactory httpClientFactory,
        PreflightApiDbContext dbContext,
        ISyncTelemetryService telemetry)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _dbContext = dbContext;
        _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        _telemetry = telemetry;
    }

    public async Task DownloadAndProcessObstaclesAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var publicationCycle = await _dbContext.FaaPublicationCycles
                .FirstOrDefaultAsync(p => p.PublicationType == PublicationType.Obstacles, cancellationToken);

            if (publicationCycle == null)
            {
                throw new Exception("No publication cycle found for Obstacles.");
            }

            var currentPublicationDate = FaaPublicationDateUtils.CalculateCurrentPublicationDate(
                publicationCycle.KnownValidDate, publicationCycle.CycleLengthDays);
            // KnownValidDate is the release date; the URL uses the "Reflects Changes To" date which is 2 days earlier
            var reflectsChangesToDate = currentPublicationDate.AddDays(-2);
            var dateString = FaaPublicationDateUtils.FormatDateForObstacles(reflectsChangesToDate);
            var url = $"https://aeronav.faa.gov/Obst_Data/DOF_{dateString}.zip";

            _logger.LogInformation(
                "Starting obstacle download from URL: {Url} for publication date: {PublicationDate}",
                url,
                currentPublicationDate);

            using var client = _httpClientFactory.CreateClient(ServiceCollectionExtensions.FaaDataHttpClient);
            using var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var zipArchive = new ZipArchive(stream);

            var obstacles = new List<Obstacle>();

            // Use DOF.DAT master file which contains all obstacles (no duplicates)
            var dofEntry = zipArchive.Entries.FirstOrDefault(e =>
                e.Name.Equals("DOF.DAT", StringComparison.OrdinalIgnoreCase));

            if (dofEntry == null)
            {
                throw new Exception("DOF.DAT master file not found in ZIP archive");
            }

            _logger.LogInformation("Processing master file: {FileName}", dofEntry.Name);

            using var entryStream = dofEntry.Open();
            using var reader = new StreamReader(entryStream);

            // Skip header lines
            for (int i = 0; i < HeaderLines; i++)
            {
                await reader.ReadLineAsync(cancellationToken);
            }

            string? line;
            var lineNumber = HeaderLines;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line) || line.Length < 100)
                {
                    continue;
                }

                var obstacle = ObstacleLineParser.ParseObstacleLine(line, _geometryFactory);
                if (obstacle != null)
                {
                    obstacles.Add(obstacle);
                }
                else
                {
                    _logger.LogWarning("Failed to parse obstacle line in {FileName} at line {LineNumber}: {Line}",
                        dofEntry.Name, lineNumber, line.Length > 50 ? line[..50] + "..." : line);
                }

                // Log progress every 100,000 records
                if (lineNumber % 100000 == 0)
                {
                    _logger.LogInformation("Parsed {Count} obstacles so far...", obstacles.Count);
                }
            }

            _logger.LogInformation("Parsed {ObstacleCount} obstacles from DOF.DAT. Starting database update...",
                obstacles.Count);

            // Full refresh wrapped in a transaction so a mid-insert failure doesn't leave partial data
            // Must use execution strategy to support NpgsqlRetryingExecutionStrategy with transactions
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                await _dbContext.Obstacles.ExecuteDeleteAsync(cancellationToken);
                _logger.LogInformation("Deleted existing obstacles from database");

                // Batch insert
                for (int i = 0; i < obstacles.Count; i += BatchSize)
                {
                    var batch = obstacles.Skip(i).Take(BatchSize).ToList();
                    await _dbContext.Obstacles.AddRangeAsync(batch, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    _dbContext.ChangeTracker.Clear();

                    _logger.LogInformation("Inserted batch {BatchNumber}/{TotalBatches} ({Count} obstacles)",
                        (i / BatchSize) + 1,
                        (obstacles.Count / BatchSize) + 1,
                        batch.Count);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Completed obstacle processing. Total obstacles: {Count}", obstacles.Count);
                _telemetry.TrackSyncCompleted("Obstacle", obstacles.Count, 0, sw.ElapsedMilliseconds);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing obstacles");
            _telemetry.TrackSyncFailed("Obstacle", ex, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
