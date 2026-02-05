using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Authentication;
using PreflightApi.API.Models;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

[ApiController]
[Route("api/obstacles")]
[ConditionalAuth]
public class ObstacleController(IObstacleService obstacleService)
    : ControllerBase
{
    /// <summary>
    /// Searches for obstacles near a coordinate
    /// </summary>
    /// <param name="lat">Latitude in decimal degrees</param>
    /// <param name="lon">Longitude in decimal degrees</param>
    /// <param name="radiusNm">Search radius in nautical miles (default: 5)</param>
    /// <param name="minHeightAgl">Minimum height AGL in feet (optional filter)</param>
    /// <param name="limit">Maximum number of results (default: 100, max: 500)</param>
    /// <returns>List of obstacles sorted by height AMSL descending</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<ObstacleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<ObstacleDto>>> SearchNearby(
        [FromQuery] decimal lat,
        [FromQuery] decimal lon,
        [FromQuery] double radiusNm = 5,
        [FromQuery] int? minHeightAgl = null,
        [FromQuery] int limit = 100)
    {
        if (lat < -90 || lat > 90)
            throw new ValidationException("lat", "Latitude must be between -90 and 90 degrees");
        if (lon < -180 || lon > 180)
            throw new ValidationException("lon", "Longitude must be between -180 and 180 degrees");
        if (radiusNm <= 0)
            throw new ValidationException("radiusNm", "Radius must be greater than 0");

        limit = Math.Min(limit, 500);
        var obstacles = await obstacleService.SearchNearby(lat, lon, radiusNm, minHeightAgl, limit);
        return Ok(obstacles);
    }

    /// <summary>
    /// Gets obstacles by state code
    /// </summary>
    /// <param name="stateCode">Two-letter state code (e.g., CO, CA, TX)</param>
    /// <param name="minHeightAgl">Minimum height AGL in feet (optional filter)</param>
    /// <param name="limit">Maximum number of results (default: 1000, max: 5000)</param>
    /// <returns>List of obstacles in the state sorted by height AMSL descending</returns>
    [HttpGet("state/{stateCode}")]
    [ProducesResponseType(typeof(IEnumerable<ObstacleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<ObstacleDto>>> GetByState(
        string stateCode,
        [FromQuery] int? minHeightAgl = null,
        [FromQuery] int limit = 1000)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            throw new ValidationException("stateCode", "State code is required");

        limit = Math.Min(limit, 5000);
        var obstacles = await obstacleService.GetByState(stateCode, minHeightAgl, limit);
        return Ok(obstacles);
    }

    /// <summary>
    /// Gets an obstacle by its OAS number
    /// </summary>
    /// <param name="oasNumber">OAS number (e.g., 08-000001)</param>
    /// <returns>Obstacle details</returns>
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
    /// Gets multiple obstacles by their OAS numbers
    /// </summary>
    /// <param name="oasNumbers">List of OAS numbers</param>
    /// <returns>List of obstacles sorted by height AMSL descending</returns>
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
    /// Gets obstacles within a bounding box
    /// </summary>
    /// <param name="minLat">Minimum latitude</param>
    /// <param name="maxLat">Maximum latitude</param>
    /// <param name="minLon">Minimum longitude</param>
    /// <param name="maxLon">Maximum longitude</param>
    /// <param name="minHeightAgl">Minimum height AGL in feet (optional filter)</param>
    /// <param name="limit">Maximum number of results (default: 1000, max: 5000)</param>
    /// <returns>List of obstacles in the bounding box sorted by height AMSL descending</returns>
    [HttpGet("bbox")]
    [ProducesResponseType(typeof(IEnumerable<ObstacleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<ObstacleDto>>> GetByBoundingBox(
        [FromQuery] decimal minLat,
        [FromQuery] decimal maxLat,
        [FromQuery] decimal minLon,
        [FromQuery] decimal maxLon,
        [FromQuery] int? minHeightAgl = null,
        [FromQuery] int limit = 1000)
    {
        if (minLat < -90 || minLat > 90 || maxLat < -90 || maxLat > 90)
            throw new ValidationException("lat", "Latitude values must be between -90 and 90 degrees");
        if (minLon < -180 || minLon > 180 || maxLon < -180 || maxLon > 180)
            throw new ValidationException("lon", "Longitude values must be between -180 and 180 degrees");
        if (minLat >= maxLat)
            throw new ValidationException("lat", "minLat must be less than maxLat");
        if (minLon >= maxLon)
            throw new ValidationException("lon", "minLon must be less than maxLon");

        limit = Math.Min(limit, 5000);
        var obstacles = await obstacleService.GetByBoundingBox(minLat, maxLat, minLon, maxLon, minHeightAgl, limit);
        return Ok(obstacles);
    }
}
