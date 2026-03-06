using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.API.Utilities;
using PreflightApi.Domain.Enums;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides access to FAA runway data with filtering, spatial search, and optional ArcGIS polygon geometry.
/// Runways include dimensions, surface type, lighting, weight-bearing capacity, and detailed runway end information.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/runways")]
[Tags("Runways")]
public class RunwayController(IRunwayService runwayService) : ControllerBase
{
    /// <summary>
    /// Gets runways for a specific airport by ICAO code or FAA identifier.
    /// </summary>
    /// <remarks>
    /// <code>
    /// GET /api/v1/runways/airport/KDFW                          — runways at DFW
    /// GET /api/v1/runways/airport/DFW?includeGeometry=true      — with ArcGIS polygon geometry
    /// </code>
    /// </remarks>
    /// <param name="icaoCodeOrIdent">ICAO code or FAA identifier (e.g., KDFW, DFW)</param>
    /// <param name="includeGeometry">Include ArcGIS runway polygon geometry in the response (default false)</param>
    /// <returns>Runways and runway end details for the airport</returns>
    /// <response code="200">Returns the airport's runways</response>
    /// <response code="400">If the identifier is empty</response>
    /// <response code="404">If the airport is not found</response>
    [HttpGet("airport/{icaoCodeOrIdent}")]
    [ProducesResponseType(typeof(IEnumerable<RunwayDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<RunwayDto>>> GetRunwaysByAirport(
        string icaoCodeOrIdent,
        [FromQuery] bool includeGeometry = false,
        CancellationToken ct = default)
    {
        ValidationHelpers.ValidateRequiredString(icaoCodeOrIdent, "icaoCodeOrIdent", "ICAO code or identifier is required");
        var runways = await runwayService.GetRunwaysByAirportAsync(icaoCodeOrIdent, includeGeometry, ct);
        return Ok(runways);
    }

    /// <summary>
    /// Gets a paginated list of runways with optional filtering.
    /// </summary>
    /// <remarks>
    /// <code>
    /// GET /api/v1/runways                                       — all runways (paginated)
    /// GET /api/v1/runways?search=DFW                            — search by airport identifier, name, or city
    /// GET /api/v1/runways?surfaceType=Asphalt&amp;minLength=5000    — asphalt runways 5000+ ft
    /// GET /api/v1/runways?state=TX&amp;lighted=true                 — lighted runways in Texas
    /// </code>
    /// </remarks>
    /// <param name="search">Search across airport identifier, ICAO code, name, and city</param>
    /// <param name="surfaceType">Filter by runway surface type enum (e.g., Asphalt, Concrete, Turf). Matches runways where the type appears as either the primary or secondary surface.</param>
    /// <param name="minLength">Minimum runway length in feet</param>
    /// <param name="state">Filter by two-letter state code (e.g., TX, CA)</param>
    /// <param name="lighted">Filter by whether the runway has edge lighting</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of runways with airport context</returns>
    /// <response code="200">Returns the runways</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<RunwayDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<RunwayDto>>> GetRunways(
        [FromQuery] PaginationParams pagination,
        CancellationToken ct,
        [FromQuery] string? search = null,
        [FromQuery] RunwaySurfaceType? surfaceType = null,
        [FromQuery] int? minLength = null,
        [FromQuery] string? state = null,
        [FromQuery] bool? lighted = null)
    {
        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await runwayService.GetRunways(search, surfaceType, minLength, state, lighted, pagination.Cursor, pagination.Limit, ct));
    }

    /// <summary>
    /// Searches for runways near a geographic point using the parent airport's location.
    /// Useful for finding diversion airports with suitable runways.
    /// </summary>
    /// <remarks>
    /// <code>
    /// GET /api/v1/runways/nearby?lat=32.897&amp;lon=-97.038                                  — default 30 NM radius
    /// GET /api/v1/runways/nearby?lat=32.897&amp;lon=-97.038&amp;minLength=4000&amp;surfaceType=Asphalt — paved 4000+ ft runways
    /// GET /api/v1/runways/nearby?lat=32.897&amp;lon=-97.038&amp;includeGeometry=true               — with polygon geometry
    /// </code>
    /// </remarks>
    /// <param name="lat">Latitude in decimal degrees (-90 to 90)</param>
    /// <param name="lon">Longitude in decimal degrees (-180 to 180)</param>
    /// <param name="radiusNm">Search radius in nautical miles (default 30, max 500)</param>
    /// <param name="minLength">Minimum runway length in feet</param>
    /// <param name="surfaceType">Filter by runway surface type enum (e.g., Asphalt, Concrete). Matches runways where the type appears as either the primary or secondary surface.</param>
    /// <param name="includeGeometry">Include ArcGIS runway polygon geometry in the response (default false)</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of runways within the search radius</returns>
    /// <response code="200">Returns the runways found</response>
    /// <response code="400">If coordinates or radius are invalid</response>
    [HttpGet("nearby")]
    [ProducesResponseType(typeof(PaginatedResponse<RunwayDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<RunwayDto>>> SearchNearby(
        [FromQuery] decimal lat,
        [FromQuery] decimal lon,
        [FromQuery] PaginationParams pagination,
        CancellationToken ct,
        [FromQuery] double radiusNm = 30,
        [FromQuery] int? minLength = null,
        [FromQuery] RunwaySurfaceType? surfaceType = null,
        [FromQuery] bool includeGeometry = false)
    {
        ValidationHelpers.ValidateCoordinates(lat, lon);
        ValidationHelpers.ValidateRadius(radiusNm, 500);

        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await runwayService.SearchNearby(lat, lon, radiusNm, minLength, surfaceType, includeGeometry, pagination.Cursor, pagination.Limit, ct));
    }
}
