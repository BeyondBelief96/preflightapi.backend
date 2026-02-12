using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.ValueObjects.Metar;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services.CronJobServices.WeatherServices.SchemaManifests;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Services.CronJobServices
{
    public class MetarCronService : IAviationWeatherService<Metar>
    {
        private readonly ILogger<MetarCronService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly PreflightApiDbContext _dbContext;
        private const string MetarUrl = "https://aviationweather.gov/data/cache/metars.cache.xml.gz";

        public MetarCronService(
        ILogger<MetarCronService> logger,
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
                _logger.LogInformation("Starting METAR data fetch and storage");
                var xmlData = await FetchMetarXmlDataAsync(cancellationToken);

                if (xmlData == null)
                {
                    _logger.LogInformation("No METAR data available from API (204 No Content)");
                    return;
                }

                var metarData = ParseMetarXmlData(xmlData, cancellationToken).ToList();

                // Load all existing METARs in a single query
                var stationIds = metarData.Select(m => m.StationId).Distinct().ToList();
                var existingMetars = await _dbContext.Metars
                    .Where(m => stationIds.Contains(m.StationId))
                    .ToDictionaryAsync(m => m.StationId!, cancellationToken);

                var errorCount = 0;
                foreach (var metar in metarData)
                {
                    try
                    {
                        if (existingMetars.TryGetValue(metar.StationId!, out var existing))
                        {
                            if (existing.RawText != metar.RawText)
                                UpdateMetarFields(existing, metar);
                        }
                        else
                        {
                            await _dbContext.Metars.AddAsync(metar, cancellationToken);
                            existingMetars[metar.StationId!] = metar;
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogWarning(ex, "Failed to process METAR for station {StationId}", metar.StationId);
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                if (errorCount > 0)
                    _logger.LogWarning("Completed METAR data update with {ErrorCount} record errors", errorCount);
                else
                    _logger.LogInformation("Completed METAR data update");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching METAR data");
                throw;
            }
        }

        private async Task<string?> FetchMetarXmlDataAsync(CancellationToken cancellationToken)
        {
            using var client = _httpClientFactory.CreateClient(ServiceCollectionExtensions.WeatherHttpClient);
            using var response = await client.GetAsync(MetarUrl, cancellationToken);

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
                    _logger.LogError("Aviation Weather API returned 400 Bad Request for METAR data");
                    throw new HttpRequestException("Aviation Weather API returned 400 Bad Request - invalid parameters or URL");

                case HttpStatusCode.NotFound:
                    _logger.LogError("Aviation Weather API returned 404 Not Found for METAR endpoint");
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

        private IEnumerable<Metar> ParseMetarXmlData(string xmlData, CancellationToken cancellationToken)
        {
            var metars = new List<Metar>();
            var doc = XDocument.Parse(xmlData);
            var metarElements = doc.Descendants("METAR");

            // Validate schema on first element
            var firstElement = metarElements.FirstOrDefault();
            if (firstElement != null)
            {
                var validationResult = AvWxSchemaValidator.ValidateElement("metar", firstElement);
                if (validationResult.HasDrift)
                {
                    if (validationResult.MissingElements.Count > 0)
                        _logger.LogError("Schema drift detected in METAR XML: missing expected elements: {Elements}",
                            string.Join(", ", validationResult.MissingElements));
                    if (validationResult.UnexpectedElements.Count > 0)
                        _logger.LogWarning("Schema drift detected in METAR XML: unexpected new elements: {Elements}",
                            string.Join(", ", validationResult.UnexpectedElements));
                    if (validationResult.MissingAttributes.Count > 0)
                        _logger.LogError("Schema drift detected in METAR XML: missing expected attributes: {Attributes}",
                            string.Join(", ", validationResult.MissingAttributes));
                    if (validationResult.UnexpectedAttributes.Count > 0)
                        _logger.LogWarning("Schema drift detected in METAR XML: unexpected new attributes: {Attributes}",
                            string.Join(", ", validationResult.UnexpectedAttributes));
                }
            }

            foreach (var element in metarElements)
            {
                var stationId = element.Element("station_id")?.Value;

                // Only process US stations (starting with K or P)
                if (string.IsNullOrEmpty(stationId) ||
                    !stationId.StartsWith("K") && !stationId.StartsWith("P"))
                {
                    continue;
                }

                var metar = new Metar
                {
                    RawText = element.Element("raw_text")?.Value,
                    StationId = stationId,
                    ObservationTime = element.Element("observation_time")?.Value,
                    Latitude = ParsingUtilities.ParseNullableFloat(element.Element("latitude")?.Value),
                    Longitude = ParsingUtilities.ParseNullableFloat(element.Element("longitude")?.Value),
                    TempC = ParsingUtilities.ParseNullableFloat(element.Element("temp_c")?.Value),
                    DewpointC = ParsingUtilities.ParseNullableFloat(element.Element("dewpoint_c")?.Value),
                    WindDirDegrees = element.Element("wind_dir_degrees")?.Value,
                    WindSpeedKt = ParsingUtilities.ParseNullableInt(element.Element("wind_speed_kt")?.Value),
                    WindGustKt = ParsingUtilities.ParseNullableInt(element.Element("wind_gust_kt")?.Value),
                    VisibilityStatuteMi = element.Element("visibility_statute_mi")?.Value,
                    AltimInHg = ParsingUtilities.ParseNullableFloat(element.Element("altim_in_hg")?.Value),
                    SeaLevelPressureMb = ParsingUtilities.ParseNullableFloat(element.Element("sea_level_pressure_mb")?.Value),
                    QualityControlFlags = ParseQualityControlFlags(element.Element("quality_control_flags")),
                    WxString = element.Element("wx_string")?.Value,
                    SkyCondition = ParseSkyConditions(element.Elements("sky_condition")),
                    FlightCategory = element.Element("flight_category")?.Value,
                    MetarType = element.Element("metar_type")?.Value,
                    ElevationM = ParsingUtilities.ParseNullableFloat(element.Element("elevation_m")?.Value)
                };

                metars.Add(metar);
            }

            return metars;
        }

        private static void UpdateMetarFields(Metar existing, Metar metar)
        {
            existing.RawText = metar.RawText;
            existing.ObservationTime = metar.ObservationTime;
            existing.Latitude = metar.Latitude;
            existing.Longitude = metar.Longitude;
            existing.TempC = metar.TempC;
            existing.DewpointC = metar.DewpointC;
            existing.WindDirDegrees = metar.WindDirDegrees;
            existing.WindSpeedKt = metar.WindSpeedKt;
            existing.WindGustKt = metar.WindGustKt;
            existing.VisibilityStatuteMi = metar.VisibilityStatuteMi;
            existing.AltimInHg = metar.AltimInHg;
            existing.SeaLevelPressureMb = metar.SeaLevelPressureMb;
            existing.QualityControlFlags = metar.QualityControlFlags;
            existing.WxString = metar.WxString;
            existing.SkyCondition = metar.SkyCondition;
            existing.FlightCategory = metar.FlightCategory;
            existing.ThreeHrPressureTendencyMb = metar.ThreeHrPressureTendencyMb;
            existing.MaxTC = metar.MaxTC;
            existing.MinTC = metar.MinTC;
            existing.MaxT24hrC = metar.MaxT24hrC;
            existing.MinT24hrC = metar.MinT24hrC;
            existing.PrecipIn = metar.PrecipIn;
            existing.Pcp3hrIn = metar.Pcp3hrIn;
            existing.Pcp6hrIn = metar.Pcp6hrIn;
            existing.Pcp24hrIn = metar.Pcp24hrIn;
            existing.SnowIn = metar.SnowIn;
            existing.VertVisFt = metar.VertVisFt;
            existing.MetarType = metar.MetarType;
            existing.ElevationM = metar.ElevationM;
        }

        private static MetarQualityControlFlags? ParseQualityControlFlags(XElement? element)
        {
            if (element == null) return null;

            return new MetarQualityControlFlags
            {
                Corrected = element.Element("corrected")?.Value,
                Auto = element.Element("auto")?.Value,
                AutoStation = element.Element("auto_station")?.Value,
                MaintenanceIndicatorOn = element.Element("maintenance_indicator_on")?.Value,
                NoSignal = element.Element("no_signal")?.Value,
                LightningSensorOff = element.Element("lightning_sensor_off")?.Value,
                FreezingRainSensorOff = element.Element("freezing_rain_sensor_off")?.Value,
                PresentWeatherSensorOff = element.Element("present_weather_sensor_off")?.Value
            };
        }

        private static List<MetarSkyCondition>? ParseSkyConditions(IEnumerable<XElement> elements)
        {
            var conditions = elements
                .Select(element => new MetarSkyCondition
                {
                    SkyCover = element.Attribute("sky_cover")?.Value ?? string.Empty,
                    CloudBaseFtAgl = ParsingUtilities.ParseNullableInt(element.Attribute("cloud_base_ft_agl")?.Value)
                })
                .ToList();

            return conditions.Any() ? conditions : null;
        }
    }
}
