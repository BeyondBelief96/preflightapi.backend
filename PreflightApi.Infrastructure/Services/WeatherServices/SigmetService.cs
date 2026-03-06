using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Mappers;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Services.WeatherServices;

public class SigmetService : ISigmetService
{
    private readonly PreflightApiDbContext _context;
    private readonly ILogger<SigmetService> _logger;
    private readonly GeometryFactory _geometryFactory;

    public SigmetService(
        PreflightApiDbContext context,
        ILogger<SigmetService> logger)
    {
        _context = context;
        _logger = logger;
        _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
    }

    public async Task<PaginatedResponse<SigmetDto>> GetAllSigmets(string? cursor, int limit, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Retrieving SIGMETs, cursor: {Cursor}, limit: {Limit}", cursor, limit);

            var query = _context.Sigmets.AsNoTracking();
            return await query.ToPaginatedAsync(s => s.Id, SigmetMapper.ToDto, cursor, limit, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all SIGMETs");
            throw;
        }
    }

    public async Task<PaginatedResponse<SigmetDto>> GetSigmetsByHazardType(SigmetHazardType hazardType, string? cursor, int limit, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Retrieving SIGMETs for hazard type {HazardType}, cursor: {Cursor}, limit: {Limit}",
                hazardType, cursor, limit);

            var hazardTypeString = ConvertHazardTypeToString(hazardType);
            // Hazard is stored as a JSON column via value converter, so property access
            // can't be translated to SQL. Load all and filter in memory (small dataset).
            var allSigmets = await _context.Sigmets.AsNoTracking()
                .OrderBy(s => s.Id)
                .ToListAsync(ct);

            var filtered = allSigmets
                .Where(a => a.Hazard != null && a.Hazard.Type == hazardTypeString)
                .ToList();

            var decoded = CursorHelper.DecodeIntWithDirection(cursor);
            var isBackward = decoded?.Direction == CursorDirection.Backward;
            var hasCursor = decoded != null;

            if (decoded != null)
            {
                filtered = isBackward
                    ? filtered.Where(s => s.Id < decoded.Value).ToList()
                    : filtered.Where(s => s.Id > decoded.Value).ToList();
            }

            if (isBackward)
                filtered.Reverse();

            var hasExtra = filtered.Count > limit;
            var page = filtered.Take(limit).ToList();

            if (isBackward)
                page.Reverse();

            bool hasMore, hasPrevious;
            string? nextCursor, previousCursor;

            if (isBackward)
            {
                hasPrevious = hasExtra;
                hasMore = page.Count > 0;
                previousCursor = hasPrevious && page.Count > 0
                    ? CursorHelper.EncodePrevious(page[0].Id)
                    : null;
                nextCursor = page.Count > 0
                    ? CursorHelper.EncodeNext(page[^1].Id)
                    : null;
            }
            else
            {
                hasMore = hasExtra;
                hasPrevious = hasCursor;
                nextCursor = hasMore && page.Count > 0
                    ? CursorHelper.EncodeNext(page[^1].Id)
                    : null;
                previousCursor = hasCursor && page.Count > 0
                    ? CursorHelper.EncodePrevious(page[0].Id)
                    : null;
            }

            return new PaginatedResponse<SigmetDto>
            {
                Data = page.Select(SigmetMapper.ToDto),
                Pagination = new PaginationMetadata
                {
                    NextCursor = nextCursor,
                    HasMore = hasMore,
                    PreviousCursor = previousCursor,
                    HasPrevious = hasPrevious,
                    Limit = limit
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SIGMETs for hazard type {HazardType}", hazardType);
            throw;
        }
    }

    public async Task<PaginatedResponse<SigmetDto>> SearchAffecting(
        double latitude,
        double longitude,
        string? cursor,
        int limit,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Searching SIGMETs affecting ({Lat}, {Lon}), cursor: {Cursor}, limit: {Limit}",
                latitude, longitude, cursor, limit);

            var point = _geometryFactory.CreatePoint(new Coordinate(longitude, latitude));

            var query = _context.Sigmets
                .AsNoTracking()
                .Where(s => s.Boundary != null && s.Boundary.Intersects(point));

            return await query.ToPaginatedAsync(s => s.Id, SigmetMapper.ToDto, cursor, limit, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching SIGMETs affecting ({Lat}, {Lon})", latitude, longitude);
            throw;
        }
    }

    public async Task<PaginatedResponse<SigmetDto>> SearchByArea(
        double minLat,
        double maxLat,
        double minLon,
        double maxLon,
        string? cursor,
        int limit,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Searching SIGMETs in area ({MinLat}, {MinLon}) to ({MaxLat}, {MaxLon}), cursor: {Cursor}, limit: {Limit}",
                minLat, minLon, maxLat, maxLon, cursor, limit);

            var coordinates = new[]
            {
                new Coordinate(minLon, minLat),
                new Coordinate(maxLon, minLat),
                new Coordinate(maxLon, maxLat),
                new Coordinate(minLon, maxLat),
                new Coordinate(minLon, minLat)
            };
            var boundingBox = _geometryFactory.CreatePolygon(coordinates);

            var query = _context.Sigmets
                .AsNoTracking()
                .Where(s => s.Boundary != null && s.Boundary.Intersects(boundingBox));

            return await query.ToPaginatedAsync(s => s.Id, SigmetMapper.ToDto, cursor, limit, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching SIGMETs in bounding box");
            throw;
        }
    }

    private static string ConvertHazardTypeToString(SigmetHazardType hazardType)
    {
        return hazardType switch
        {
            SigmetHazardType.CONVECTIVE => "CONVECTIVE",
            SigmetHazardType.ICE => "ICE",
            SigmetHazardType.TURB => "TURB",
            SigmetHazardType.IFR => "IFR",
            SigmetHazardType.MTN_OBSCN => "MTN OBSCN",
            _ => throw new ArgumentOutOfRangeException(nameof(hazardType))
        };
    }
}
