using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PreflightApi.Infrastructure.Dtos.Pagination;

namespace PreflightApi.Infrastructure.Utilities;

public static class PaginationExtensions
{
    /// <summary>
    /// Paginates an IQueryable with a string primary key using cursor-based pagination.
    /// </summary>
    public static async Task<PaginatedResponse<TDto>> ToPaginatedAsync<TEntity, TDto>(
        this IQueryable<TEntity> query,
        Expression<Func<TEntity, string>> keySelector,
        Func<TEntity, TDto> mapper,
        string? cursor,
        int limit,
        CancellationToken ct = default) where TEntity : class
    {
        var decodedCursor = CursorHelper.DecodeString(cursor);

        if (decodedCursor != null)
        {
            var parameter = keySelector.Parameters[0];
            var keyAccess = keySelector.Body;
            var cursorConstant = Expression.Constant(decodedCursor);
            var comparison = Expression.GreaterThan(
                Expression.Call(typeof(string), nameof(string.Compare), null, keyAccess, cursorConstant),
                Expression.Constant(0));
            var lambda = Expression.Lambda<Func<TEntity, bool>>(comparison, parameter);
            query = query.Where(lambda);
        }

        var keyGetter = keySelector.Compile();
        query = query.OrderBy(keySelector);

        var items = await query.Take(limit + 1).ToListAsync(ct);
        var hasMore = items.Count > limit;

        if (hasMore)
        {
            items = items.Take(limit).ToList();
        }

        var data = items.Select(mapper);
        var nextCursor = hasMore && items.Count > 0
            ? CursorHelper.Encode(keyGetter(items[^1]))
            : null;

        return new PaginatedResponse<TDto>
        {
            Data = data,
            Pagination = new PaginationMetadata
            {
                NextCursor = nextCursor,
                HasMore = hasMore,
                Limit = limit
            }
        };
    }

    /// <summary>
    /// Paginates an IQueryable with a Guid primary key using cursor-based pagination.
    /// </summary>
    public static async Task<PaginatedResponse<TDto>> ToPaginatedAsync<TEntity, TDto>(
        this IQueryable<TEntity> query,
        Expression<Func<TEntity, Guid>> keySelector,
        Func<TEntity, TDto> mapper,
        string? cursor,
        int limit,
        CancellationToken ct = default) where TEntity : class
    {
        var decodedCursor = CursorHelper.DecodeGuid(cursor);

        if (decodedCursor.HasValue)
        {
            var parameter = keySelector.Parameters[0];
            var keyAccess = keySelector.Body;
            var cursorConstant = Expression.Constant(decodedCursor.Value);
            var comparison = Expression.GreaterThan(keyAccess, cursorConstant);
            var lambda = Expression.Lambda<Func<TEntity, bool>>(comparison, parameter);
            query = query.Where(lambda);
        }

        var keyGetter = keySelector.Compile();
        query = query.OrderBy(keySelector);

        var items = await query.Take(limit + 1).ToListAsync(ct);
        var hasMore = items.Count > limit;

        if (hasMore)
        {
            items = items.Take(limit).ToList();
        }

        var data = items.Select(mapper);
        var nextCursor = hasMore && items.Count > 0
            ? CursorHelper.Encode(keyGetter(items[^1]))
            : null;

        return new PaginatedResponse<TDto>
        {
            Data = data,
            Pagination = new PaginationMetadata
            {
                NextCursor = nextCursor,
                HasMore = hasMore,
                Limit = limit
            }
        };
    }

    /// <summary>
    /// Paginates an IQueryable with an int primary key using cursor-based pagination.
    /// </summary>
    public static async Task<PaginatedResponse<TDto>> ToPaginatedAsync<TEntity, TDto>(
        this IQueryable<TEntity> query,
        Expression<Func<TEntity, int>> keySelector,
        Func<TEntity, TDto> mapper,
        string? cursor,
        int limit,
        CancellationToken ct = default) where TEntity : class
    {
        var decodedCursor = CursorHelper.DecodeInt(cursor);

        if (decodedCursor.HasValue)
        {
            var parameter = keySelector.Parameters[0];
            var keyAccess = keySelector.Body;
            var cursorConstant = Expression.Constant(decodedCursor.Value);
            var comparison = Expression.GreaterThan(keyAccess, cursorConstant);
            var lambda = Expression.Lambda<Func<TEntity, bool>>(comparison, parameter);
            query = query.Where(lambda);
        }

        var keyGetter = keySelector.Compile();
        query = query.OrderBy(keySelector);

        var items = await query.Take(limit + 1).ToListAsync(ct);
        var hasMore = items.Count > limit;

        if (hasMore)
        {
            items = items.Take(limit).ToList();
        }

        var data = items.Select(mapper);
        var nextCursor = hasMore && items.Count > 0
            ? CursorHelper.Encode(keyGetter(items[^1]))
            : null;

        return new PaginatedResponse<TDto>
        {
            Data = data,
            Pagination = new PaginationMetadata
            {
                NextCursor = nextCursor,
                HasMore = hasMore,
                Limit = limit
            }
        };
    }
}
