using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides access to METAR (Meteorological Aerodrome Report) observations.
/// METARs are routine weather observations from airport weather stations, updated approximately every hour.
/// METAR data is also used by the Performance endpoints to automatically calculate crosswind and density altitude for airports.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/metars")]
[Tags("Weather - METARs")]
public class MetarController(IMetarService metarService) : ControllerBase
{
    /// <summary>
    /// Gets the most recent METAR observation for a specific airport.
    /// Returns decoded weather data including wind, visibility, sky conditions, temperature, and flight category.
    /// </summary>
    /// <param name="icaoCodeOrIdent">ICAO code or FAA identifier (e.g., KDFW, DFW)</param>
    /// <returns>The latest METAR observation for the airport</returns>
    /// <response code="200">Returns the METAR observation</response>
    /// <response code="404">If no METAR is found for the airport</response>
    [HttpGet("{icaoCodeOrIdent}")]
    [ProducesResponseType(typeof(MetarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MetarDto>> GetMetarForAirport(string icaoCodeOrIdent)
    {
        var metar = await metarService.GetMetarForAirport(icaoCodeOrIdent);
        return Ok(metar);
    }

    /// <summary>
    /// Gets METARs for all airports in a state
    /// </summary>
    /// <param name="stateCode">Two-letter state code (e.g., TX, CA)</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of METARs for airports in the state</returns>
    /// <response code="200">Returns the paginated METARs</response>
    [HttpGet("state/{stateCode}")]
    [ProducesResponseType(typeof(PaginatedResponse<MetarDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<MetarDto>>> GetMetarsByState(
        string stateCode,
        [FromQuery] PaginationParams pagination)
    {
        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await metarService.GetMetarsByState(stateCode, pagination.Cursor, pagination.Limit));
    }

    /// <summary>
    /// Gets METARs for all airports across multiple states
    /// </summary>
    /// <remarks>
    /// Pass state codes as a single comma-separated query parameter:
    /// <code>GET /api/v1/metars/by-states?stateCodes=TX,OK,LA</code>
    /// </remarks>
    /// <param name="stateCodes">Comma-separated two-letter state codes (e.g., TX,OK,LA). Must contain at least one code.</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of METARs for airports in the specified states</returns>
    /// <response code="200">Returns the paginated METARs</response>
    /// <response code="400">If the state codes parameter is empty</response>
    [HttpGet("by-states")]
    [ProducesResponseType(typeof(PaginatedResponse<MetarDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<MetarDto>>> GetMetarsByStates(
        [FromQuery] string stateCodes,
        [FromQuery] PaginationParams pagination)
    {
        if (string.IsNullOrWhiteSpace(stateCodes))
            throw new ValidationException("stateCodes", "State codes are required");

        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        var stateCodeArray = stateCodes.Split(',')
            .Select(s => s.Trim())
            .ToArray();

        return Ok(await metarService.GetMetarsByStates(stateCodeArray, pagination.Cursor, pagination.Limit));
    }
}
