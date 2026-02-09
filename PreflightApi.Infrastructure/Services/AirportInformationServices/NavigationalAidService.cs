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

public class NavigationalAidService : INavigationalAidService
{
    private readonly PreflightApiDbContext _context;
    private readonly ILogger<NavigationalAidService> _logger;

    public NavigationalAidService(
        PreflightApiDbContext context,
        ILogger<NavigationalAidService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PaginatedResponse<NavigationalAidDto>> GetAllAsync(string? search, string? cursor, int limit)
    {
        _logger.LogInformation("Getting navigational aids, search: {Search}, cursor: {Cursor}, limit: {Limit}",
            search, cursor, limit);

        var query = _context.NavigationalAids.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchUpper = search.ToUpperInvariant();
            query = query.Where(n => n.NavId.Contains(searchUpper) ||
                                     (n.Name != null && n.Name.Contains(searchUpper)));
        }

        var decodedCursor = CursorHelper.DecodeString(cursor);
        if (decodedCursor != null && Guid.TryParse(decodedCursor, out var cursorGuid))
        {
            query = query.Where(n => n.Id.CompareTo(cursorGuid) > 0);
        }

        query = query.OrderBy(n => n.Id);

        var items = await query.Take(limit + 1).ToListAsync();
        var hasMore = items.Count > limit;
        if (hasMore)
        {
            items = items.Take(limit).ToList();
        }

        var data = items.Select(NavigationalAidMapper.ToDto);
        var nextCursor = hasMore && items.Count > 0
            ? CursorHelper.Encode(items[^1].Id.ToString())
            : null;

        return new PaginatedResponse<NavigationalAidDto>
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

    public async Task<IEnumerable<NavigationalAidDto>> GetByIdentifierAsync(string identifier)
    {
        _logger.LogInformation("Getting navigational aids by identifier: {Identifier}", identifier);

        var upperIdentifier = identifier.ToUpperInvariant();
        var navAids = await _context.NavigationalAids
            .Where(n => n.NavId == upperIdentifier)
            .ToListAsync();

        if (!navAids.Any())
        {
            throw new NavigationalAidNotFoundException(identifier);
        }

        return navAids.Select(NavigationalAidMapper.ToDto);
    }

    public async Task<PaginatedResponse<NavigationalAidDto>> GetByTypeAsync(string navType, string? cursor, int limit)
    {
        _logger.LogInformation("Getting navigational aids by type: {NavType}", navType);

        var upperType = navType.ToUpperInvariant();
        var query = _context.NavigationalAids
            .Where(n => n.NavType == upperType);

        var decodedCursor = CursorHelper.DecodeString(cursor);
        if (decodedCursor != null && Guid.TryParse(decodedCursor, out var cursorGuid))
        {
            query = query.Where(n => n.Id.CompareTo(cursorGuid) > 0);
        }

        query = query.OrderBy(n => n.Id);

        var items = await query.Take(limit + 1).ToListAsync();
        var hasMore = items.Count > limit;
        if (hasMore)
        {
            items = items.Take(limit).ToList();
        }

        var data = items.Select(NavigationalAidMapper.ToDto);
        var nextCursor = hasMore && items.Count > 0
            ? CursorHelper.Encode(items[^1].Id.ToString())
            : null;

        return new PaginatedResponse<NavigationalAidDto>
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

    public async Task<PaginatedResponse<NavigationalAidDto>> GetByStateAsync(string stateCode, string? cursor, int limit)
    {
        _logger.LogInformation("Getting navigational aids by state: {StateCode}", stateCode);

        var upperState = stateCode.ToUpperInvariant();
        var query = _context.NavigationalAids
            .Where(n => n.StateCode == upperState);

        var decodedCursor = CursorHelper.DecodeString(cursor);
        if (decodedCursor != null && Guid.TryParse(decodedCursor, out var cursorGuid))
        {
            query = query.Where(n => n.Id.CompareTo(cursorGuid) > 0);
        }

        query = query.OrderBy(n => n.Id);

        var items = await query.Take(limit + 1).ToListAsync();
        var hasMore = items.Count > limit;
        if (hasMore)
        {
            items = items.Take(limit).ToList();
        }

        var data = items.Select(NavigationalAidMapper.ToDto);
        var nextCursor = hasMore && items.Count > 0
            ? CursorHelper.Encode(items[^1].Id.ToString())
            : null;

        return new PaginatedResponse<NavigationalAidDto>
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
}
