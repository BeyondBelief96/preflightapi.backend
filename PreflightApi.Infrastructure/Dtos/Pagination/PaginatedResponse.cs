namespace PreflightApi.Infrastructure.Dtos.Pagination;

/// <summary>
/// Cursor-based paginated response wrapper. To retrieve subsequent pages, pass the
/// <c>pagination.nextCursor</c> value as the <c>cursor</c> query parameter in your next request.
/// Continue until <c>pagination.hasMore</c> is false.
/// </summary>
/// <typeparam name="T">The type of items in the response.</typeparam>
public class PaginatedResponse<T>
{
    /// <summary>The current page of results.</summary>
    public IEnumerable<T> Data { get; init; } = [];
    /// <summary>Pagination metadata including the cursor to fetch the next page.</summary>
    public PaginationMetadata Pagination { get; init; } = new();
}

/// <summary>
/// Metadata for cursor-based pagination. Use <c>nextCursor</c> as the <c>cursor</c> query parameter
/// to fetch the next page of results. When <c>hasMore</c> is false, there are no more pages.
/// </summary>
public class PaginationMetadata
{
    /// <summary>Opaque cursor value to pass as the <c>cursor</c> query parameter to fetch the next page. Null when there are no more results.</summary>
    public string? NextCursor { get; init; }
    /// <summary>True if more results are available beyond this page; false if this is the last page.</summary>
    public bool HasMore { get; init; }
    /// <summary>Maximum number of items returned per page (1-500, default 100).</summary>
    public int Limit { get; init; }
}
