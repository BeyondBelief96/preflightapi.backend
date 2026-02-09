using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Domain.ValueObjects.NavigationalAids;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Enums;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services.CronJobServices.NasrServices.Mappings;
using System.IO.Compression;

namespace PreflightApi.Infrastructure.Services.CronJobServices.NasrServices;

public class NavigationalAidCronService : FaaNasrBaseService<NavigationalAid>, INavigationalAidCronService
{
    protected override NasrDataType DataType => NasrDataType.NAV;
    protected override string[] UniqueIdentifiers => new[] { "NavId", "NavType" };
    protected override PublicationType PublicationType => PublicationType.NasrSubscription_NavigationalAids;
    protected override IEnumerable<(string FileName, Type ClassMap, bool IsBaseData)> CsvMappings =>
    new[]
    {
        ("NAV_BASE.csv", typeof(NavigationalAidMap), true),
        ("NAV_RMK.csv", typeof(NavigationalAidRemarkMap), false),
    };

    protected override bool UsesLegacySiteNoDeduplication => true;

    public NavigationalAidCronService(
        ILogger<NavigationalAidCronService> logger,
        IHttpClientFactory httpClientFactory,
        IFaaPublicationCycleService faaPublicationCycleService,
        PreflightApiDbContext dbContext)
        : base(logger, httpClientFactory, faaPublicationCycleService, dbContext)
    {
    }

    protected override async Task PostProcessAsync(ZipArchive archive, CancellationToken cancellationToken)
    {
        var entry = archive.Entries.FirstOrDefault(e =>
            e.Name.Equals("NAV_CKPT.csv", StringComparison.OrdinalIgnoreCase));

        if (entry == null)
        {
            Logger.LogWarning("NAV_CKPT.csv not found in archive");
            return;
        }

        Logger.LogInformation("Processing NAV_CKPT.csv checkpoint data");

        await using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream);
        using var csv = new CsvReader(reader, GetCsvConfiguration());
        ConfigureCsvReader(csv);
        csv.Context.RegisterClassMap<NavCheckpointMap>();

        var grouped = new Dictionary<string, List<NavaidCheckpoint>>();

        await foreach (var record in csv.GetRecordsAsync<NavCheckpointRecord>(cancellationToken))
        {
            var key = $"{record.NavId}|{record.NavType}";
            if (!grouped.TryGetValue(key, out var list))
            {
                list = new List<NavaidCheckpoint>();
                grouped[key] = list;
            }

            list.Add(new NavaidCheckpoint
            {
                Altitude = record.Altitude,
                Bearing = record.Bearing,
                AirGroundCode = record.AirGroundCode,
                Description = record.Description,
                AirportId = record.AirportId,
                StateCheckCode = record.StateCheckCode
            });
        }

        Logger.LogInformation("Grouped {Count} NAVAIDs with checkpoints", grouped.Count);

        var allKeys = grouped.Keys.ToList();
        const int batchSize = 100;

        for (var i = 0; i < allKeys.Count; i += batchSize)
        {
            var batchKeys = allKeys.Skip(i).Take(batchSize).ToList();
            var navIds = batchKeys.Select(k => k.Split('|')[0]).Distinct().ToList();

            var entities = await DbContext.Set<NavigationalAid>()
                .Where(e => navIds.Contains(e.NavId))
                .ToListAsync(cancellationToken);

            foreach (var entity in entities)
            {
                var key = $"{entity.NavId}|{entity.NavType}";
                if (grouped.TryGetValue(key, out var checkpoints))
                {
                    entity.Checkpoints = checkpoints;
                }
            }

            await DbContext.SaveChangesAsync(cancellationToken);
        }

        Logger.LogInformation("Completed processing NAV_CKPT.csv checkpoint data");
    }
}
