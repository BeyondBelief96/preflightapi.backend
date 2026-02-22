using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO.Compression;
using System.Xml.Linq;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Settings;
using PreflightApi.Infrastructure.Utilities;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Infrastructure.Services.CronJobServices
{
    public class TerminalProcedureCronService : ITerminalProcedureCronService
    {
        private readonly ILogger<TerminalProcedureCronService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly PreflightApiDbContext _dbContext;
        private readonly ICloudStorageService _cloudStorageService;
        private readonly CloudStorageSettings _cloudStorageSettings;

        public TerminalProcedureCronService(
            ILogger<TerminalProcedureCronService> logger,
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

        public async Task DownloadAndProcessTerminalProceduresAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var publicationCycle = await _dbContext.FaaPublicationCycles.FirstOrDefaultAsync(
                    p => p.PublicationType == PublicationType.TerminalProcedure, cancellationToken);

                if (publicationCycle == null)
                {
                    throw new Exception("No publication cycle found for Terminal Procedures.");
                }

                var currentPublicationDate = FaaPublicationDateUtils.CalculateCurrentPublicationDate(publicationCycle.KnownValidDate, publicationCycle.CycleLengthDays);
                var dateString = FaaPublicationDateUtils.FormatDateForTerminalProcedures(currentPublicationDate);
                var regions = new[] { "A", "B", "C", "D", "E" };

                // Track all uploaded blob names so we can clean up stale ones after
                var uploadedBlobNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var region in regions)
                {
                    var faaUrl = $"https://aeronav.faa.gov/upload_313-d/terminal/DDTPP{region}_{dateString}.zip";

                    _logger.LogInformation(
                        "Starting download from URL: {Url} for region: {Region}",
                        faaUrl,
                        region);

                    using var client = _httpClientFactory.CreateClient(ServiceCollectionExtensions.FaaDataHttpClient);
                    using var response = await client.GetStreamAsync(faaUrl, cancellationToken);
                    using var zipArchive = new ZipArchive(response);

                    if (region == "E")
                    {
                        var xmlEntry = zipArchive.Entries.FirstOrDefault(e => e.Name.EndsWith(".xml"));
                        if (xmlEntry == null)
                        {
                            throw new Exception("Terminal procedure metadata XML file not found in zip archive.");
                        }

                        using var xmlStream = xmlEntry.Open();
                        using var reader = new StreamReader(xmlStream);
                        var xmlContent = await reader.ReadToEndAsync(cancellationToken);

                        await ParseAndStoreXmlDataAsync(xmlContent, cancellationToken);
                    }

                    // Extract ALL PDF files from the ZIP (not just airport diagrams)
                    var pdfEntries = zipArchive.Entries.Where(e =>
                        e.Name.EndsWith(".PDF", StringComparison.OrdinalIgnoreCase));
                    var uploaded = await UploadPdfsToStorageAsync(pdfEntries, uploadedBlobNames, cancellationToken);
                    foreach (var name in uploaded) uploadedBlobNames.Add(name);
                }

                // Clean up stale blobs that aren't in the new publication cycle
                await DeleteStaleBlobsAsync(uploadedBlobNames, cancellationToken);

                _logger.LogInformation("Completed terminal procedure processing.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing terminal procedures");
                throw;
            }
        }

        private async Task DeleteStaleBlobsAsync(HashSet<string> uploadedBlobNames, CancellationToken cancellationToken)
        {
            var containerName = _cloudStorageSettings.TerminalProceduresContainerName;

            try
            {
                var existingBlobs = await _cloudStorageService.ListBlobsAsync(containerName);
                var staleBlobs = existingBlobs
                    .Where(b => !uploadedBlobNames.Contains(b))
                    .ToList();

                if (staleBlobs.Count > 0)
                {
                    await _cloudStorageService.DeleteBlobsAsync(containerName, staleBlobs);
                    _logger.LogInformation("Deleted {StaleCount} stale terminal procedures from storage (kept {KeptCount})",
                        staleBlobs.Count, uploadedBlobNames.Count);
                }
                else
                {
                    _logger.LogInformation("No stale terminal procedures to clean up");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting stale blobs from container: {ContainerName}", containerName);
                throw;
            }
        }

        private async Task<List<string>> UploadPdfsToStorageAsync(
            IEnumerable<ZipArchiveEntry> pdfEntries,
            HashSet<string> alreadyUploaded,
            CancellationToken cancellationToken)
        {
            const int batchSize = 50;
            var containerName = _cloudStorageSettings.TerminalProceduresContainerName;
            var uploadedNames = new List<string>();

            try
            {
                // Skip PDFs already uploaded from previous regions
                var entriesList = pdfEntries
                    .Where(e => !alreadyUploaded.Contains(e.Name))
                    .ToList();
                var totalCount = entriesList.Count;
                var uploadedCount = 0;

                for (int i = 0; i < totalCount; i += batchSize)
                {
                    var batch = entriesList.Skip(i).Take(batchSize).ToList();
                    var blobs = new List<(string BlobName, byte[] Content, string ContentType)>();

                    foreach (var pdfEntry in batch)
                    {
                        using var pdfStream = pdfEntry.Open();
                        using var memoryStream = new MemoryStream();
                        await pdfStream.CopyToAsync(memoryStream, cancellationToken);
                        blobs.Add((pdfEntry.Name, memoryStream.ToArray(), "application/pdf"));
                    }

                    if (blobs.Count > 0)
                    {
                        await _cloudStorageService.UploadBlobsAsync(containerName, blobs);
                        uploadedCount += blobs.Count;
                        uploadedNames.AddRange(blobs.Select(b => b.BlobName));
                        _logger.LogInformation("Uploaded batch of {BatchCount} terminal procedures ({UploadedCount}/{TotalCount})",
                            blobs.Count, uploadedCount, totalCount);
                    }
                }

                _logger.LogInformation("Completed uploading {Count} terminal procedures to storage", uploadedCount);
                return uploadedNames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading PDFs to container: {ContainerName}", containerName);
                throw;
            }
        }

        private async Task ParseAndStoreXmlDataAsync(string xmlContent, CancellationToken cancellationToken)
        {
            const int batchSize = 1000;

            var doc = XDocument.Parse(xmlContent);
            var procedures = doc.Descendants("airport_name")
                .SelectMany(airport => airport.Elements("record")
                .Select(record => new TerminalProcedure
                {
                    AirportName = airport.Attribute("ID")?.Value ?? "",
                    IcaoIdent = airport.Attribute("icao_ident")?.Value,
                    AirportIdent = airport.Attribute("apt_ident")?.Value,
                    ChartCode = record.Element("chart_code")?.Value ?? "",
                    ChartName = record.Element("chart_name")?.Value ?? "",
                    PdfFileName = record.Element("pdf_name")?.Value ?? "",
                    AmendmentNumber = record.Element("amdnt_num")?.Value,
                    AmendmentDate = record.Element("amdnt_date")?.Value
                }))
                .Where(tp => !string.IsNullOrEmpty(tp.AirportName) &&
                       !string.IsNullOrEmpty(tp.ChartCode) &&
                       !string.IsNullOrEmpty(tp.ChartName) &&
                       !string.IsNullOrEmpty(tp.PdfFileName))
                .ToList();

            _logger.LogInformation("Processing {Count} terminal procedures from XML", procedures.Count);

            // Clear-and-reload strategy: delete all existing records, then bulk insert
            await _dbContext.TerminalProcedures.ExecuteDeleteAsync(cancellationToken);
            _logger.LogInformation("Cleared existing terminal procedure records");

            for (int i = 0; i < procedures.Count; i += batchSize)
            {
                var batch = procedures.Skip(i).Take(batchSize).ToList();
                await _dbContext.TerminalProcedures.AddRangeAsync(batch, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Inserted terminal procedure batch: {Count} records ({Processed}/{Total})",
                    batch.Count, Math.Min(i + batchSize, procedures.Count), procedures.Count);
            }
        }
    }
}
