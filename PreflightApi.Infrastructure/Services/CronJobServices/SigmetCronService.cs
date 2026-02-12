using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.ValueObjects.Sigmets;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services.CronJobServices.WeatherServices.SchemaManifests;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Services.CronJobServices
{
    public class SigmetCronService : IAviationWeatherService<Sigmet>
    {
        private readonly ILogger<SigmetCronService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly PreflightApiDbContext _dbContext;
        private const string SigmetUrl = "https://aviationweather.gov/data/cache/airsigmets.cache.xml.gz";

        public SigmetCronService(
            ILogger<SigmetCronService> logger,
            IHttpClientFactory httpClientFactory,
            PreflightApiDbContext dbContext)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _dbContext = dbContext;
        }

        public async Task PollWeatherDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting SIGMET data fetch and storage");
                var xmlData = await FetchSigmetXmlDataAsync(cancellationToken);

                if (xmlData == null)
                {
                    _logger.LogInformation("No SIGMET data available from API (204 No Content)");
                    await PurgeExpiredSigmetsAsync(cancellationToken);
                    return;
                }

                var sigmetData = ParseSigmetXmlData(xmlData);
                await UpdateOrCreateSigmetsAsync(sigmetData, cancellationToken);
                await PurgeExpiredSigmetsAsync(cancellationToken);
                _logger.LogInformation("Completed SIGMET data update");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching SIGMET data");
                throw;
            }
        }

        private async Task<string?> FetchSigmetXmlDataAsync(CancellationToken cancellationToken)
        {
            using var client = _httpClientFactory.CreateClient(ServiceCollectionExtensions.WeatherHttpClient);
            using var response = await client.GetAsync(SigmetUrl, cancellationToken);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    await using (var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken))
                    await using (var decompressedStream = new System.IO.Compression.GZipStream(
                        responseStream,
                        System.IO.Compression.CompressionMode.Decompress))
                    using (var reader = new StreamReader(decompressedStream))
                    {
                        var content = await reader.ReadToEndAsync(cancellationToken);
                        return string.IsNullOrWhiteSpace(content) ? null : content;
                    }

                case HttpStatusCode.NoContent:
                    return null;

                case HttpStatusCode.BadRequest:
                    _logger.LogError("Aviation Weather API returned 400 Bad Request for SIGMET data");
                    throw new HttpRequestException("Aviation Weather API returned 400 Bad Request - invalid parameters or URL");

                case HttpStatusCode.NotFound:
                    _logger.LogError("Aviation Weather API returned 404 Not Found for SIGMET endpoint");
                    throw new HttpRequestException("Aviation Weather API endpoint not found (404)");

                case HttpStatusCode.TooManyRequests:
                    _logger.LogWarning("Aviation Weather API rate limit exceeded (429 Too Many Requests)");
                    throw new HttpRequestException("Aviation Weather API rate limit exceeded (429)");

                case HttpStatusCode.InternalServerError:
                    _logger.LogError("Aviation Weather API returned 500 Internal Server Error");
                    throw new HttpRequestException("Aviation Weather API internal server error (500)");

                case HttpStatusCode.BadGateway:
                case HttpStatusCode.GatewayTimeout:
                    _logger.LogWarning("Aviation Weather API service disruption ({StatusCode})", (int)response.StatusCode);
                    throw new HttpRequestException($"Aviation Weather API service disruption ({(int)response.StatusCode})");

                default:
                    _logger.LogError("Aviation Weather API returned unexpected status code {StatusCode}", (int)response.StatusCode);
                    throw new HttpRequestException($"Aviation Weather API returned unexpected status code: {(int)response.StatusCode}");
            }
        }

        private IEnumerable<Sigmet> ParseSigmetXmlData(string xmlData)
        {
            var sigmets = new List<Sigmet>();
            var doc = XDocument.Parse(xmlData);
            var sigmetElements = doc.Descendants("AIRSIGMET");

            // Validate schema on first element
            var firstElement = sigmetElements.FirstOrDefault();
            if (firstElement != null)
            {
                var validationResult = AvWxSchemaValidator.ValidateElement("sigmet", firstElement);
                if (validationResult.HasDrift)
                {
                    if (validationResult.MissingElements.Count > 0)
                        _logger.LogError("Schema drift detected in SIGMET XML: missing expected elements: {Elements}",
                            string.Join(", ", validationResult.MissingElements));
                    if (validationResult.UnexpectedElements.Count > 0)
                        _logger.LogWarning("Schema drift detected in SIGMET XML: unexpected new elements: {Elements}",
                            string.Join(", ", validationResult.UnexpectedElements));
                    if (validationResult.MissingAttributes.Count > 0)
                        _logger.LogError("Schema drift detected in SIGMET XML: missing expected attributes: {Attributes}",
                            string.Join(", ", validationResult.MissingAttributes));
                    if (validationResult.UnexpectedAttributes.Count > 0)
                        _logger.LogWarning("Schema drift detected in SIGMET XML: unexpected new attributes: {Attributes}",
                            string.Join(", ", validationResult.UnexpectedAttributes));
                }
            }

            foreach (var element in sigmetElements)
            {
                try
                {
                    sigmets.Add(new Sigmet
                    {
                        RawText = element.Element("raw_text")?.Value,
                        ValidTimeFrom = element.Element("valid_time_from")?.Value,
                        ValidTimeTo = element.Element("valid_time_to")?.Value,
                        MovementDirDegrees = ParsingUtilities.ParseNullableInt(element.Element("movement_dir_degrees")?.Value),
                        MovementSpeedKt = ParsingUtilities.ParseNullableInt(element.Element("movement_spd_kt")?.Value),
                        SigmetType = element.Element("airsigmet_type")?.Value,
                        Altitude = ParseAltitude(element.Element("altitude")),
                        Hazard = ParseHazard(element.Element("hazard")),
                        Areas = ParseAreas(element.Elements("area"))
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse SIGMET element");
                }
            }

            return sigmets;
        }

        private static SigmetAltitude? ParseAltitude(XElement? element)
        {
            if (element == null) return null;

            return new SigmetAltitude
            {
                MinFtMsl = ParsingUtilities.ParseNullableInt(element.Attribute("min_ft_msl")?.Value),
                MaxFtMsl = ParsingUtilities.ParseNullableInt(element.Attribute("max_ft_msl")?.Value)
            };
        }

        private static SigmetHazard? ParseHazard(XElement? element)
        {
            if (element == null) return null;

            return new SigmetHazard
            {
                Type = element.Attribute("type")?.Value,
                Severity = element.Attribute("severity")?.Value
            };
        }

        private static List<SigmetArea>? ParseAreas(IEnumerable<XElement> elements)
        {
            var areas = elements.Select(areaElement =>
            {
                var points = areaElement.Elements("point")
                    .Select(pointElement => new SigmetPoint
                    {
                        Longitude = ParsingUtilities.ParseFloat(pointElement.Element("longitude")?.Value ?? "0"),
                        Latitude = ParsingUtilities.ParseFloat(pointElement.Element("latitude")?.Value ?? "0")
                    })
                    .ToList();

                return new SigmetArea
                {
                    NumPoints = ParsingUtilities.ParseInt(areaElement.Attribute("num_points")?.Value ?? "0"),
                    Points = points
                };
            }).ToList();

            return areas.Any() ? areas : null;
        }

        private async Task UpdateOrCreateSigmetsAsync(IEnumerable<Sigmet> sigmets, CancellationToken cancellationToken)
        {
            var sigmetsList = sigmets.ToList();
            var validTimeFroms = sigmetsList
                .Where(a => a.ValidTimeFrom != null)
                .Select(a => a.ValidTimeFrom!)
                .Distinct()
                .ToList();

            // Remove existing SIGMETs that overlap with incoming data
            // (ValidTimeFrom is not unique — multiple SIGMETs can share the same start time)
            var deleted = await _dbContext.Sigmets
                .Where(a => a.ValidTimeFrom != null && validTimeFroms.Contains(a.ValidTimeFrom))
                .ExecuteDeleteAsync(cancellationToken);

            if (deleted > 0)
                _logger.LogDebug("Removed {Count} existing SIGMETs for replacement", deleted);

            await _dbContext.Sigmets.AddRangeAsync(sigmetsList, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task PurgeExpiredSigmetsAsync(CancellationToken cancellationToken)
        {
            var currentTime = DateTime.UtcNow.ToString("O");

            var result = await _dbContext.Sigmets
                .Where(a => a.ValidTimeTo != null && a.ValidTimeTo.CompareTo(currentTime) < 0)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation("Purged {Count} expired SIGMETs", result);
        }
    }
}
