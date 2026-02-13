using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers
{
    /// <summary>
    /// Provides access to FAA airport data from the National Airspace System Resources (NASR) database.
    /// Airports can be queried by ICAO code, FAA identifier, state, or text search. Use an airport's
    /// ICAO code or identifier to query related data from other endpoints such as METARs, TAFs, runways,
    /// communication frequencies, airport diagrams, and chart supplements.
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/airports")]
    [Tags("Airports")]
    public class AirportController(
        IAirportService airportService,
        IRunwayService runwayService)
        : ControllerBase
    {
        /// <summary>
        /// Lists airports with optional search and state filtering
        /// </summary>
        /// <remarks>
        /// Supports combinable query parameters for flexible filtering:
        /// <code>
        /// GET /api/v1/airports                           — all airports (paginated)
        /// GET /api/v1/airports?search=Dallas             — text search across name, city, ICAO, and FAA identifier
        /// GET /api/v1/airports?state=TX                  — airports in Texas
        /// GET /api/v1/airports?state=TX,OK,LA            — airports in multiple states
        /// GET /api/v1/airports?search=Regional&amp;state=TX  — combined search + state filter
        /// </code>
        /// </remarks>
        /// <param name="search">Optional text search across airport name, city, ICAO code, and FAA identifier</param>
        /// <param name="state">Optional comma-separated two-letter state codes (e.g., TX or TX,OK,LA)</param>
        /// <param name="pagination">Cursor-based pagination parameters</param>
        /// <returns>Paginated list of airports</returns>
        /// <response code="200">Returns the paginated airports</response>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResponse<AirportDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResponse<AirportDto>>> GetAirports(
            [FromQuery] string? search,
            [FromQuery] string? state,
            [FromQuery] PaginationParams pagination)
        {
            pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);

            string[]? stateCodes = null;
            if (!string.IsNullOrWhiteSpace(state))
            {
                stateCodes = state.Split(',')
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .ToArray();
            }

            return Ok(await airportService.GetAirports(search, stateCodes, pagination.Cursor, pagination.Limit));
        }

        /// <summary>
        /// Gets a specific airport by ICAO code or FAA identifier
        /// </summary>
        /// <param name="icaoCodeOrIdent">ICAO code or FAA identifier (e.g., KDFW, DFW)</param>
        /// <returns>The airport details</returns>
        /// <response code="200">Returns the airport</response>
        /// <response code="404">If the airport is not found</response>
        [HttpGet("{icaoCodeOrIdent}")]
        [ProducesResponseType(typeof(AirportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AirportDto>> GetAirportByIcaoCodeOrIdent(string icaoCodeOrIdent)
        {
            var airport = await airportService.GetAirportByIcaoCodeOrIdent(icaoCodeOrIdent);
            return Ok(airport);
        }

        /// <summary>
        /// Gets multiple airports by their ICAO codes or FAA identifiers
        /// </summary>
        /// <remarks>
        /// Pass ICAO codes or FAA identifiers as a single comma-separated query parameter:
        /// <code>GET /api/v1/airports/batch?ids=KDFW,KAUS,KHOU</code>
        /// Both ICAO codes (KDFW) and FAA identifiers (DFW) can be mixed in the same request.
        /// </remarks>
        /// <param name="ids">Comma-separated ICAO codes or FAA identifiers (e.g., KDFW,KAUS,KHOU)</param>
        /// <returns>Airports matching the specified codes</returns>
        /// <response code="200">Returns the matching airports</response>
        /// <response code="400">If the ids parameter is empty</response>
        [HttpGet("batch")]
        [ProducesResponseType(typeof(IEnumerable<AirportDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<AirportDto>>> GetAirportsBatch(
            [FromQuery] string ids)
        {
            if (string.IsNullOrWhiteSpace(ids))
                throw new ValidationException("ids", "At least one ICAO code or identifier is required");

            var codesArray = ids.Split(',')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToArray();

            var airports = await airportService.GetAirportsByIcaoCodesOrIdents(codesArray);
            return Ok(airports);
        }

        /// <summary>
        /// Gets runways for a specific airport, including dimensions, surface type, lighting,
        /// and detailed runway end information (approach types, markings, obstacles).
        /// Runway heading data can be used with the E6B crosswind calculator endpoint.
        /// </summary>
        /// <param name="icaoCodeOrIdent">ICAO code or FAA identifier (e.g., KDFW, DFW)</param>
        /// <returns>Runways and runway end details for the airport</returns>
        /// <response code="200">Returns the airport's runways</response>
        /// <response code="404">If the airport is not found</response>
        [HttpGet("{icaoCodeOrIdent}/runways")]
        [ProducesResponseType(typeof(IEnumerable<RunwayDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<RunwayDto>>> GetRunwaysByAirport(string icaoCodeOrIdent)
        {
            var runways = await runwayService.GetRunwaysByAirportAsync(icaoCodeOrIdent);
            return Ok(runways);
        }
    }
}
