using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using PreflightApi.Domain.Enums;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Mappers;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Services.AirportInformationServices;

public class RunwayService : IRunwayService
{
    private readonly PreflightApiDbContext _context;
    private readonly ILogger<RunwayService> _logger;
    private readonly GeometryFactory _geometryFactory;

    private const int NauticalMileToMeters = 1852;

    public RunwayService(
        PreflightApiDbContext context,
        ILogger<RunwayService> logger)
    {
        _context = context;
        _logger = logger;
        _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
    }

    public async Task<IEnumerable<RunwayDto>> GetRunwaysByAirportAsync(string icaoCodeOrIdent, bool includeGeometry = false, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting runways for airport: {IcaoCodeOrIdent}", icaoCodeOrIdent);

            var candidates = AirportIdentifierResolver.GetCandidateIdentifiers(icaoCodeOrIdent);
            var airport = await _context.Airports
                .FirstOrDefaultAsync(a =>
                    (a.IcaoId != null && candidates.Contains(a.IcaoId)) ||
                    (a.ArptId != null && candidates.Contains(a.ArptId)), ct);

            if (airport == null)
            {
                throw new AirportNotFoundException(icaoCodeOrIdent);
            }

            var runways = await _context.Runways
                .Include(r => r.RunwayEnds)
                .Where(r => r.SiteNo == airport.SiteNo)
                .OrderBy(r => r.RunwayId)
                .ToListAsync(ct);

            return runways.Select(r => RunwayMapper.ToDto(r, airport, _logger, includeGeometry));
        }
        catch (Exception ex) when (ex is not AirportNotFoundException)
        {
            _logger.LogError(ex, "Error getting runways for airport: {IcaoCodeOrIdent}", icaoCodeOrIdent);
            throw;
        }
    }

    public async Task<PaginatedResponse<RunwayDto>> GetRunways(
        string? search,
        RunwaySurfaceType? surfaceType,
        int? minLength,
        string? state,
        bool? lighted,
        string? cursor,
        int limit,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Getting runways with search: {Search}, surfaceType: {SurfaceType}, minLength: {MinLength}, state: {State}, lighted: {Lighted}",
                search, surfaceType, minLength, state, lighted);

            // Step 1: Build airport query to find matching SiteNos
            var airportQuery = _context.Airports.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var startsWithPattern = $"{search}%";
                var containsPattern = $"%{search}%";
                airportQuery = airportQuery.Where(a =>
                    EF.Functions.ILike(a.ArptId!, startsWithPattern) ||
                    EF.Functions.ILike(a.IcaoId!, startsWithPattern) ||
                    EF.Functions.ILike(a.ArptName!, containsPattern) ||
                    EF.Functions.ILike(a.City!, containsPattern));
            }

            if (!string.IsNullOrWhiteSpace(state))
            {
                var upperState = state.ToUpperInvariant();
                airportQuery = airportQuery.Where(a => a.StateCode == upperState);
            }

            var airports = await airportQuery
                .ToDictionaryAsync(a => a.SiteNo, cancellationToken: ct);

            if (airports.Count == 0)
                return PaginatedResponse<RunwayDto>.Empty(limit);

            // Step 2: Query runways for matching airports with runway-level filters
            var siteNos = airports.Keys.ToList();
            var runwayQuery = _context.Runways
                .AsNoTracking()
                .Include(r => r.RunwayEnds)
                .Where(r => siteNos.Contains(r.SiteNo));

            runwayQuery = ApplyRunwayFilters(runwayQuery, surfaceType, minLength, lighted);

            return await runwayQuery.ToPaginatedAsync(
                r => r.Id,
                r => RunwayMapper.ToDto(r, airports[r.SiteNo], _logger),
                cursor,
                limit,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting runways");
            throw;
        }
    }

    public async Task<PaginatedResponse<RunwayDto>> SearchNearby(
        double latitude,
        double longitude,
        double radiusNm,
        int? minLength,
        RunwaySurfaceType? surfaceType,
        bool includeGeometry,
        string? cursor,
        int limit,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Searching runways near ({Lat}, {Lon}) within {Radius} NM",
                latitude, longitude, radiusNm);

            var radiusMeters = radiusNm * NauticalMileToMeters;
            var point = _geometryFactory.CreatePoint(new Coordinate(longitude, latitude));

            // Step 1: Find airports within radius
            var airports = await _context.Airports
                .AsNoTracking()
                .Where(a => a.Location != null && a.Location.IsWithinDistance(point, radiusMeters))
                .ToDictionaryAsync(a => a.SiteNo, ct);

            if (airports.Count == 0)
                return PaginatedResponse<RunwayDto>.Empty(limit);

            // Step 2: Query runways for those airports with additional filters
            var siteNos = airports.Keys.ToList();
            var runwayQuery = _context.Runways
                .AsNoTracking()
                .Include(r => r.RunwayEnds)
                .Where(r => siteNos.Contains(r.SiteNo));

            runwayQuery = ApplyRunwayFilters(runwayQuery, surfaceType, minLength, null);

            return await runwayQuery.ToPaginatedAsync(
                r => r.Id,
                r => RunwayMapper.ToDto(r, airports[r.SiteNo], _logger, includeGeometry),
                cursor,
                limit,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching runways near ({Lat}, {Lon})", latitude, longitude);
            throw;
        }
    }

    private static IQueryable<Domain.Entities.Runway> ApplyRunwayFilters(
        IQueryable<Domain.Entities.Runway> query,
        RunwaySurfaceType? surfaceType,
        int? minLength,
        bool? lighted)
    {
        if (surfaceType.HasValue)
        {
            var dbCode = RunwayMapper.ToDbCode(surfaceType.Value);
            if (dbCode != null)
            {
                query = query.Where(r => r.SurfaceTypeCode != null &&
                    (r.SurfaceTypeCode == dbCode ||
                     r.SurfaceTypeCode.StartsWith(dbCode + "-") ||
                     r.SurfaceTypeCode.StartsWith(dbCode + "/") ||
                     r.SurfaceTypeCode.EndsWith("-" + dbCode) ||
                     r.SurfaceTypeCode.EndsWith("/" + dbCode)));
            }
        }

        if (minLength.HasValue)
        {
            query = query.Where(r => r.Length >= minLength.Value);
        }

        if (lighted.HasValue)
        {
            if (lighted.Value)
                query = query.Where(r => r.EdgeLightIntensity != null && r.EdgeLightIntensity != "NONE");
            else
                query = query.Where(r => r.EdgeLightIntensity == null || r.EdgeLightIntensity == "NONE");
        }

        return query;
    }
}
