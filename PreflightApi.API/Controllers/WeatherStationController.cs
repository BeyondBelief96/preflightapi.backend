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
[Route("api/v{version:apiVersion}/weather-stations")]
[Tags("Weather Stations")]
public class WeatherStationController(IWeatherStationService weatherStationService)
    : ControllerBase
{
    /// <summary>
    /// Gets all weather stations with optional search filter
    /// </summary>
    /// <param name="search">Optional search by station identifier or city</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of weather stations</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<WeatherStationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<WeatherStationDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] PaginationParams pagination)
    {
        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await weatherStationService.GetAllAsync(search, pagination.Cursor, pagination.Limit));
    }

    /// <summary>
    /// Gets weather stations by identifier
    /// </summary>
    /// <param name="identifier">Weather station identifier (ASOS/AWOS ID)</param>
    /// <returns>List of weather stations matching the identifier</returns>
    [HttpGet("{identifier}")]
    [ProducesResponseType(typeof(IEnumerable<WeatherStationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<WeatherStationDto>>> GetByIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ValidationException("identifier", "Weather station identifier is required");

        return Ok(await weatherStationService.GetByIdentifierAsync(identifier));
    }

    /// <summary>
    /// Gets weather stations by sensor type
    /// </summary>
    /// <param name="sensorType">Sensor type (e.g., ASOS, AWOS-1, AWOS-2, AWOS-3)</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of weather stations of the specified type</returns>
    [HttpGet("type/{sensorType}")]
    [ProducesResponseType(typeof(PaginatedResponse<WeatherStationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<WeatherStationDto>>> GetByType(
        string sensorType,
        [FromQuery] PaginationParams pagination)
    {
        if (string.IsNullOrWhiteSpace(sensorType))
            throw new ValidationException("sensorType", "Sensor type is required");

        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await weatherStationService.GetByTypeAsync(sensorType, pagination.Cursor, pagination.Limit));
    }

    /// <summary>
    /// Gets weather stations by state
    /// </summary>
    /// <param name="stateCode">Two-letter state code (e.g., TX, CA)</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of weather stations in the specified state</returns>
    [HttpGet("state/{stateCode}")]
    [ProducesResponseType(typeof(PaginatedResponse<WeatherStationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<WeatherStationDto>>> GetByState(
        string stateCode,
        [FromQuery] PaginationParams pagination)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            throw new ValidationException("stateCode", "State code is required");

        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await weatherStationService.GetByStateAsync(stateCode, pagination.Cursor, pagination.Limit));
    }

    /// <summary>
    /// Gets weather stations associated with an airport
    /// </summary>
    /// <param name="airportIdentifier">Airport identifier (FAA or ICAO code)</param>
    /// <returns>List of weather stations at the airport</returns>
    [HttpGet("airport/{airportIdentifier}")]
    [ProducesResponseType(typeof(IEnumerable<WeatherStationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<WeatherStationDto>>> GetByAirport(string airportIdentifier)
    {
        if (string.IsNullOrWhiteSpace(airportIdentifier))
            throw new ValidationException("airportIdentifier", "Airport identifier is required");

        return Ok(await weatherStationService.GetByAirportAsync(airportIdentifier));
    }
}
