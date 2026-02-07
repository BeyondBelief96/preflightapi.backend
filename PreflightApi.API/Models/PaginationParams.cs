using Microsoft.AspNetCore.Mvc;

namespace PreflightApi.API.Models;

/// <summary>
/// Query parameters for cursor-based pagination.
/// </summary>
public class PaginationParams
{
    /// <summary>Cursor from a previous response to fetch the next page.</summary>
    [FromQuery(Name = "cursor")]
    public string? Cursor { get; set; }

    /// <summary>Maximum number of items to return (default 100, max 500).</summary>
    [FromQuery(Name = "limit")]
    public int Limit { get; set; } = 100;
}
