using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.API.Utilities;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides access to PIREPs (Pilot Reports) issued in PIREP or AIREP format — real-time weather observations
/// reported by pilots in flight. PIREPs contain firsthand reports of turbulence, icing, sky conditions,
/// visibility, and other flight conditions at specific altitudes and locations. Unlike METARs (ground-based),
/// PIREPs describe conditions aloft. Report types are UA (routine) or UUA (urgent, indicating severe conditions).
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/pireps")]
[Tags("Weather - PIREPs")]
public class PirepController(IPirepService pirepService, IAirportService airportService) : ControllerBase
{
    /// <summary>
    /// Gets all current PIREPs. Returns all active pilot reports with reported conditions
    /// including turbulence (type, intensity, altitude), icing (type, intensity, altitude),
    /// and sky conditions. Each report includes the geographic coordinates and altitude where
    /// the observation was made.
    /// </summary>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of active pilot reports including turbulence, icing, and sky conditions</returns>
    /// <response code="200">Returns the paginated list of PIREPs</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<PirepDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<PirepDto>>> GetAllPireps(
        [FromQuery] PaginationParams pagination,
        CancellationToken ct)
    {
        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await pirepService.GetAllPireps(pagination.Cursor, pagination.Limit, ct));
    }

    /// <summary>
    /// Searches for PIREPs near a geographic point. Returns pilot reports within the specified
    /// radius of the given coordinates, useful for checking conditions along a flight route.
    /// </summary>
    /// <remarks>
    /// <code>
    /// GET /api/v1/pireps/nearby?lat=32.897&amp;lon=-97.038                    — default 50 NM radius
    /// GET /api/v1/pireps/nearby?lat=32.897&amp;lon=-97.038&amp;radiusNm=100    — custom radius
    /// </code>
    /// </remarks>
    /// <param name="lat">Latitude in decimal degrees (-90 to 90)</param>
    /// <param name="lon">Longitude in decimal degrees (-180 to 180)</param>
    /// <param name="radiusNm">Search radius in nautical miles (default 50, max 500)</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of PIREPs within the search radius</returns>
    /// <response code="200">Returns the PIREPs found</response>
    /// <response code="400">If coordinates or radius are invalid</response>
    [HttpGet("nearby")]
    [ProducesResponseType(typeof(PaginatedResponse<PirepDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<PirepDto>>> SearchNearby(
        [FromQuery] decimal lat,
        [FromQuery] decimal lon,
        [FromQuery] PaginationParams pagination,
        CancellationToken ct,
        [FromQuery] double radiusNm = 50)
    {
        ValidationHelpers.ValidateCoordinates(lat, lon);
        ValidationHelpers.ValidateRadius(radiusNm, 500);

        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await pirepService.SearchNearby(lat, lon, radiusNm, pagination.Cursor, pagination.Limit, ct));
    }

    /// <summary>
    /// Searches for PIREPs near an airport. Looks up the airport coordinates by ICAO code or
    /// FAA identifier, then returns pilot reports within the specified radius.
    /// </summary>
    /// <remarks>
    /// <code>
    /// GET /api/v1/pireps/airport/KDFW                  — default 50 NM radius
    /// GET /api/v1/pireps/airport/DFW?radiusNm=100      — custom radius with FAA identifier
    /// </code>
    /// </remarks>
    /// <param name="icaoCodeOrIdent">ICAO code (e.g., KDFW) or FAA identifier (e.g., DFW)</param>
    /// <param name="radiusNm">Search radius in nautical miles (default 50, max 500)</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of PIREPs within the search radius of the airport</returns>
    /// <response code="200">Returns the PIREPs found</response>
    /// <response code="400">If the radius is invalid or the airport has no coordinates on record</response>
    /// <response code="404">If the airport is not found</response>
    [HttpGet("airport/{icaoCodeOrIdent}")]
    [ProducesResponseType(typeof(PaginatedResponse<PirepDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaginatedResponse<PirepDto>>> SearchNearAirport(
        string icaoCodeOrIdent,
        [FromQuery] PaginationParams pagination,
        CancellationToken ct,
        [FromQuery] double radiusNm = 50)
    {
        ValidationHelpers.ValidateRequiredString(icaoCodeOrIdent, "icaoCodeOrIdent", "ICAO code or identifier is required");
        ValidationHelpers.ValidateRadius(radiusNm, 500);

        var airport = await airportService.GetAirportByIcaoCodeOrIdent(icaoCodeOrIdent, ct);

        if (airport.LatDecimal == null || airport.LongDecimal == null)
            throw new ValidationException("icaoCodeOrIdent", $"Airport '{icaoCodeOrIdent}' does not have coordinates on record");

        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await pirepService.SearchNearby(airport.LatDecimal.Value, airport.LongDecimal.Value, radiusNm, pagination.Cursor, pagination.Limit, ct));
    }
}
