using Microsoft.AspNetCore.Mvc;

namespace PreflightApi.API.Models;

public class PaginationParams
{
    [FromQuery(Name = "cursor")]
    public string? Cursor { get; set; }

    [FromQuery(Name = "limit")]
    public int Limit { get; set; } = 100;
}
