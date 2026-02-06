namespace PreflightApi.Infrastructure.Dtos.Pagination;

public class PaginatedResponse<T>
{
    public IEnumerable<T> Data { get; init; } = [];
    public PaginationMetadata Pagination { get; init; } = new();
}

public class PaginationMetadata
{
    public string? NextCursor { get; init; }
    public bool HasMore { get; init; }
    public int Limit { get; init; }
}
