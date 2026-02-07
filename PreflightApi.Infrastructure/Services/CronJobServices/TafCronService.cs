using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.ValueObjects.Taf;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Services.CronJobServices
{
    public class TafCronService : IAviationWeatherService<Taf>
    {
        private readonly ILogger<TafCronService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly PreflightApiDbContext _dbContext;
        private const string TafUrl = "https://aviationweather.gov/data/cache/tafs.cache.xml.gz";

        public TafCronService(
        ILogger<TafCronService> logger,
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
                _logger.LogInformation("Starting TAF data fetch and storage");
                var xmlData = await FetchTafXmlDataAsync(cancellationToken);

                if (xmlData == null)
                {
                    _logger.LogInformation("No TAF data available from API (204 No Content)");
                    return;
                }

                var tafData = ParseTafXmlData(xmlData, cancellationToken).ToList();

                // Load all existing TAFs in a single query
                var stationIds = tafData.Select(t => t.StationId).Distinct().ToList();
                var existingTafs = await _dbContext.Tafs
                    .Where(t => stationIds.Contains(t.StationId))
                    .ToDictionaryAsync(t => t.StationId!, cancellationToken);

                var errorCount = 0;
                foreach (var taf in tafData)
                {
                    try
                    {
                        if (existingTafs.TryGetValue(taf.StationId!, out var existing))
                        {
                            if (existing.RawText != taf.RawText)
                                UpdateTafFields(existing, taf);
                        }
                        else
                        {
                            await _dbContext.Tafs.AddAsync(taf, cancellationToken);
                            existingTafs[taf.StationId!] = taf;
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogWarning(ex, "Failed to process TAF for station {StationId}", taf.StationId);
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                if (errorCount > 0)
                    _logger.LogWarning("Completed TAF data update with {ErrorCount} record errors", errorCount);
                else
                    _logger.LogInformation("Completed TAF data update");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching TAF data");
                throw;
            }
        }

        private async Task<string?> FetchTafXmlDataAsync(CancellationToken cancellationToken)
        {
            using var client = _httpClientFactory.CreateClient(ServiceCollectionExtensions.WeatherHttpClient);
            using var response = await client.GetAsync(TafUrl, cancellationToken);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    await using (var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken))
                    await using (var decompressedStream = new System.IO.Compression.GZipStream(
                        responseStream,
                        System.IO.Compression.CompressionMode.Decompress))
                    using (var reader = new StreamReader(decompressedStream))
                    {
                        return await reader.ReadToEndAsync(cancellationToken);
                    }

                case HttpStatusCode.NoContent:
                    return null;

                case HttpStatusCode.BadRequest:
                    _logger.LogError("Aviation Weather API returned 400 Bad Request for TAF data");
                    throw new HttpRequestException("Aviation Weather API returned 400 Bad Request - invalid parameters or URL");

                case HttpStatusCode.NotFound:
                    _logger.LogError("Aviation Weather API returned 404 Not Found for TAF endpoint");
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

        private IEnumerable<Taf> ParseTafXmlData(string xmlData, CancellationToken cancellationToken)
        {
            var tafs = new List<Taf>();
            var doc = XDocument.Parse(xmlData);
            var tafElements = doc.Descendants("TAF");

            foreach (var element in tafElements)
            {
                var stationId = element.Element("station_id")?.Value;

                // Only process US stations (starting with K or P)
                if (string.IsNullOrEmpty(stationId) ||
                    !stationId.StartsWith("K") && !stationId.StartsWith("P"))
                {
                    continue;
                }

                var taf = new Taf
                {
                    RawText = element.Element("raw_text")?.Value,
                    StationId = stationId,
                    IssueTime = element.Element("issue_time")?.Value,
                    BulletinTime = element.Element("bulletin_time")?.Value,
                    ValidTimeFrom = element.Element("valid_time_from")?.Value,
                    ValidTimeTo = element.Element("valid_time_to")?.Value,
                    Remarks = element.Element("remarks")?.Value,
                    Latitude = ParsingUtilities.ParseNullableFloat(element.Element("latitude")?.Value),
                    Longitude = ParsingUtilities.ParseNullableFloat(element.Element("longitude")?.Value),
                    ElevationM = ParsingUtilities.ParseNullableFloat(element.Element("elevation_m")?.Value),
                    Forecast = ParseForecasts(element.Elements("forecast"))
                };

                tafs.Add(taf);
            }

            return tafs;
        }

        private static List<TafForecast>? ParseForecasts(IEnumerable<XElement> elements)
        {
            var forecasts = new List<TafForecast>();

            foreach (var element in elements)
            {
                var forecast = new TafForecast
                {
                    FcstTimeFrom = element.Element("fcst_time_from")?.Value,
                    FcstTimeTo = element.Element("fcst_time_to")?.Value,
                    ChangeIndicator = element.Element("change_indicator")?.Value,
                    TimeBecoming = element.Element("time_becoming")?.Value,
                    Probability = ParsingUtilities.ParseNullableInt(element.Element("probability")?.Value),
                    WindDirDegrees = element.Element("wind_dir_degrees")?.Value,
                    WindSpeedKt = ParsingUtilities.ParseNullableInt(element.Element("wind_speed_kt")?.Value),
                    WindGustKt = ParsingUtilities.ParseNullableInt(element.Element("wind_gust_kt")?.Value),
                    WindShearHgtFtAgl = ParsingUtilities.ParseNullableShort(element.Element("wind_shear_hgt_ft_agl")?.Value),
                    WindShearDirDegrees = ParsingUtilities.ParseNullableShort(element.Element("wind_shear_dir_degrees")?.Value),
                    WindShearSpeedKt = ParsingUtilities.ParseNullableInt(element.Element("wind_shear_speed_kt")?.Value),
                    VisibilityStatuteMi = element.Element("visibility_statute_mi")?.Value,
                    AltimInHg = ParsingUtilities.ParseNullableFloat(element.Element("altim_in_hg")?.Value),
                    VertVisFt = ParsingUtilities.ParseNullableShort(element.Element("vert_vis_ft")?.Value),
                    WxString = element.Element("wx_string")?.Value,
                    NotDecoded = element.Element("not_decoded")?.Value,
                    SkyConditions = ParseSkyConditions(element.Elements("sky_condition")),
                    TurbulenceConditions = ParseTurbulenceConditions(element.Elements("turbulence_condition")),
                    IcingConditions = ParseIcingConditions(element.Elements("icing_condition")),
                    Temperature = ParseTemperatures(element.Elements("temperature"))
                };

                forecasts.Add(forecast);
            }

            return forecasts.Any() ? forecasts : null;
        }

        private static List<TafSkyCondition> ParseSkyConditions(IEnumerable<XElement> elements)
        {
            return elements.Select(element => new TafSkyCondition
            {
                SkyCover = element.Attribute("sky_cover")?.Value ?? string.Empty,
                CloudBaseFtAgl = ParsingUtilities.ParseNullableInt(element.Attribute("cloud_base_ft_agl")?.Value),
                CloudType = element.Attribute("cloud_type")?.Value
            }).ToList();
        }

        private static List<TafTurbulenceCondition> ParseTurbulenceConditions(IEnumerable<XElement> elements)
        {
            return elements.Select(element => new TafTurbulenceCondition
            {
                TurbulenceIntensity = element.Attribute("turbulence_intensity")?.Value,
                TurbulenceMinAltFtAgl = ParsingUtilities.ParseNullableInt(element.Attribute("turbulence_min_alt_ft_agl")?.Value),
                TurbulenceMaxAltFtAgl = ParsingUtilities.ParseNullableInt(element.Attribute("turbulence_max_alt_ft_agl")?.Value)
            }).ToList();
        }

        private static List<TafIcingCondition> ParseIcingConditions(IEnumerable<XElement> elements)
        {
            return elements.Select(element => new TafIcingCondition
            {
                IcingIntensity = element.Attribute("icing_intensity")?.Value,
                IcingMinAltFtAgl = ParsingUtilities.ParseNullableInt(element.Attribute("icing_min_alt_ft_agl")?.Value),
                IcingMaxAltFtAgl = ParsingUtilities.ParseNullableInt(element.Attribute("icing_max_alt_ft_agl")?.Value)
            }).ToList();
        }

        private static List<TafTemperature> ParseTemperatures(IEnumerable<XElement> elements)
        {
            return elements.Select(element => new TafTemperature
            {
                ValidTime = element.Element("valid_time")?.Value,
                SfcTempC = ParsingUtilities.ParseNullableFloat(element.Element("sfc_temp_c")?.Value),
                MaxTempC = element.Element("max_temp_c")?.Value,
                MinTempC = element.Element("min_temp_c")?.Value
            }).ToList();
        }

        private static void UpdateTafFields(Taf existing, Taf taf)
        {
            existing.RawText = taf.RawText;
            existing.IssueTime = taf.IssueTime;
            existing.BulletinTime = taf.BulletinTime;
            existing.ValidTimeFrom = taf.ValidTimeFrom;
            existing.ValidTimeTo = taf.ValidTimeTo;
            existing.Remarks = taf.Remarks;
            existing.Latitude = taf.Latitude;
            existing.Longitude = taf.Longitude;
            existing.ElevationM = taf.ElevationM;
            existing.Forecast = taf.Forecast;
        }
    }
}
