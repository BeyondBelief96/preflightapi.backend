using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Mappers;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Services.AirportInformationServices;

public class NavaidService : INavaidService
{
    private readonly PreflightApiDbContext _context;
    private readonly ILogger<NavaidService> _logger;
    private readonly GeometryFactory _geometryFactory;

    private const int NauticalMileToMeters = 1852;

    public NavaidService(
        PreflightApiDbContext context,
        ILogger<NavaidService> logger)
    {
        _context = context;
        _logger = logger;
        _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
    }

    public async Task<PaginatedResponse<NavaidDto>> GetNavaids(
        string? search,
        string? navType,
        string? stateCode,
        string? cursor,
        int limit,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Getting navaids with search: {Search}, type: {NavType}, state: {State}, cursor: {Cursor}, limit: {Limit}",
                search, navType, stateCode, cursor, limit);

            var query = _context.Navaids.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(navType))
            {
                var upperType = navType.ToUpperInvariant();
                query = query.Where(n => n.NavType == upperType);
            }

            if (!string.IsNullOrWhiteSpace(stateCode))
            {
                var upperState = stateCode.ToUpperInvariant();
                query = query.Where(n => n.StateCode == upperState);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var startsWithPattern = $"{search}%";
                var containsPattern = $"%{search}%";
                query = query.Where(n =>
                    EF.Functions.ILike(n.NavId, startsWithPattern) ||
                    EF.Functions.ILike(n.Name, containsPattern) ||
                    EF.Functions.ILike(n.City, containsPattern));
            }

            return await query.ToPaginatedAsync(n => n.Id, n => NavaidMapper.ToDto(n, _logger), cursor, limit, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting navaids");
            throw;
        }
    }

    public async Task<IEnumerable<NavaidDto>> GetNavaidsByIdentifier(string navId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting navaids by identifier: {NavId}", navId);

            var upperNavId = navId.ToUpperInvariant();
            var navaids = await _context.Navaids
                .AsNoTracking()
                .Where(n => n.NavId == upperNavId)
                .OrderBy(n => n.NavType)
                .ToListAsync(ct);

            return navaids.Select(n => NavaidMapper.ToDto(n, _logger));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting navaids by identifier: {NavId}", navId);
            throw;
        }
    }

    public async Task<IEnumerable<NavaidDto>> GetNavaidsByIdentifiers(IEnumerable<string> navIds, CancellationToken ct = default)
    {
        try
        {
            var upperIds = navIds.Select(id => id.ToUpperInvariant()).Distinct().ToList();

            if (upperIds.Count == 0)
                return [];

            _logger.LogInformation("Getting {Count} navaids by identifiers", upperIds.Count);

            var navaids = await _context.Navaids
                .AsNoTracking()
                .Where(n => upperIds.Contains(n.NavId))
                .OrderBy(n => n.NavId)
                .ThenBy(n => n.NavType)
                .ToListAsync(ct);

            return navaids.Select(n => NavaidMapper.ToDto(n, _logger));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting navaids by identifiers");
            throw;
        }
    }

    public async Task<PaginatedResponse<NavaidDto>> SearchNearby(
        decimal latitude,
        decimal longitude,
        double radiusNm,
        string? navType,
        string? cursor,
        int limit,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Searching navaids near ({Lat}, {Lon}) within {Radius} NM, type: {NavType}, cursor: {Cursor}, limit: {Limit}",
                latitude, longitude, radiusNm, navType, cursor, limit);

            var radiusMeters = radiusNm * NauticalMileToMeters;
            var point = _geometryFactory.CreatePoint(new Coordinate((double)longitude, (double)latitude));

            var query = _context.Navaids
                .AsNoTracking()
                .Where(n => n.Location != null && n.Location.IsWithinDistance(point, radiusMeters));

            if (!string.IsNullOrWhiteSpace(navType))
            {
                var upperType = navType.ToUpperInvariant();
                query = query.Where(n => n.NavType == upperType);
            }

            return await query.ToPaginatedAsync(n => n.Id, n => NavaidMapper.ToDto(n, _logger), cursor, limit, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching navaids near ({Lat}, {Lon})", latitude, longitude);
            throw;
        }
    }
}
