using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/obstacles")]
public class ObstacleController(IObstacleService obstacleService)
    : ControllerBase
{
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
