namespace PreflightApi.Infrastructure.Dtos.Pagination;

/// <summary>
/// Cursor-based paginated response wrapper.
/// </summary>
/// <typeparam name="T">The type of items in the response.</typeparam>
public class PaginatedResponse<T>
{
    /// <summary>The page of results.</summary>
    public IEnumerable<T> Data { get; init; } = [];
    /// <summary>Pagination metadata including cursor for next page.</summary>
    public PaginationMetadata Pagination { get; init; } = new();
}

/// <summary>
/// Metadata for cursor-based pagination.
/// </summary>
public class PaginationMetadata
{
    /// <summary>Cursor value to pass for the next page of results (null if no more pages).</summary>
    public string? NextCursor { get; init; }
    /// <summary>Whether more results are available beyond this page.</summary>
    public bool HasMore { get; init; }
    /// <summary>Maximum number of items per page.</summary>
    public int Limit { get; init; }
}
