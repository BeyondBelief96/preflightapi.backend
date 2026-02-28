using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PreflightApi.Infrastructure.Dtos.Pagination;

namespace PreflightApi.Infrastructure.Utilities;

public static class PaginationExtensions
{
    /// <summary>
    /// Paginates an IQueryable with a string primary key using cursor-based pagination.
    /// Supports both forward and backward navigation via directional cursors.
    /// </summary>
    public static async Task<PaginatedResponse<TDto>> ToPaginatedAsync<TEntity, TDto>(
        this IQueryable<TEntity> query,
        Expression<Func<TEntity, string>> keySelector,
        Func<TEntity, TDto> mapper,
        string? cursor,
        int limit,
        CancellationToken ct = default) where TEntity : class
    {
        var decoded = CursorHelper.DecodeStringWithDirection(cursor);
        var isBackward = decoded?.Direction == CursorDirection.Backward;
        var decodedKey = decoded?.Value;
        var hasCursor = decoded != null;

        var parameter = keySelector.Parameters[0];
        var keyAccess = keySelector.Body;

        if (decodedKey != null)
        {
            var cursorConstant = Expression.Constant(decodedKey);
            var comparison = isBackward
                ? Expression.LessThan(
                    Expression.Call(typeof(string), nameof(string.Compare), null, keyAccess, cursorConstant),
                    Expression.Constant(0))
                : Expression.GreaterThan(
                    Expression.Call(typeof(string), nameof(string.Compare), null, keyAccess, cursorConstant),
                    Expression.Constant(0));
            var lambda = Expression.Lambda<Func<TEntity, bool>>(comparison, parameter);
            query = query.Where(lambda);
        }

        var keyGetter = keySelector.Compile();

        query = isBackward
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);

        var items = await query.Take(limit + 1).ToListAsync(ct);
        var hasExtra = items.Count > limit;

        if (hasExtra)
            items = items.Take(limit).ToList();

        if (isBackward)
            items.Reverse();

        return BuildResponse(items, keyGetter, mapper, limit, hasCursor, isBackward, hasExtra);
    }

    /// <summary>
    /// Paginates an IQueryable with a Guid primary key using cursor-based pagination.
    /// Supports both forward and backward navigation via directional cursors.
    /// </summary>
    public static async Task<PaginatedResponse<TDto>> ToPaginatedAsync<TEntity, TDto>(
        this IQueryable<TEntity> query,
        Expression<Func<TEntity, Guid>> keySelector,
        Func<TEntity, TDto> mapper,
        string? cursor,
        int limit,
        CancellationToken ct = default) where TEntity : class
    {
        var decoded = CursorHelper.DecodeGuidWithDirection(cursor);
        var isBackward = decoded?.Direction == CursorDirection.Backward;
        var decodedKey = decoded?.Value;
        var hasCursor = decoded != null;

        if (decodedKey.HasValue)
        {
            var parameter = keySelector.Parameters[0];
            var keyAccess = keySelector.Body;
            var cursorConstant = Expression.Constant(decodedKey.Value);
            var comparison = isBackward
                ? Expression.LessThan(keyAccess, cursorConstant)
                : Expression.GreaterThan(keyAccess, cursorConstant);
            var lambda = Expression.Lambda<Func<TEntity, bool>>(comparison, parameter);
            query = query.Where(lambda);
        }

        var keyGetter = keySelector.Compile();

        query = isBackward
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);

        var items = await query.Take(limit + 1).ToListAsync(ct);
        var hasExtra = items.Count > limit;

        if (hasExtra)
            items = items.Take(limit).ToList();

        if (isBackward)
            items.Reverse();

        return BuildResponse(items, e => keyGetter(e).ToString(), mapper, limit, hasCursor, isBackward, hasExtra);
    }

    /// <summary>
    /// Paginates an IQueryable with an int primary key using cursor-based pagination.
    /// Supports both forward and backward navigation via directional cursors.
    /// </summary>
    public static async Task<PaginatedResponse<TDto>> ToPaginatedAsync<TEntity, TDto>(
        this IQueryable<TEntity> query,
        Expression<Func<TEntity, int>> keySelector,
        Func<TEntity, TDto> mapper,
        string? cursor,
        int limit,
        CancellationToken ct = default) where TEntity : class
    {
        var decoded = CursorHelper.DecodeIntWithDirection(cursor);
        var isBackward = decoded?.Direction == CursorDirection.Backward;
        var decodedKey = decoded?.Value;
        var hasCursor = decoded != null;

        if (decodedKey.HasValue)
        {
            var parameter = keySelector.Parameters[0];
            var keyAccess = keySelector.Body;
            var cursorConstant = Expression.Constant(decodedKey.Value);
            var comparison = isBackward
                ? Expression.LessThan(keyAccess, cursorConstant)
                : Expression.GreaterThan(keyAccess, cursorConstant);
            var lambda = Expression.Lambda<Func<TEntity, bool>>(comparison, parameter);
            query = query.Where(lambda);
        }

        var keyGetter = keySelector.Compile();

        query = isBackward
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);

        var items = await query.Take(limit + 1).ToListAsync(ct);
        var hasExtra = items.Count > limit;

        if (hasExtra)
            items = items.Take(limit).ToList();

        if (isBackward)
            items.Reverse();

        return BuildResponse(items, e => keyGetter(e).ToString(), mapper, limit, hasCursor, isBackward, hasExtra);
    }

    private static PaginatedResponse<TDto> BuildResponse<TEntity, TDto>(
        List<TEntity> items,
        Func<TEntity, string> keyToString,
        Func<TEntity, TDto> mapper,
        int limit,
        bool hasCursor,
        bool isBackward,
        bool hasExtra)
    {
        bool hasMore, hasPrevious;
        string? nextCursor, previousCursor;

        if (isBackward)
        {
            hasPrevious = hasExtra;
            hasMore = items.Count > 0; // Items after us if we actually have results
            previousCursor = hasPrevious && items.Count > 0
                ? CursorHelper.EncodePrevious(keyToString(items[0]))
                : null;
            nextCursor = items.Count > 0
                ? CursorHelper.EncodeNext(keyToString(items[^1]))
                : null;
        }
        else
        {
            hasMore = hasExtra;
            hasPrevious = hasCursor;
            nextCursor = hasMore && items.Count > 0
                ? CursorHelper.EncodeNext(keyToString(items[^1]))
                : null;
            previousCursor = hasCursor && items.Count > 0
                ? CursorHelper.EncodePrevious(keyToString(items[0]))
                : null;
        }

        return new PaginatedResponse<TDto>
        {
            Data = items.Select(mapper),
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
}
