using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace PreflightApi.API.Models;

/// <summary>
/// Query parameters for cursor-based pagination. Pass the <c>nextCursor</c> value from a previous
/// response as the <c>cursor</c> parameter to retrieve the next page.
/// </summary>
public class PaginationParams
{
    /// <summary>Opaque cursor value from a previous response's <c>pagination.nextCursor</c> field. Omit or leave null to start from the first page.</summary>
    [FromQuery(Name = "cursor")]
    public string? Cursor { get; set; }

    /// <summary>Maximum number of items to return per page. Minimum 1, maximum 500, default 100.</summary>
    [FromQuery(Name = "limit")]
    [Range(1, 500)]
    [DefaultValue(100)]
    public int Limit { get; set; } = 100;
}
