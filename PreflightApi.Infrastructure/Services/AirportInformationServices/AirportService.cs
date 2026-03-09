using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Mappers;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Services
{
    public class AirportService : IAirportService
    {
        private readonly PreflightApiDbContext _context;
        private readonly ILogger<AirportService> _logger;
        private readonly GeometryFactory _geometryFactory;
        private readonly int NAUTICAL_MILE_TO_METERS = 1852;

        public AirportService(
            PreflightApiDbContext context,
            ILogger<AirportService> logger)
        {
            _context = context;
            _logger = logger;
            _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        }

        public async Task<PaginatedResponse<AirportDto>> GetAirports(string? search = null, string[]? stateCodes = null, string? cursor = null, int limit = 100, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Getting airports with search: {Search}, states: {States}, cursor: {Cursor}, limit: {Limit}",
                    search, stateCodes != null ? string.Join(",", stateCodes) : null, cursor, limit);

                var query = _context.Airports.AsQueryable();

                if (stateCodes is { Length: > 0 })
                {
                    var upperStateCodes = stateCodes.Select(s => s.ToUpperInvariant()).ToArray();
                    query = query.Where(a => a.StateCode != null && upperStateCodes.Contains(a.StateCode));
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var pattern = $"%{search}%";
                    var startsWithPattern = $"{search}%";
                    query = query.Where(a =>
                        (a.IcaoId != null && EF.Functions.ILike(a.IcaoId, startsWithPattern)) ||
                        (a.ArptId != null && EF.Functions.ILike(a.ArptId, startsWithPattern)) ||
                        (a.ArptName != null && EF.Functions.ILike(a.ArptName, pattern)) ||
                        (a.City != null && EF.Functions.ILike(a.City, pattern)));
                }

                return await query.ToPaginatedAsync(a => a.SiteNo, a => AirportMapper.ToDto(a, _logger), cursor, limit, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting airports with search: {Search}, states: {States}", search,
                    stateCodes != null ? string.Join(",", stateCodes) : null);
                throw;
            }
        }

        public async Task<AirportDto> GetAirportByIcaoCodeOrIdent(string icaoCodeOrIdent, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Getting airport by ICAO code or ident: {IcaoCodeOrIdent}", icaoCodeOrIdent);

                var candidates = AirportIdentifierResolver.GetCandidateIdentifiers(icaoCodeOrIdent);
                var airport = await _context.Airports
                    .FirstOrDefaultAsync(a =>
                        (a.IcaoId != null && candidates.Contains(a.IcaoId)) ||
                        (a.ArptId != null && candidates.Contains(a.ArptId)), ct);

                if (airport == null)
                {
                    throw new AirportNotFoundException(icaoCodeOrIdent);
                }

                return AirportMapper.ToDto(airport, _logger);
            }
            catch (Exception ex) when (ex is not AirportNotFoundException)
            {
                _logger.LogError(ex, "Error getting airport by ICAO code or ident: {IcaoCodeOrIdent}", icaoCodeOrIdent);
                throw;
            }
        }

        public async Task<IEnumerable<AirportDto>> GetAirportsByIcaoCodesOrIdents(string[] codesOrIdents, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Getting airports by ICAO codes or idents: {CodesOrIdents}",
                    string.Join(", ", codesOrIdents));

                var expandedCodes = AirportIdentifierResolver.ExpandCandidates(codesOrIdents);
                var airports = await _context.Airports
                    .Where(a =>
                        (a.IcaoId != null && expandedCodes.Contains(a.IcaoId)) ||
                        (a.ArptId != null && expandedCodes.Contains(a.ArptId)))
                    .ToListAsync(ct);

                return airports.Select(a => AirportMapper.ToDto(a, _logger));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting airports by ICAO codes or idents: {CodesOrIdents}",
                    string.Join(", ", codesOrIdents));
                throw;
            }
        }

        public async Task<PaginatedResponse<AirportDto>> SearchNearby(
            double latitude,
            double longitude,
            double radiusNm,
            string? cursor = null,
            int limit = 100,
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation(
                    "Searching airports near ({Lat}, {Lon}) within {Radius} NM, cursor: {Cursor}, limit: {Limit}",
                    latitude, longitude, radiusNm, cursor, limit);

                var radiusMeters = radiusNm * NAUTICAL_MILE_TO_METERS;
                var point = _geometryFactory.CreatePoint(new Coordinate(longitude, latitude));

                var query = _context.Airports
                    .FromSqlInterpolated(
                        $"SELECT * FROM airports WHERE location IS NOT NULL AND ST_DWithin(location::geography, {point}::geography, {radiusMeters})")
                    .AsNoTracking();

                return await query.ToPaginatedAsync(a => a.SiteNo, a => AirportMapper.ToDto(a, _logger), cursor, limit, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching airports near ({Lat}, {Lon})", latitude, longitude);
                throw;
            }
        }
    }
}
