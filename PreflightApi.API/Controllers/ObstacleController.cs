using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides access to obstacle data from the FAA Digital Obstacle File (DOF).
/// Obstacles include towers, buildings, smokestacks, and other structures that may affect flight safety.
/// Each obstacle has an OAS (Obstacle Assessment Surface) number as its unique identifier.
/// OAS numbers are returned by the navigation log endpoint for obstacles near a planned route —
/// use the by-oas-numbers endpoint to retrieve full details for those obstacles.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/obstacles")]
[Tags("Obstacles")]
public class ObstacleController(IObstacleService obstacleService, IAirportService airportService)
    : ControllerBase
{
    /// <summary>
    /// Searches for obstacles near an airport. Looks up the airport coordinates, then finds
    /// obstacles within the specified radius. Use <c>minHeightAgl</c> to filter out low obstacles.
    /// </summary>
    /// <remarks>
    /// <code>
    /// GET /api/v1/obstacles/airport/KDFW                              — default 10 NM radius
    /// GET /api/v1/obstacles/airport/DFW?radiusNm=5&amp;minHeightAgl=200  — towers 200+ ft AGL within 5 NM
    /// </code>
    /// </remarks>
    /// <param name="icaoCodeOrIdent">ICAO code (e.g., KDFW) or FAA identifier (e.g., DFW)</param>
    /// <param name="radiusNm">Search radius in nautical miles (default 10, must be greater than 0)</param>
    /// <param name="minHeightAgl">Optional minimum height AGL in feet — only return obstacles at or above this height</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of obstacles within the search radius of the airport</returns>
    /// <response code="200">Returns the obstacles found</response>
    /// <response code="400">If the radius is invalid or the airport has no coordinates on record</response>
    /// <response code="404">If the airport is not found</response>
    [HttpGet("airport/{icaoCodeOrIdent}")]
    [ProducesResponseType(typeof(PaginatedResponse<ObstacleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaginatedResponse<ObstacleDto>>> SearchNearAirport(
        string icaoCodeOrIdent,
        [FromQuery] double radiusNm = 10,
        [FromQuery] int? minHeightAgl = null,
        [FromQuery] PaginationParams? pagination = null)
    {
        if (radiusNm <= 0)
            throw new ValidationException("radiusNm", "Radius must be greater than 0");

        var airport = await airportService.GetAirportByIcaoCodeOrIdent(icaoCodeOrIdent);

        if (airport.LatDecimal == null || airport.LongDecimal == null)
            throw new ValidationException("icaoCodeOrIdent", $"Airport '{icaoCodeOrIdent}' does not have coordinates on record");

        pagination ??= new PaginationParams();
        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await obstacleService.SearchNearby(airport.LatDecimal.Value, airport.LongDecimal.Value, radiusNm, minHeightAgl, pagination.Cursor, pagination.Limit));
    }

    /// <summary>
    /// Searches for obstacles near a geographic point.
    /// </summary>
    /// <remarks>
    /// <code>
    /// GET /api/v1/obstacles/search?lat=32.897&amp;lon=-97.038                              — default 5 NM radius
    /// GET /api/v1/obstacles/search?lat=32.897&amp;lon=-97.038&amp;radiusNm=10&amp;minHeightAgl=500  — tall obstacles within 10 NM
    /// </code>
    /// </remarks>
    /// <param name="lat">Latitude in decimal degrees (-90 to 90)</param>
    /// <param name="lon">Longitude in decimal degrees (-180 to 180)</param>
    /// <param name="radiusNm">Search radius in nautical miles (default 5, must be greater than 0)</param>
    /// <param name="minHeightAgl">Optional minimum height AGL in feet — only return obstacles at or above this height</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of obstacles within the search radius</returns>
    /// <response code="200">Returns the obstacles found</response>
    /// <response code="400">If coordinates or radius are invalid</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PaginatedResponse<ObstacleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<ObstacleDto>>> SearchNearby(
        [FromQuery] decimal lat,
        [FromQuery] decimal lon,
        [FromQuery] double radiusNm = 5,
        [FromQuery] int? minHeightAgl = null,
        [FromQuery] PaginationParams? pagination = null)
    {
        if (lat < -90 || lat > 90)
            throw new ValidationException("lat", "Latitude must be between -90 and 90 degrees");
        if (lon < -180 || lon > 180)
            throw new ValidationException("lon", "Longitude must be between -180 and 180 degrees");
        if (radiusNm <= 0)
            throw new ValidationException("radiusNm", "Radius must be greater than 0");

        pagination ??= new PaginationParams();
        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await obstacleService.SearchNearby(lat, lon, radiusNm, minHeightAgl, pagination.Cursor, pagination.Limit));
    }

    /// <summary>
    /// Gets obstacles in a specific state.
    /// </summary>
    /// <remarks>
    /// <code>
    /// GET /api/v1/obstacles/state/TX                      — all obstacles in Texas
    /// GET /api/v1/obstacles/state/TX?minHeightAgl=1000    — obstacles 1000+ ft AGL in Texas
    /// </code>
    /// </remarks>
    /// <param name="stateCode">Two-letter state code (e.g., <c>TX</c>, <c>CA</c>)</param>
    /// <param name="minHeightAgl">Optional minimum height AGL in feet — only return obstacles at or above this height</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of obstacles in the state</returns>
    /// <response code="200">Returns the obstacles</response>
    /// <response code="400">If the state code is empty</response>
    [HttpGet("state/{stateCode}")]
    [ProducesResponseType(typeof(PaginatedResponse<ObstacleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<ObstacleDto>>> GetByState(
        string stateCode,
        [FromQuery] int? minHeightAgl = null,
        [FromQuery] PaginationParams? pagination = null)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            throw new ValidationException("stateCode", "State code is required");

        pagination ??= new PaginationParams();
        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await obstacleService.GetByState(stateCode, minHeightAgl, pagination.Cursor, pagination.Limit));
    }

    /// <summary>
    /// Gets a single obstacle by its OAS (Obstacle Assessment Surface) number.
    /// </summary>
    /// <remarks>
    /// <code>
    /// GET /api/v1/obstacles/12-345678
    /// </code>
    /// </remarks>
    /// <param name="oasNumber">Obstacle Assessment Surface number (e.g., <c>12-345678</c>)</param>
    /// <returns>The obstacle details including type, height (AGL and MSL), lighting, coordinates, and marking</returns>
    /// <response code="200">Returns the obstacle</response>
    /// <response code="404">If the obstacle is not found</response>
    [HttpGet("{oasNumber}")]
    [ProducesResponseType(typeof(ObstacleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ObstacleDto>> GetByOasNumber(string oasNumber)
    {
        var obstacle = await obstacleService.GetByOasNumber(oasNumber);
        if (obstacle == null)
        {
            throw new ObstacleNotFoundException(oasNumber);
        }
        return Ok(obstacle);
    }

    /// <summary>
    /// Gets multiple obstacles by their OAS numbers. This endpoint is designed to be used with the
    /// ObstacleOasNumbers returned by the navigation log endpoint (<c>POST /api/v1/navlog/calculate</c>)
    /// to retrieve full details for obstacles near a planned route.
    /// </summary>
    /// <remarks>
    /// Send a JSON array of OAS number strings in the request body:
    /// <code>
    /// ["12-345678", "12-345679", "12-345680"]
    /// </code>
    /// </remarks>
    /// <param name="oasNumbers">JSON array of OAS number strings (maximum 1000)</param>
    /// <returns>Obstacles matching the specified OAS numbers with type, height, lighting, and location data</returns>
    /// <response code="200">Returns the matching obstacles</response>
    /// <response code="400">If the list is empty or exceeds 1000 items</response>
    [HttpPost("by-oas-numbers")]
    [ProducesResponseType(typeof(IEnumerable<ObstacleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<ObstacleDto>>> GetByOasNumbers([FromBody] List<string> oasNumbers)
    {
        if (oasNumbers == null || oasNumbers.Count == 0)
        {
            throw new ValidationException("oasNumbers", "At least one OAS number is required");
        }

        if (oasNumbers.Count > 1000)
        {
            throw new ValidationException("oasNumbers", "Maximum of 1000 OAS numbers allowed per request");
        }

        var obstacles = await obstacleService.GetByOasNumbers(oasNumbers);
        return Ok(obstacles);
    }

    /// <summary>
    /// Gets obstacles within a geographic bounding box.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Note:</strong> The bounding box must not cross the antimeridian (i.e., <c>minLon</c>
    /// must be less than <c>maxLon</c>). Antimeridian-crossing queries are not supported.
    /// </para>
    /// <code>
    /// GET /api/v1/obstacles/bbox?minLat=32.5&amp;maxLat=33.5&amp;minLon=-97.5&amp;maxLon=-96.5
    /// GET /api/v1/obstacles/bbox?minLat=32.5&amp;maxLat=33.5&amp;minLon=-97.5&amp;maxLon=-96.5&amp;minHeightAgl=500
    /// </code>
    /// </remarks>
    /// <param name="minLat">Southwest corner latitude (-90 to 90)</param>
    /// <param name="maxLat">Northeast corner latitude (-90 to 90, must be greater than minLat)</param>
    /// <param name="minLon">Southwest corner longitude (-180 to 180)</param>
    /// <param name="maxLon">Northeast corner longitude (-180 to 180, must be greater than minLon)</param>
    /// <param name="minHeightAgl">Optional minimum height AGL in feet — only return obstacles at or above this height</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of obstacles within the bounding box</returns>
    /// <response code="200">Returns the obstacles found</response>
    /// <response code="400">If coordinates are invalid, minLat >= maxLat, or the box crosses the antimeridian</response>
    [HttpGet("bbox")]
    [ProducesResponseType(typeof(PaginatedResponse<ObstacleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<ObstacleDto>>> GetByBoundingBox(
        [FromQuery] decimal minLat,
        [FromQuery] decimal maxLat,
        [FromQuery] decimal minLon,
        [FromQuery] decimal maxLon,
        [FromQuery] int? minHeightAgl = null,
        [FromQuery] PaginationParams? pagination = null)
    {
        if (minLat < -90 || minLat > 90 || maxLat < -90 || maxLat > 90)
            throw new ValidationException("lat", "Latitude values must be between -90 and 90 degrees");
        if (minLon < -180 || minLon > 180 || maxLon < -180 || maxLon > 180)
            throw new ValidationException("lon", "Longitude values must be between -180 and 180 degrees");
        if (minLat >= maxLat)
            throw new ValidationException("lat", "minLat must be less than maxLat");
        if (minLon >= maxLon)
            throw new ValidationException("lon", "minLon must be less than maxLon");

        pagination ??= new PaginationParams();
        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await obstacleService.GetByBoundingBox(minLat, maxLat, minLon, maxLon, minHeightAgl, pagination.Cursor, pagination.Limit));
    }
}
