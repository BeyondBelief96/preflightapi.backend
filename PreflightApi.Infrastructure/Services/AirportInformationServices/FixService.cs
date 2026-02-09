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

public class FixService : IFixService
{
    private readonly PreflightApiDbContext _context;
    private readonly ILogger<FixService> _logger;

    public FixService(
        PreflightApiDbContext context,
        ILogger<FixService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PaginatedResponse<FixDto>> GetAllAsync(string? search, string? cursor, int limit)
    {
        _logger.LogInformation("Getting fixes, search: {Search}, cursor: {Cursor}, limit: {Limit}",
            search, cursor, limit);

        var query = _context.Fixes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchUpper = search.ToUpperInvariant();
            query = query.Where(f => f.FixId.Contains(searchUpper));
        }

        var decodedCursor = CursorHelper.DecodeString(cursor);
        if (decodedCursor != null && Guid.TryParse(decodedCursor, out var cursorGuid))
        {
            query = query.Where(f => f.Id.CompareTo(cursorGuid) > 0);
        }

        query = query.OrderBy(f => f.Id);

        var items = await query.Take(limit + 1).ToListAsync();
        var hasMore = items.Count > limit;
        if (hasMore)
        {
            items = items.Take(limit).ToList();
        }

        var data = items.Select(FixMapper.ToDto);
        var nextCursor = hasMore && items.Count > 0
            ? CursorHelper.Encode(items[^1].Id.ToString())
            : null;

        return new PaginatedResponse<FixDto>
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

    public async Task<IEnumerable<FixDto>> GetByIdentifierAsync(string identifier)
    {
        _logger.LogInformation("Getting fixes by identifier: {Identifier}", identifier);

        var upperIdentifier = identifier.ToUpperInvariant();
        var fixes = await _context.Fixes
            .Where(f => f.FixId == upperIdentifier)
            .ToListAsync();

        if (!fixes.Any())
        {
            throw new FixNotFoundException(identifier);
        }

        return fixes.Select(FixMapper.ToDto);
    }

    public async Task<PaginatedResponse<FixDto>> GetByStateAsync(string stateCode, string? cursor, int limit)
    {
        _logger.LogInformation("Getting fixes by state: {StateCode}", stateCode);

        var upperState = stateCode.ToUpperInvariant();
        var query = _context.Fixes
            .Where(f => f.StateCode == upperState);

        var decodedCursor = CursorHelper.DecodeString(cursor);
        if (decodedCursor != null && Guid.TryParse(decodedCursor, out var cursorGuid))
        {
            query = query.Where(f => f.Id.CompareTo(cursorGuid) > 0);
        }

        query = query.OrderBy(f => f.Id);

        var items = await query.Take(limit + 1).ToListAsync();
        var hasMore = items.Count > limit;
        if (hasMore)
        {
            items = items.Take(limit).ToList();
        }

        var data = items.Select(FixMapper.ToDto);
        var nextCursor = hasMore && items.Count > 0
            ? CursorHelper.Encode(items[^1].Id.ToString())
            : null;

        return new PaginatedResponse<FixDto>
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

    public async Task<PaginatedResponse<FixDto>> GetByUseCodeAsync(string useCode, string? cursor, int limit)
    {
        _logger.LogInformation("Getting fixes by use code: {UseCode}", useCode);

        var upperUseCode = useCode.ToUpperInvariant();
        var query = _context.Fixes
            .Where(f => f.FixUseCode != null && f.FixUseCode == upperUseCode);

        var decodedCursor = CursorHelper.DecodeString(cursor);
        if (decodedCursor != null && Guid.TryParse(decodedCursor, out var cursorGuid))
        {
            query = query.Where(f => f.Id.CompareTo(cursorGuid) > 0);
        }

        query = query.OrderBy(f => f.Id);

        var items = await query.Take(limit + 1).ToListAsync();
        var hasMore = items.Count > limit;
        if (hasMore)
        {
            items = items.Take(limit).ToList();
        }

        var data = items.Select(FixMapper.ToDto);
        var nextCursor = hasMore && items.Count > 0
            ? CursorHelper.Encode(items[^1].Id.ToString())
            : null;

        return new PaginatedResponse<FixDto>
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
