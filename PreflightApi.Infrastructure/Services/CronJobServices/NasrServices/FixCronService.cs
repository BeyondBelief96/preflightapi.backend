using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Domain.ValueObjects.Fixes;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Enums;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services.CronJobServices.NasrServices.Mappings;
using System.IO.Compression;

namespace PreflightApi.Infrastructure.Services.CronJobServices.NasrServices;

public class FixCronService : FaaNasrBaseService<Fix>, IFixCronService
{
    protected override NasrDataType DataType => NasrDataType.FIX;
    protected override string[] UniqueIdentifiers => new[] { "FixId", "IcaoRegionCode", "StateCode", "CountryCode" };
    protected override PublicationType PublicationType => PublicationType.NasrSubscription_Fixes;
    protected override IEnumerable<(string FileName, Type ClassMap, bool IsBaseData)> CsvMappings =>
    new[]
    {
        ("FIX_BASE.csv", typeof(FixMap), true),
    };

    protected override bool UsesLegacySiteNoDeduplication => false;

    public FixCronService(
        ILogger<FixCronService> logger,
        IHttpClientFactory httpClientFactory,
        IFaaPublicationCycleService faaPublicationCycleService,
        PreflightApiDbContext dbContext)
        : base(logger, httpClientFactory, faaPublicationCycleService, dbContext)
    {
    }

    protected override async Task PostProcessAsync(ZipArchive archive, CancellationToken cancellationToken)
    {
        var chartData = await ReadAndGroupCsvAsync<FixChartRecord, FixChartMap>(
            archive, "FIX_CHRT.csv", r => $"{r.FixId}|{r.IcaoRegionCode}|{r.StateCode}|{r.CountryCode}", cancellationToken);

        var navData = await ReadAndGroupCsvAsync<FixNavRecord, FixNavMap>(
            archive, "FIX_NAV.csv", r => $"{r.FixId}|{r.IcaoRegionCode}|{r.StateCode}|{r.CountryCode}", cancellationToken);

        var allKeys = chartData.Keys.Union(navData.Keys).ToList();

        if (allKeys.Count == 0)
        {
            Logger.LogInformation("No supplementary FIX data found");
            return;
        }

        Logger.LogInformation("Processing {ChartKeys} fixes with chart data and {NavKeys} fixes with NAVAID references",
            chartData.Count, navData.Count);

        const int batchSize = 100;

        for (var i = 0; i < allKeys.Count; i += batchSize)
        {
            var batchKeys = allKeys.Skip(i).Take(batchSize).ToList();
            var fixIds = batchKeys.Select(k => k.Split('|')[0]).Distinct().ToList();

            var entities = await DbContext.Set<Fix>()
                .Where(e => fixIds.Contains(e.FixId))
                .ToListAsync(cancellationToken);

            foreach (var entity in entities)
            {
                var key = $"{entity.FixId}|{entity.IcaoRegionCode}|{entity.StateCode}|{entity.CountryCode}";

                if (chartData.TryGetValue(key, out var charts))
                {
                    entity.ChartingTypes = charts
                        .Select(c => c.ChartingTypeDesc)
                        .Where(d => !string.IsNullOrWhiteSpace(d))
                        .Distinct()
                        .ToList()!;
                }

                if (navData.TryGetValue(key, out var navs))
                {
                    entity.NavaidReferences = navs.Select(n => new FixNavaidReference
                    {
                        NavId = n.NavId,
                        NavType = n.NavType,
                        Bearing = n.Bearing,
                        Distance = n.Distance
                    }).ToList();
                }
            }

            await DbContext.SaveChangesAsync(cancellationToken);
        }

        Logger.LogInformation("Completed processing supplementary FIX data");
    }

    private async Task<Dictionary<string, List<TRecord>>> ReadAndGroupCsvAsync<TRecord, TMap>(
        ZipArchive archive, string fileName, Func<TRecord, string> keySelector, CancellationToken cancellationToken)
        where TMap : ClassMap
    {
        var result = new Dictionary<string, List<TRecord>>();

        var entry = archive.Entries.FirstOrDefault(e =>
            e.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));

        if (entry == null)
        {
            Logger.LogWarning("{FileName} not found in archive", fileName);
            return result;
        }

        Logger.LogInformation("Reading {FileName}", fileName);

        await using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream);
        using var csv = new CsvReader(reader, GetCsvConfiguration());
        ConfigureCsvReader(csv);
        csv.Context.RegisterClassMap<TMap>();

        await foreach (var record in csv.GetRecordsAsync<TRecord>(cancellationToken))
        {
            var key = keySelector(record);
            if (!result.TryGetValue(key, out var list))
            {
                list = new List<TRecord>();
                result[key] = list;
            }

            list.Add(record);
        }

        Logger.LogInformation("Read {Count} groups from {FileName}", result.Count, fileName);
        return result;
    }
}
