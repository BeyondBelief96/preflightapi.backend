using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Mappers;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Services.AirportInformationServices;

public class WeatherStationService : IWeatherStationService
{
    private readonly PreflightApiDbContext _context;
    private readonly ILogger<WeatherStationService> _logger;

    public WeatherStationService(
        PreflightApiDbContext context,
        ILogger<WeatherStationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PaginatedResponse<WeatherStationDto>> GetAllAsync(string? search, string? cursor, int limit)
    {
        _logger.LogInformation("Getting weather stations, search: {Search}, cursor: {Cursor}, limit: {Limit}",
            search, cursor, limit);

        var query = _context.WeatherStations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchUpper = search.ToUpperInvariant();
            query = query.Where(w => w.AsosAwosId.Contains(searchUpper) ||
                                     (w.City != null && w.City.Contains(searchUpper)));
        }

        var decodedCursor = CursorHelper.DecodeString(cursor);
        if (decodedCursor != null && Guid.TryParse(decodedCursor, out var cursorGuid))
        {
            query = query.Where(w => w.Id.CompareTo(cursorGuid) > 0);
        }

        query = query.OrderBy(w => w.Id);

        var items = await query.Take(limit + 1).ToListAsync();
        var hasMore = items.Count > limit;
        if (hasMore)
        {
            items = items.Take(limit).ToList();
        }

        var data = items.Select(WeatherStationMapper.ToDto);
        var nextCursor = hasMore && items.Count > 0
            ? CursorHelper.Encode(items[^1].Id.ToString())
            : null;

        return new PaginatedResponse<WeatherStationDto>
        {
            Data = data,
            Pagination = new PaginationMetadata
            {
                Limit = limit,
                NextCursor = nextCursor,
                HasMore = hasMore
            }
        };
    }

    public async Task<IEnumerable<WeatherStationDto>> GetByIdentifierAsync(string identifier)
    {
        _logger.LogInformation("Getting weather stations by identifier: {Identifier}", identifier);

        var upperIdentifier = identifier.ToUpperInvariant();
        var stations = await _context.WeatherStations
            .Where(w => w.AsosAwosId == upperIdentifier)
            .ToListAsync();

        if (!stations.Any())
        {
            throw new WeatherStationNotFoundException(identifier);
        }

        return stations.Select(WeatherStationMapper.ToDto);
    }

    public async Task<PaginatedResponse<WeatherStationDto>> GetByTypeAsync(string sensorType, string? cursor, int limit)
    {
        _logger.LogInformation("Getting weather stations by type: {SensorType}", sensorType);

        var upperType = sensorType.ToUpperInvariant();
        var query = _context.WeatherStations
            .Where(w => w.AsosAwosType == upperType);

        var decodedCursor = CursorHelper.DecodeString(cursor);
        if (decodedCursor != null && Guid.TryParse(decodedCursor, out var cursorGuid))
        {
            query = query.Where(w => w.Id.CompareTo(cursorGuid) > 0);
        }

        query = query.OrderBy(w => w.Id);

        var items = await query.Take(limit + 1).ToListAsync();
        var hasMore = items.Count > limit;
        if (hasMore)
        {
            items = items.Take(limit).ToList();
        }

        var data = items.Select(WeatherStationMapper.ToDto);
        var nextCursor = hasMore && items.Count > 0
            ? CursorHelper.Encode(items[^1].Id.ToString())
            : null;

        return new PaginatedResponse<WeatherStationDto>
        {
            Data = data,
            Pagination = new PaginationMetadata
            {
                Limit = limit,
                NextCursor = nextCursor,
                HasMore = hasMore
            }
        };
    }

    public async Task<PaginatedResponse<WeatherStationDto>> GetByStateAsync(string stateCode, string? cursor, int limit)
    {
        _logger.LogInformation("Getting weather stations by state: {StateCode}", stateCode);

        var upperState = stateCode.ToUpperInvariant();
        var query = _context.WeatherStations
            .Where(w => w.StateCode == upperState);

        var decodedCursor = CursorHelper.DecodeString(cursor);
        if (decodedCursor != null && Guid.TryParse(decodedCursor, out var cursorGuid))
        {
            query = query.Where(w => w.Id.CompareTo(cursorGuid) > 0);
        }

        query = query.OrderBy(w => w.Id);

        var items = await query.Take(limit + 1).ToListAsync();
        var hasMore = items.Count > limit;
        if (hasMore)
        {
            items = items.Take(limit).ToList();
        }

        var data = items.Select(WeatherStationMapper.ToDto);
        var nextCursor = hasMore && items.Count > 0
            ? CursorHelper.Encode(items[^1].Id.ToString())
            : null;

        return new PaginatedResponse<WeatherStationDto>
        {
            Data = data,
            Pagination = new PaginationMetadata
            {
                Limit = limit,
                NextCursor = nextCursor,
                HasMore = hasMore
            }
        };
    }

    public async Task<IEnumerable<WeatherStationDto>> GetByAirportAsync(string airportIdentifier)
    {
        _logger.LogInformation("Getting weather stations by airport: {AirportIdentifier}", airportIdentifier);

        var upperIdentifier = airportIdentifier.ToUpperInvariant();

        // Look up the airport's SiteNo by ArptId or IcaoId
        var airport = await _context.Airports
            .Where(a => a.ArptId == upperIdentifier || a.IcaoId == upperIdentifier)
            .FirstOrDefaultAsync();

        if (airport == null)
        {
            throw new AirportNotFoundException(airportIdentifier);
        }

        var stations = await _context.WeatherStations
            .Where(w => w.SiteNo == airport.SiteNo)
            .ToListAsync();

        return stations.Select(WeatherStationMapper.ToDto);
    }
}
