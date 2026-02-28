using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services.CronJobServices.ArcGisServices.Models;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Services.CronJobServices.ArcGisServices;

public class RunwayGeometryCronService : ArcGisBaseService<Runway>, IRunwayGeometryCronService
{
    protected override string BaseUrl =>
        "https://services6.arcgis.com/ssFJjBXIUyZDrSYZ/arcgis/rest/services/Runways/FeatureServer/0/query";

    private const string AirportLookupUrl =
        "https://services6.arcgis.com/ssFJjBXIUyZDrSYZ/arcgis/rest/services/US_Airport/FeatureServer/0/query";

    public RunwayGeometryCronService(
        ILogger<RunwayGeometryCronService> logger,
        IHttpClientFactory httpClientFactory,
        PreflightApiDbContext dbContext)
        : base(logger, httpClientFactory, dbContext)
    {
    }

    public async Task UpdateRunwayGeometriesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting runway geometry sync from ArcGIS");

        // Step 1: Fetch GLOBAL_ID→IDENT lookup from US_Airport ArcGIS (no geometry, ~19K records)
        var guidToIdent = await FetchAirportLookupAsync(cancellationToken);
        _logger.LogInformation("Fetched {Count} airport GUID→IDENT mappings from ArcGIS", guidToIdent.Count);

        // Step 2: Build ArptId→SiteNo lookup from local Airport table
        var arptIdToSiteNo = await _dbContext.Airports
            .AsNoTracking()
            .Where(a => a.ArptId != null)
            .Select(a => new { a.ArptId, a.SiteNo })
            .ToDictionaryAsync(a => a.ArptId!, a => a.SiteNo, StringComparer.OrdinalIgnoreCase, cancellationToken);
        _logger.LogInformation("Loaded {Count} local airport ArptId→SiteNo mappings", arptIdToSiteNo.Count);

        // Step 3: Build "SiteNo|RunwayId"→Runway lookup from local Runway table
        var runways = await _dbContext.Runways
            .Where(r => r.SiteNo != null)
            .ToDictionaryAsync(r => $"{r.SiteNo}|{r.RunwayId}", cancellationToken);
        _logger.LogInformation("Loaded {Count} local runways", runways.Count);

        // Step 4: Fetch runway features with geometry from ArcGIS
        var parameters = new Dictionary<string, string>
        {
            ["where"] = "1=1",
            ["outFields"] = "OBJECTID,AIRPORT_ID,DESIGNATOR",
            ["returnGeometry"] = "true"
        };

        var response = await QueryFeatures<RunwayArcGisModel>(parameters, cancellationToken);
        _logger.LogInformation("Fetched {Count} runway features from ArcGIS", response.Features.Count);

        // Step 5: Match ArcGIS runways to local runways and update geometry
        var matchedCount = 0;
        var unmatchedCount = 0;
        var skippedCount = 0;

        foreach (var feature in response.Features)
        {
            var airportGuid = feature.Attributes.AirportId;
            var designator = feature.Attributes.Designator;

            if (string.IsNullOrEmpty(airportGuid) || string.IsNullOrEmpty(designator))
            {
                skippedCount++;
                continue;
            }

            // Chain: AIRPORT_ID (GUID) → IDENT → SiteNo → Runway
            if (!guidToIdent.TryGetValue(airportGuid, out var ident))
            {
                unmatchedCount++;
                continue;
            }

            if (!arptIdToSiteNo.TryGetValue(ident, out var siteNo))
            {
                unmatchedCount++;
                continue;
            }

            var key = $"{siteNo}|{designator}";
            if (!runways.TryGetValue(key, out var runway))
            {
                unmatchedCount++;
                continue;
            }

            var geometry = CreatePolygonFromRings(feature.Geometry?.Rings ?? Array.Empty<List<double[]>>());
            if (geometry != null)
            {
                runway.Geometry = geometry;
                matchedCount++;
            }
            else
            {
                skippedCount++;
            }
        }

        var changesCount = await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Runway geometry sync completed: {Matched} matched, {Unmatched} unmatched, {Skipped} skipped, {Changes} database changes",
            matchedCount, unmatchedCount, skippedCount, changesCount);
    }

    private async Task<Dictionary<string, string>> FetchAirportLookupAsync(CancellationToken cancellationToken)
    {
        var allFeatures = new List<ArcGisFeature<AirportLookupArcGisModel>>();
        var offset = 0;
        bool hasMoreRecords;

        var httpClient = _httpClientFactory.CreateClient("ArcGis");

        do
        {
            var queryParams = new Dictionary<string, string>
            {
                ["where"] = "1=1",
                ["outFields"] = "GLOBAL_ID,IDENT",
                ["returnGeometry"] = "false",
                ["f"] = "json",
                ["outSR"] = "4326",
                ["resultRecordCount"] = PageSize.ToString(),
                ["resultOffset"] = offset.ToString()
            };

            var url = WebUtilities.AddQueryString(AirportLookupUrl, queryParams);
            _logger.LogDebug("Fetching airport lookup from offset {Offset}", offset);

            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<HttpIOException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(
                    MaxRetryAttempts,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(RetryBaseDelaySeconds, retryAttempt)));

            var response = await retryPolicy.ExecuteAsync(async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                return await httpClient.SendAsync(request, cancellationToken);
            });

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ArcGisResponse<AirportLookupArcGisModel>>(content, _jsonOptions);

            if (result?.Features == null)
                throw new Exception("Invalid response from ArcGIS airport lookup service");

            allFeatures.AddRange(result.Features);
            hasMoreRecords = result.ExceededTransferLimit;
            offset += result.Features.Count;

            if (hasMoreRecords)
            {
                _logger.LogDebug("Fetched {Count} airport lookup features, total so far: {Total}. Fetching more...",
                    result.Features.Count, allFeatures.Count);
            }
        } while (hasMoreRecords);

        // Build GUID→IDENT dictionary
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var feature in allFeatures)
        {
            var globalId = feature.Attributes.GlobalId;
            var ident = feature.Attributes.Ident;
            if (!string.IsNullOrEmpty(globalId) && !string.IsNullOrEmpty(ident))
            {
                lookup[globalId] = ident;
            }
        }

        return lookup;
    }

    protected override Task<Runway?> FindExistingEntity(object id, CancellationToken cancellationToken)
    {
        // Not used — matching is done in bulk via dictionary lookup
        throw new NotSupportedException();
    }

    protected override Runway CreateNewEntity(object id)
    {
        // Not used — this service only updates existing runways
        throw new NotSupportedException();
    }

    protected override void MapFieldsToEntity(Runway entity, object attributes)
    {
        // Not used — geometry is set directly in UpdateRunwayGeometriesAsync
        throw new NotSupportedException();
    }
}
