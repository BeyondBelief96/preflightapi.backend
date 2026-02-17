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

namespace PreflightApi.Infrastructure.Services.WeatherServices;

public class PirepService : IPirepService
{
    private readonly PreflightApiDbContext _context;
    private readonly ILogger<PirepService> _logger;
    private readonly GeometryFactory _geometryFactory;

    public PirepService(
        PreflightApiDbContext context,
        ILogger<PirepService> logger)
    {
        _context = context;
        _logger = logger;
        _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
    }

    public async Task<PaginatedResponse<PirepDto>> GetAllPireps(string? cursor, int limit, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Retrieving PIREPs, cursor: {Cursor}, limit: {Limit}", cursor, limit);

            var query = _context.Pireps.AsNoTracking();
            return await query.ToPaginatedAsync(p => p.Id, PirepMapper.ToDto, cursor, limit, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all PIREPs");
            throw;
        }
    }

    public async Task<PaginatedResponse<PirepDto>> SearchNearby(
        decimal latitude,
        decimal longitude,
        double radiusNm,
        string? cursor,
        int limit,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Searching PIREPs near ({Lat}, {Lon}) within {Radius} NM, cursor: {Cursor}, limit: {Limit}",
                latitude, longitude, radiusNm, cursor, limit);

            var radiusMeters = radiusNm * 1852;
            var point = _geometryFactory.CreatePoint(new Coordinate((double)longitude, (double)latitude));

            var query = _context.Pireps
                .AsNoTracking()
                .Where(p => p.Location != null && p.Location.IsWithinDistance(point, radiusMeters));

            return await query.ToPaginatedAsync(p => p.Id, PirepMapper.ToDto, cursor, limit, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching PIREPs near ({Lat}, {Lon})", latitude, longitude);
            throw;
        }
    }
}
