using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO.Compression;
using System.Xml.Linq;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Settings;
using PreflightApi.Infrastructure.Utilities;
using PreflightApi.Infrastructure.Services.CronJobServices.FaaDocServices.SchemaManifests;

namespace PreflightApi.Infrastructure.Services.CronJobServices
{
    public class ChartSupplementCronService : IChartSupplementCronService
    {
        private readonly ILogger<ChartSupplementCronService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly PreflightApiDbContext _dbContext;
        private readonly ICloudStorageService _cloudStorageService;
        private readonly CloudStorageSettings _cloudStorageSettings;

        public ChartSupplementCronService(
            ILogger<ChartSupplementCronService> logger,
            IHttpClientFactory httpClientFactory,
            PreflightApiDbContext dbContext,
            ICloudStorageService cloudStorageService,
            IOptions<CloudStorageSettings> cloudStorageSettings)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _dbContext = dbContext;
            _cloudStorageService = cloudStorageService;
            _cloudStorageSettings = cloudStorageSettings.Value;
        }

        public async Task DownloadAndProcessChartSupplementsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var currentDate = DateTime.UtcNow;

                var publicationCycle = await _dbContext.FaaPublicationCycles.FirstOrDefaultAsync(p => p.PublicationType == PublicationType.ChartSupplement);

                if (publicationCycle == null)
                {
                    throw new Exception("No publication cycle found for Chart Supplements.");
                }

                var currentPublicationDate = FaaPublicationDateUtils.CalculateCurrentPublicationDate(publicationCycle.KnownValidDate, publicationCycle.CycleLengthDays);
                var dateString = FaaPublicationDateUtils.FormatDateForChartSupplements(currentPublicationDate);
                var faaChartSupplementUrl = $"https://aeronav.faa.gov/Upload_313-d/supplements/DCS_{dateString}.zip";

                _logger.LogInformation(
                "Starting download from URL: {Url} for publication date: {PublicationDate}",
                faaChartSupplementUrl,
                currentPublicationDate);

                using var client = _httpClientFactory.CreateClient(ServiceCollectionExtensions.FaaDataHttpClient);
                using var response = await client.GetStreamAsync(faaChartSupplementUrl, cancellationToken);
                using var zipArchive = new ZipArchive(response);

                var xmlEntry = zipArchive.Entries.FirstOrDefault(e => e.Name.EndsWith(".xml"));
                if (xmlEntry == null)
                {
                    throw new Exception("Chart Supplment Database XML file not found in zip archive.");
                }

                using var xmlStream = xmlEntry.Open();
                using var reader = new StreamReader(xmlStream);
                var xmlContent = await reader.ReadToEndAsync();

                await ParseAndStoreXmlDataAsync(xmlContent, cancellationToken);

                var pdfEntries = zipArchive.Entries.Where(e => e.Name.EndsWith(".pdf"));
                await UploadPdfsToStorageAsync(pdfEntries, cancellationToken);

