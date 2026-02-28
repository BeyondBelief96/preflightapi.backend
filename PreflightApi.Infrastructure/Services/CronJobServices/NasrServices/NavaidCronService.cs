using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.ValueObjects;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Enums;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services.CronJobServices.NasrServices.Mappings;
using PreflightApi.Infrastructure.Services.CronJobServices.NasrServices.SchemaManifests;
using PreflightApi.Infrastructure.Services.CronJobServices.NasrServices.Utils;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Services.CronJobServices.NasrServices
{
    public class NavaidCronService : FaaNasrBaseService<Navaid>, INavaidCronService
    {
        private readonly ILogger<NavaidCronService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IFaaPublicationCycleService _faaPublicationCycleService;
        private readonly PreflightApiDbContext _dbContext;
        private readonly ISyncTelemetryService _telemetry;
        private const string BaseUrl = "https://nfdc.faa.gov/webContent/28DaySub/extra/";
        private const int DbBatchSize = 100;

        protected override NasrDataType DataType => NasrDataType.NAV;

        protected override string[] UniqueIdentifiers => new[]
        {
            "NavId",
            "NavType",
            "CountryCode",
            "City"
        };

        protected override IEnumerable<(string FileName, Type ClassMap, bool IsBaseData)> CsvMappings =>
            new[]
            {
                ("NAV_BASE.csv", typeof(NavaidBaseMap), true),
            };

        protected override bool UsesLegacySiteNoDeduplication => false;

        protected override PublicationType PublicationType => PublicationType.NasrSubscription_Navaids;

        public NavaidCronService(
            ILogger<NavaidCronService> logger,
            IHttpClientFactory httpClientFactory,
            IFaaPublicationCycleService faaPublicationCycleService,
            PreflightApiDbContext dbContext,
            ISyncTelemetryService telemetry)
            : base(logger, httpClientFactory, faaPublicationCycleService, dbContext, telemetry)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _faaPublicationCycleService = faaPublicationCycleService;
            _dbContext = dbContext;
            _telemetry = telemetry;
        }

        public async Task ProcessCheckpointsAndRemarksAsync(CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            var publicationCycle = await _faaPublicationCycleService.GetPublicationCycleAsync(PublicationType);
            if (publicationCycle == null)
            {
                _logger.LogWarning("No publication cycle found for NAV checkpoints/remarks");
                return;
            }

            var currentPublicationDate = FaaPublicationDateUtils.CalculateCurrentPublicationDate(
                publicationCycle.KnownValidDate,
                publicationCycle.CycleLengthDays);
            var dateString = FaaPublicationDateUtils.FormatDateForNasr(currentPublicationDate);
            var fileName = $"{dateString}_NAV_CSV.zip";
            var zipUrl = $"{BaseUrl}{fileName}";

            try
            {
                _logger.LogInformation("Downloading NAV data for checkpoint/remark enrichment from {ZipUrl}", zipUrl);
                using var client = _httpClientFactory.CreateClient(ServiceCollectionExtensions.FaaDataHttpClient);
                await using var response = await client.GetStreamAsync(zipUrl, cancellationToken);

                // Buffer into memory so we can read multiple entries
                using var memoryStream = new MemoryStream();
                await response.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;

                using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);

                await ProcessCheckpointsAsync(archive, cancellationToken);
                await ProcessRemarksAsync(archive, cancellationToken);

                _logger.LogInformation("NAV checkpoint/remark enrichment completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing NAV checkpoint/remark data");
                throw;
            }
        }

        private async Task ProcessCheckpointsAsync(ZipArchive archive, CancellationToken cancellationToken)
        {
            var entry = archive.Entries.FirstOrDefault(e =>
                e.Name.Equals("NAV_CKPT.csv", StringComparison.OrdinalIgnoreCase));

            if (entry == null)
            {
                _logger.LogWarning("NAV_CKPT.csv not found in archive");
                return;
            }

            _logger.LogInformation("Processing NAV_CKPT.csv for checkpoint data");

            // Parse all checkpoints and group by NAVAID key
            var checkpointsByKey = new Dictionary<string, List<NavaidCheckpoint>>();

            await using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream);
            using var csv = new CsvReader(reader, GetCsvConfiguration());

            ConfigureCsvReader(csv);
            csv.Context.RegisterClassMap<NavaidCheckpointMap>();

            await csv.ReadAsync();
            csv.ReadHeader();
            ValidateHeaders(entry.Name, csv.HeaderRecord!);

            while (await csv.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var navId = csv.GetField("NAV_ID")?.Trim() ?? string.Empty;
                var navType = csv.GetField("NAV_TYPE")?.Trim() ?? string.Empty;
                var countryCode = csv.GetField("COUNTRY_CODE")?.Trim() ?? string.Empty;
                var city = csv.GetField("CITY")?.Trim() ?? string.Empty;
                var key = string.Join("|", navId, navType, countryCode, city);

                var checkpoint = csv.GetRecord<NavaidCheckpoint>();
                if (checkpoint == null) continue;

                if (!checkpointsByKey.TryGetValue(key, out var list))
                {
                    list = new List<NavaidCheckpoint>();
                    checkpointsByKey[key] = list;
                }
                list.Add(checkpoint);
            }

            _logger.LogInformation("Parsed {Count} NAVAID keys with checkpoints", checkpointsByKey.Count);

            // Batch update the database
            await BatchUpdateJsonColumnAsync(
                checkpointsByKey,
                (navaid, json) => navaid.CheckpointsJson = json,
                "checkpoints",
                cancellationToken);
        }

        private async Task ProcessRemarksAsync(ZipArchive archive, CancellationToken cancellationToken)
        {
            var entry = archive.Entries.FirstOrDefault(e =>
                e.Name.Equals("NAV_RMK.csv", StringComparison.OrdinalIgnoreCase));

            if (entry == null)
            {
                _logger.LogWarning("NAV_RMK.csv not found in archive");
                return;
            }

            _logger.LogInformation("Processing NAV_RMK.csv for remark data");

            var remarksByKey = new Dictionary<string, List<NavaidRemark>>();

            await using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream);
            using var csv = new CsvReader(reader, GetCsvConfiguration());

            ConfigureCsvReader(csv);
            csv.Context.RegisterClassMap<NavaidRemarkMap>();

            await csv.ReadAsync();
            csv.ReadHeader();
            ValidateHeaders(entry.Name, csv.HeaderRecord!);

            while (await csv.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var navId = csv.GetField("NAV_ID")?.Trim() ?? string.Empty;
                var navType = csv.GetField("NAV_TYPE")?.Trim() ?? string.Empty;
                var countryCode = csv.GetField("COUNTRY_CODE")?.Trim() ?? string.Empty;
                var city = csv.GetField("CITY")?.Trim() ?? string.Empty;
                var key = string.Join("|", navId, navType, countryCode, city);

                var remark = csv.GetRecord<NavaidRemark>();
                if (remark == null) continue;

                if (!remarksByKey.TryGetValue(key, out var list))
                {
                    list = new List<NavaidRemark>();
                    remarksByKey[key] = list;
                }
                list.Add(remark);
            }

            _logger.LogInformation("Parsed {Count} NAVAID keys with remarks", remarksByKey.Count);

            await BatchUpdateJsonColumnAsync(
                remarksByKey,
                (navaid, json) => navaid.RemarksJson = json,
                "remarks",
                cancellationToken);
        }

        private async Task BatchUpdateJsonColumnAsync<TItem>(
            Dictionary<string, List<TItem>> dataByKey,
            Action<Navaid, string> setJsonColumn,
            string columnName,
            CancellationToken cancellationToken)
        {
            var keys = dataByKey.Keys.ToList();
            var totalUpdated = 0;

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                for (var i = 0; i < keys.Count; i += DbBatchSize)
                {
                    var batchKeys = keys.Skip(i).Take(DbBatchSize).ToList();

                    // Find existing navaids matching these keys in a single query
                    var navIds = batchKeys
                        .Select(k => k.Split('|'))
                        .Where(p => p.Length == 4)
                        .Select(p => p[0])
                        .Distinct()
                        .ToList();

                    var navaids = (await _dbContext.Navaids
                            .Where(n => navIds.Contains(n.NavId))
                            .ToListAsync(cancellationToken))
                        .Where(n => batchKeys.Contains(n.CreateUniqueKey()))
                        .ToList();

                    foreach (var navaid in navaids)
                    {
                        var key = navaid.CreateUniqueKey();
                        if (dataByKey.TryGetValue(key, out var items))
                        {
                            var json = JsonSerializer.Serialize(items);
                            setJsonColumn(navaid, json);
                            _dbContext.Entry(navaid).State = EntityState.Modified;
                            totalUpdated++;
                        }
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Updated {Count} navaids with {Column} data (batch {Batch})",
                        navaids.Count, columnName, (i / DbBatchSize) + 1);
                }

                await transaction.CommitAsync(cancellationToken);
            });

            _logger.LogInformation("Completed updating {Total} navaids with {Column} data", totalUpdated, columnName);
        }

        private void ValidateHeaders(string fileName, string[] headers)
        {
            var validationResult = NasrSchemaValidator.ValidateHeaders(fileName, headers);
            if (validationResult.HasDrift)
            {
                if (validationResult.MissingColumns.Count > 0)
                    _logger.LogError("Schema drift detected in {FileName}: missing expected columns: {Columns}",
                        fileName, string.Join(", ", validationResult.MissingColumns));
                if (validationResult.UnexpectedColumns.Count > 0)
                    _logger.LogWarning("Schema drift detected in {FileName}: unexpected new columns: {Columns}",
                        fileName, string.Join(", ", validationResult.UnexpectedColumns));
            }
        }

        private static CsvConfiguration GetCsvConfiguration()
        {
            return new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null,
                IgnoreBlankLines = true,
                DetectDelimiter = true
            };
        }

        private static void ConfigureCsvReader(CsvReader csv)
        {
            csv.Context.TypeConverterCache.RemoveConverter<decimal?>();
            csv.Context.TypeConverterCache.RemoveConverter<int?>();
            csv.Context.TypeConverterCache.AddConverter<decimal?>(new OptionalDecimalConverter());
            csv.Context.TypeConverterCache.AddConverter<int?>(new OptionalIntConverter());
            csv.Context.TypeConverterCache.AddConverter<DateTime?>(new OptionalDateConverter());
        }
    }
}
