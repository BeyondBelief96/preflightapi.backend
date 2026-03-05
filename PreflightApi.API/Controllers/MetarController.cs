using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.API.Utilities;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides access to decoded METAR (Meteorological Aerodrome Report) aviation weather observations.
/// METARs are routine weather observations from airport weather stations, updated approximately every hour.
/// METAR data is also used by the E6B endpoints to automatically calculate crosswind and density altitude for airports.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/metars")]
[Tags("Weather - METARs")]
public class MetarController(IMetarService metarService) : ControllerBase
{
    /// <summary>
    /// Gets the most recent METAR observation for a specific airport.
    /// Returns decoded weather data including wind, visibility, sky conditions, temperature, and flight category.
    /// </summary>
    /// <remarks>
    /// <code>
    /// GET /api/v1/metars/KDFW    — by ICAO code
    /// GET /api/v1/metars/DFW     — by FAA identifier
    /// </code>
    /// </remarks>
    /// <param name="icaoCodeOrIdent">ICAO code or FAA identifier (e.g., KDFW, DFW). Case-insensitive.</param>
    /// <returns>The latest METAR observation for the airport</returns>
    /// <response code="200">Returns the METAR observation</response>
    /// <response code="404">If no METAR is found for the airport</response>
    [HttpGet("{icaoCodeOrIdent}")]
    [ProducesResponseType(typeof(MetarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MetarDto>> GetMetarForAirport(string icaoCodeOrIdent, CancellationToken ct)
    {
        ValidationHelpers.ValidateRequiredString(icaoCodeOrIdent, "icaoCodeOrIdent", "ICAO code or identifier is required");
        var metar = await metarService.GetMetarForAirport(icaoCodeOrIdent, ct);
        return Ok(metar);
    }

    /// <summary>
    /// Gets the most recent METAR observations for multiple airports in a single request.
    /// Accepts ICAO codes or FAA identifiers. Identifiers that don't resolve to a METAR are silently skipped.
    /// </summary>
    /// <remarks>
    /// Both ICAO codes and FAA identifiers can be mixed in the same request.
    /// Maximum 100 identifiers per request.
    /// <code>
    /// GET /api/v1/metars/batch?ids=KDFW,KAUS,KHOU
    /// GET /api/v1/metars/batch?ids=DFW,AUS
    /// </code>
    /// </remarks>
    /// <param name="ids">Comma-separated ICAO codes or FAA identifiers (e.g., <c>KDFW,KAUS,KHOU</c>). Maximum 100.</param>
    /// <returns>METAR observations for the requested airports</returns>
    /// <response code="200">Returns the METAR observations</response>
    /// <response code="400">If the ids parameter is empty or exceeds 100 identifiers</response>
    [HttpGet("batch")]
    [ProducesResponseType(typeof(IEnumerable<MetarDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<MetarDto>>> GetMetarsBatch(
        [FromQuery] string ids,
        CancellationToken ct)
    {
        ValidationHelpers.ValidateRequiredString(ids, "ids", "At least one ICAO code or identifier is required");

        var codesArray = ids.Split(',')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToArray();

        ValidationHelpers.ValidateBatchSize(codesArray.Length, 100, "ids");

        var metars = await metarService.GetMetarsForAirports(codesArray, ct);
        return Ok(metars);
    }

    /// <summary>
    /// Gets METARs for airports in one or more states
    /// </summary>
    /// <remarks>
    /// Pass state codes as a single comma-separated query parameter:
    /// <code>
    /// GET /api/v1/metars?state=TX         — METARs for Texas airports
    /// GET /api/v1/metars?state=TX,OK,LA   — METARs for multiple states
    /// </code>
    /// </remarks>
    /// <param name="state">Comma-separated two-letter state codes (e.g., TX or TX,OK,LA)</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of METARs for airports in the specified states</returns>
    /// <response code="200">Returns the paginated METARs</response>
    /// <response code="400">If the state parameter is empty</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<MetarDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<MetarDto>>> GetMetarsByState(
        [FromQuery] string state,
        [FromQuery] PaginationParams pagination,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(state))
            throw new ValidationException("state", "At least one state code is required");

        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        var stateCodeArray = state.Split(',')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToArray();

        return Ok(await metarService.GetMetarsByStates(stateCodeArray, pagination.Cursor, pagination.Limit, ct));
    }
}
