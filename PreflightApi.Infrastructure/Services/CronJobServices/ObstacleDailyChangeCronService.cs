using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Services.CronJobServices;

public class ObstacleDailyChangeCronService : IObstacleDailyChangeCronService
{
    private readonly ILogger<ObstacleDailyChangeCronService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PreflightApiDbContext _dbContext;
    private readonly GeometryFactory _geometryFactory;

    private const string ChangeFileUrl = "https://aeronav.faa.gov/Obst_Data/DOF_DAILY_CHANGE_UPDATE.ZIP";
    private const string ChangeFileName = "DOF_DAILY_CHANGE_UPDATE.DAT";
    private const int ActionFieldLength = 10;

    public ObstacleDailyChangeCronService(
        ILogger<ObstacleDailyChangeCronService> logger,
        IHttpClientFactory httpClientFactory,
        PreflightApiDbContext dbContext)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _dbContext = dbContext;
        _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
    }

    public async Task ProcessDailyChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting daily obstacle change download from {Url}", ChangeFileUrl);

            using var client = _httpClientFactory.CreateClient(ServiceCollectionExtensions.FaaDataHttpClient);
            using var response = await client.GetAsync(ChangeFileUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var zipArchive = new ZipArchive(stream);

            var changeEntry = zipArchive.Entries.FirstOrDefault(e =>
                e.Name.Equals(ChangeFileName, StringComparison.OrdinalIgnoreCase));

            if (changeEntry == null)
            {
                throw new Exception($"{ChangeFileName} not found in ZIP archive");
            }

            using var entryStream = changeEntry.Open();
            using var reader = new StreamReader(entryStream);

            var adds = new List<Obstacle>();
            var updates = new List<Obstacle>();
            var dismantles = new List<string>();
            var skippedOld = 0;
            var parseErrors = 0;

            // Skip header lines — detect the dash separator line and start after it
            var headersPassed = false;
            string? line;
            var lineNumber = 0;

            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                lineNumber++;

                if (!headersPassed)
                {
                    // Header section ends with a line of dashes (e.g., "----------...")
                    if (line.TrimStart().StartsWith("---"))
                    {
                        headersPassed = true;
                    }
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Parse ACTION from positions 0-9
                var actionField = line.Length >= ActionFieldLength
                    ? line[..ActionFieldLength].Trim().ToUpperInvariant()
                    : string.Empty;

                if (string.IsNullOrEmpty(actionField))
                {
                    parseErrors++;
                    _logger.LogWarning("Empty action field at line {LineNumber}", lineNumber);
                    continue;
                }

                // Parse obstacle data from position 10+
                var obstacleData = line.Length > ActionFieldLength ? line[ActionFieldLength..] : string.Empty;

                switch (actionField)
                {
                    case "ADD":
                    case "NEW":
                    {
                        var obstacle = ObstacleLineParser.ParseObstacleLine(obstacleData, _geometryFactory);
                        if (obstacle != null)
                        {
                            if (actionField == "ADD")
                                adds.Add(obstacle);
                            else
                                updates.Add(obstacle);
                        }
                        else
                        {
                            parseErrors++;
                            _logger.LogWarning(
                                "Failed to parse {Action} obstacle at line {LineNumber}: {Line}",
                                actionField, lineNumber, line.Length > 60 ? line[..60] + "..." : line);
                        }
                        break;
                    }
                    case "OLD":
                        skippedOld++;
                        break;
                    case "DISMANTLE":
                    {
                        var obstacle = ObstacleLineParser.ParseObstacleLine(obstacleData, _geometryFactory);
                        if (obstacle != null)
                        {
                            dismantles.Add(obstacle.OasNumber);
                        }
                        else
                        {
                            parseErrors++;
                            _logger.LogWarning(
                                "Failed to parse DISMANTLE obstacle at line {LineNumber}: {Line}",
                                lineNumber, line.Length > 60 ? line[..60] + "..." : line);
                        }
                        break;
                    }
                    default:
                        parseErrors++;
                        _logger.LogWarning("Unknown action '{Action}' at line {LineNumber}", actionField, lineNumber);
                        break;
                }
            }

            _logger.LogInformation(
                "Parsed daily changes: {Adds} adds, {Updates} updates (NEW), {Dismantles} dismantles, {SkippedOld} OLD skipped, {ParseErrors} parse errors",
                adds.Count, updates.Count, dismantles.Count, skippedOld, parseErrors);

            if (adds.Count == 0 && updates.Count == 0 && dismantles.Count == 0)
            {
                _logger.LogInformation("No obstacle changes to process");
                return;
            }

            // Collect all OAS numbers we need to check for existing records
            var allUpsertOasNumbers = adds.Select(o => o.OasNumber)
                .Concat(updates.Select(o => o.OasNumber))
                .Distinct()
                .ToList();

            var existingOasNumbers = await _dbContext.Obstacles
                .Where(o => allUpsertOasNumbers.Contains(o.OasNumber))
                .Select(o => o.OasNumber)
                .ToListAsync(cancellationToken);

            var existingSet = new HashSet<string>(existingOasNumbers);

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            var addedCount = 0;
            var updatedCount = 0;
            var dismantledCount = 0;

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                // Process ADDs — insert if missing, update if exists (idempotent)
                foreach (var obstacle in adds)
                {
                    if (existingSet.Contains(obstacle.OasNumber))
                    {
                        UpdateExistingObstacle(obstacle);
                        updatedCount++;
                    }
                    else
                    {
                        await _dbContext.Obstacles.AddAsync(obstacle, cancellationToken);
                        addedCount++;
                    }
                }

                // Process NEWs — update existing, insert if missing (upsert for safety)
                foreach (var obstacle in updates)
                {
                    if (existingSet.Contains(obstacle.OasNumber))
                    {
                        UpdateExistingObstacle(obstacle);
                        updatedCount++;
                    }
                    else
                    {
                        await _dbContext.Obstacles.AddAsync(obstacle, cancellationToken);
                        addedCount++;
                    }
                }

                // Process DISMANTLEs — delete by OAS number (no-op if already gone)
                if (dismantles.Count > 0)
                {
                    var toDelete = await _dbContext.Obstacles
                        .Where(o => dismantles.Contains(o.OasNumber))
                        .ToListAsync(cancellationToken);
                    dismantledCount = toDelete.Count;
                    _dbContext.Obstacles.RemoveRange(toDelete);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            });

            _logger.LogInformation(
                "Daily obstacle change processing complete: {Added} added, {Updated} updated, {Dismantled} dismantled",
                addedCount, updatedCount, dismantledCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing daily obstacle changes");
            throw;
        }
    }

    private void UpdateExistingObstacle(Obstacle source)
    {
        var entry = _dbContext.Obstacles.Attach(new Obstacle { OasNumber = source.OasNumber });
        entry.CurrentValues.SetValues(source);
    }
}