                _logger.LogInformation("Completed chart supplement processing.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chart supplements");
                throw;
            }
        }

        private async Task ParseAndStoreXmlDataAsync(string xmlContent, CancellationToken cancellationToken)
        {
            const int batchSize = 1000;

            try
            {
                // Parse XML and group data in memory first
                var doc = XDocument.Parse(xmlContent);

                // Validate schema on first record to detect drift
                var validationResult = FaaDocSchemaValidator.Validate("chartsupplement", doc);
                if (validationResult.HasDrift)
                {
                    if (validationResult.MissingElements.Count > 0)
                        _logger.LogError("Schema drift detected in Chart Supplement XML: missing expected elements: {Elements}",
                            string.Join(", ", validationResult.MissingElements));
                    if (validationResult.UnexpectedElements.Count > 0)
                        _logger.LogWarning("Schema drift detected in Chart Supplement XML: unexpected new elements: {Elements}",
                            string.Join(", ", validationResult.UnexpectedElements));
                    if (validationResult.MissingAttributes.Count > 0)
                        _logger.LogError("Schema drift detected in Chart Supplement XML: missing expected attributes: {Attributes}",
                            string.Join(", ", validationResult.MissingAttributes));
                    if (validationResult.UnexpectedAttributes.Count > 0)
                        _logger.LogWarning("Schema drift detected in Chart Supplement XML: unexpected new attributes: {Attributes}",
                            string.Join(", ", validationResult.UnexpectedAttributes));
                }

                var supplements = doc.Descendants("location")
                    .SelectMany(location => location.Elements("airport"))
                    .Select(airport => new ChartSupplement
                    {
                        AirportName = NullIfEmpty(airport.Element("aptname")?.Value),
                        AirportCity = NullIfEmpty(airport.Element("aptcity")?.Value),
                        NavigationalAidName = NullIfEmpty(airport.Element("navidname")?.Value),
                        AirportCode = NullIfEmpty(airport.Element("aptid")?.Value),
                        FileName = NullIfEmpty(airport.Element("pages")?.Element("pdf")?.Value)
                    })
                    .ToList();

                // Process airports
                var airportSupplements = supplements
                    .Where(s => !string.IsNullOrEmpty(s.AirportCode))
                    .ToList();

                if (airportSupplements.Any())
                {
                    await ProcessAirportBatch(airportSupplements, batchSize, cancellationToken);
                }

                // Process navaids
                var navaidSupplements = supplements
                    .Where(s => string.IsNullOrEmpty(s.AirportCode) && !string.IsNullOrEmpty(s.NavigationalAidName))
                    .ToList();

                if (navaidSupplements.Any())
                {
                    await ProcessNavaidBatch(navaidSupplements, batchSize, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chart supplements");
                throw;
            }
        }

        private async Task ProcessAirportBatch(List<ChartSupplement> supplements, int batchSize, CancellationToken cancellationToken)
        {
            for (int i = 0; i < supplements.Count; i += batchSize)
            {
                var batch = supplements.Skip(i).Take(batchSize).ToList();
                var batchFileNames = batch
                    .Where(s => !string.IsNullOrEmpty(s.FileName))
                    .Select(s => s.FileName!)
                    .Distinct()
                    .ToList();

                // Get existing records by FileName, then key by (FileName, AirportCode) composite
                var existingSupplements = await _dbContext.ChartSupplements
                    .Where(cs => cs.FileName != null && batchFileNames.Contains(cs.FileName))
                    .ToListAsync(cancellationToken);

                var existingLookup = new Dictionary<(string, string), ChartSupplement>();
                foreach (var existing in existingSupplements)
                {
                    if (!string.IsNullOrEmpty(existing.FileName) && !string.IsNullOrEmpty(existing.AirportCode))
                    {
                        existingLookup.TryAdd((existing.FileName, existing.AirportCode), existing);
                    }
                }

                var newSupplements = new List<ChartSupplement>();

                foreach (var supplement in batch)
                {
                    if (string.IsNullOrEmpty(supplement.FileName) || string.IsNullOrEmpty(supplement.AirportCode))
                    {
                        continue;
                    }

                    if (existingLookup.TryGetValue((supplement.FileName, supplement.AirportCode), out var existingMatch))
                    {
                        existingMatch.AirportName = supplement.AirportName;
                        existingMatch.AirportCity = supplement.AirportCity;
                    }
                    else
                    {
                        newSupplements.Add(supplement);
                    }
                }

                if (newSupplements.Count > 0)
                {
                    await _dbContext.ChartSupplements.AddRangeAsync(newSupplements, cancellationToken);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Processed airport batch: {Count} supplements ({NewCount} new, {UpdatedCount} updated)",
                    batch.Count, newSupplements.Count, batch.Count - newSupplements.Count);
            }
        }

        private async Task ProcessNavaidBatch(List<ChartSupplement> supplements, int batchSize, CancellationToken cancellationToken)
        {
            for (int i = 0; i < supplements.Count; i += batchSize)
            {
                var batch = supplements.Skip(i).Take(batchSize).ToList();
                var batchFileNames = batch
                    .Where(s => !string.IsNullOrEmpty(s.FileName))
                    .Select(s => s.FileName!)
                    .Distinct()
                    .ToList();

                // Get existing records by FileName, then key by (FileName, NavigationalAidName) composite
                var existingSupplements = await _dbContext.ChartSupplements
                    .Where(cs => cs.FileName != null && batchFileNames.Contains(cs.FileName))
                    .ToListAsync(cancellationToken);

                var existingLookup = new Dictionary<(string, string), ChartSupplement>();
                foreach (var existing in existingSupplements)
                {
                    if (!string.IsNullOrEmpty(existing.FileName) && !string.IsNullOrEmpty(existing.NavigationalAidName))
                    {
                        existingLookup.TryAdd((existing.FileName, existing.NavigationalAidName), existing);
                    }
                }

                var newSupplements = new List<ChartSupplement>();

                foreach (var supplement in batch)
                {
                    if (string.IsNullOrEmpty(supplement.FileName) || string.IsNullOrEmpty(supplement.NavigationalAidName))
                    {
                        continue;
                    }

                    var key = (supplement.FileName, supplement.NavigationalAidName);
                    if (existingLookup.TryGetValue(key, out var existingMatch))
                    {
                        existingMatch.AirportName = supplement.AirportName;
                        existingMatch.AirportCity = supplement.AirportCity;
                    }
                    else if (existingLookup.TryAdd(key, supplement))
                    {
                        // TryAdd guards against within-batch duplicates
                        newSupplements.Add(supplement);
                    }
                }

                if (newSupplements.Count > 0)
                {
                    await _dbContext.ChartSupplements.AddRangeAsync(newSupplements, cancellationToken);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Processed navaid batch: {Count} supplements ({NewCount} new, {UpdatedCount} updated)",
                    batch.Count, newSupplements.Count, batch.Count - newSupplements.Count);
            }
        }

        private async Task UploadPdfsToStorageAsync(IEnumerable<ZipArchiveEntry> pdfEntries, CancellationToken cancellationToken)
        {
            const int batchSize = 50; // Process 50 PDFs at a time to avoid memory issues
            var containerName = _cloudStorageSettings.ChartSupplementsContainerName;
            var existingObjects = new Dictionary<string, string>();

            try
            {
                _logger.LogInformation("Listing existing blobs in container: {ContainerName}", containerName);
                var existingBlobs = await _cloudStorageService.ListBlobsAsync(containerName);

                foreach (var blobName in existingBlobs)
                {
                    var baseName = ExtractBaseName(blobName);
                    existingObjects[baseName] = blobName;
                }

                _logger.LogInformation("Found {Count} existing chart supplements in storage", existingObjects.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing existing blobs in container: {ContainerName}", containerName);
                throw;
            }

            var entriesList = pdfEntries.ToList();
            var totalCount = entriesList.Count;
            var uploadedCount = 0;

            // Collect all blobs to delete first (just tracking names, not content)
            var blobsToDelete = new List<string>();
            foreach (var pdfEntry in entriesList)
            {
                var baseName = ExtractBaseName(pdfEntry.Name);
                if (existingObjects.TryGetValue(baseName, out var existingKey))
                {
                    blobsToDelete.Add(existingKey);
                }
            }

            // Batch delete old editions
            if (blobsToDelete.Count > 0)
            {
                _logger.LogInformation("Deleting {Count} old chart supplement editions", blobsToDelete.Count);
                await _cloudStorageService.DeleteBlobsAsync(containerName, blobsToDelete);
            }

            // Process uploads in batches to avoid memory issues
            for (int i = 0; i < totalCount; i += batchSize)
            {
                var batch = entriesList.Skip(i).Take(batchSize).ToList();
                var blobs = new List<(string BlobName, byte[] Content, string ContentType)>();

                foreach (var pdfEntry in batch)
                {
                    try
                    {
                        using var pdfStream = pdfEntry.Open();
                        using var memoryStream = new MemoryStream();
                        await pdfStream.CopyToAsync(memoryStream, cancellationToken);
                        blobs.Add((pdfEntry.Name, memoryStream.ToArray(), "application/pdf"));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading PDF {FileName}", pdfEntry.Name);
                        throw;
                    }
                }

                if (blobs.Count > 0)
                {
                    await _cloudStorageService.UploadBlobsAsync(containerName, blobs);
                    uploadedCount += blobs.Count;
                    _logger.LogInformation("Uploaded batch of {BatchCount} chart supplements ({UploadedCount}/{TotalCount})",
                        blobs.Count, uploadedCount, totalCount);
                }
            }

            _logger.LogInformation("Completed uploading {Count} chart supplements to storage", uploadedCount);
        }

        private static string? NullIfEmpty(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value;

        private static string ExtractBaseName(string chartSupplementFileName)
        {
            // Assuming format is always like "NW_39_26DEC2024.pdf"
            var parts = chartSupplementFileName.Split('_');
            if (parts.Length >= 2)
            {
                return $"{parts[0]}_{parts[1]}";  // Returns "NW_39"
            }

            throw new ArgumentException($"Invalid file name format: {chartSupplementFileName}");
        }
    }
}
