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
        /// Gets all airports with optional text search
        /// </summary>
        /// <param name="search">Optional search term to filter by name, identifier, or city</param>
        /// <param name="pagination">Cursor-based pagination parameters</param>
        /// <returns>Paginated list of airports</returns>
        /// <response code="200">Returns the paginated airports</response>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResponse<AirportDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResponse<AirportDto>>> GetAllAirports(
            [FromQuery] string? search,
            [FromQuery] PaginationParams pagination)
        {
            pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
            return Ok(await airportService.GetAllAirports(search, pagination.Cursor, pagination.Limit));
        }

        /// <summary>
        /// Searches airports by name, identifier, or city
        /// </summary>
        /// <param name="query">Search query (minimum 2 characters)</param>
        /// <returns>Matching airports</returns>
        /// <response code="200">Returns matching airports</response>
        /// <response code="400">If the query is less than 2 characters</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<AirportDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<AirportDto>>> SearchAirports([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                throw new ValidationException("query", "Search query must be at least 2 characters");

            var airports = await airportService.SearchAirports(query);
            return Ok(airports);
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
        /// Gets airports in a specific state
        /// </summary>
        /// <param name="stateCode">Two-letter state code (e.g., TX, CA)</param>
        /// <param name="pagination">Cursor-based pagination parameters</param>
        /// <returns>Paginated list of airports in the state</returns>
        /// <response code="200">Returns the paginated airports</response>
        [HttpGet("state/{stateCode}")]
        [ProducesResponseType(typeof(PaginatedResponse<AirportDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResponse<AirportDto>>> GetAirportsByState(
            string stateCode,
            [FromQuery] PaginationParams pagination)
        {
            pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
            return Ok(await airportService.GetAirportsByState(stateCode, pagination.Cursor, pagination.Limit));
        }

        /// <summary>
        /// Gets airports across multiple states
        /// </summary>
        /// <remarks>
        /// Pass state codes as a single comma-separated query parameter:
        /// <code>GET /api/v1/airports/by-states?stateCodes=TX,OK,LA</code>
        /// </remarks>
        /// <param name="stateCodes">Comma-separated two-letter state codes (e.g., TX,OK,LA). Must contain at least one code.</param>
        /// <param name="pagination">Cursor-based pagination parameters</param>
        /// <returns>Paginated list of airports in the specified states</returns>
        /// <response code="200">Returns the paginated airports</response>
        /// <response code="400">If the state codes parameter is empty</response>
        [HttpGet("by-states")]
        [ProducesResponseType(typeof(PaginatedResponse<AirportDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PaginatedResponse<AirportDto>>> GetAirportsByStates(
            [FromQuery] string stateCodes,
            [FromQuery] PaginationParams pagination)
        {
            if (string.IsNullOrWhiteSpace(stateCodes))
                throw new ValidationException("stateCodes", "State codes are required");

            pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
            var stateCodeArray = stateCodes.Split(',')
                .Select(s => s.Trim())
                .ToArray();

            return Ok(await airportService.GetAirportsByStates(stateCodeArray, pagination.Cursor, pagination.Limit));
        }

        /// <summary>
        /// Gets multiple airports by their ICAO codes or identifiers
        /// </summary>
        /// <remarks>
        /// Pass ICAO codes or FAA identifiers as a single comma-separated query parameter:
        /// <code>GET /api/v1/airports/batch?icaoCodesOrIdents=KDFW,KAUS,KHOU</code>
        /// Both ICAO codes (KDFW) and FAA identifiers (DFW) can be mixed in the same request.
        /// </remarks>
        /// <param name="icaoCodesOrIdents">Comma-separated ICAO codes or FAA identifiers (e.g., KDFW,KAUS,KHOU). Must contain at least one code.</param>
        /// <returns>Airports matching the specified codes</returns>
        /// <response code="200">Returns the matching airports</response>
        /// <response code="400">If the ICAO codes or identifiers parameter is empty</response>
        [HttpGet("batch")]
        [ProducesResponseType(typeof(IEnumerable<AirportDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<AirportDto>>> GetAirportsByIcaoCodesOrIdents(
            [FromQuery] string icaoCodesOrIdents)
        {
            if (string.IsNullOrWhiteSpace(icaoCodesOrIdents))
                throw new ValidationException("icaoCodesOrIdents", "ICAO codes or identifiers are required");

            var codesArray = icaoCodesOrIdents.Split(',')
                .Select(s => s.Trim())
                .ToArray();

            var airports = await airportService.GetAirportsByIcaoCodesOrIdents(codesArray);
            return Ok(airports);
        }

        /// <summary>
        /// Gets airports whose identifier starts with a prefix
        /// </summary>
        /// <param name="prefix">Identifier prefix to match (e.g., KDF)</param>
        /// <param name="pagination">Cursor-based pagination parameters</param>
        /// <returns>Paginated list of airports with matching identifiers</returns>
        /// <response code="200">Returns the paginated airports</response>
        [HttpGet("prefix/{prefix}")]
        [ProducesResponseType(typeof(PaginatedResponse<AirportDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResponse<AirportDto>>> GetAirportsByPrefix(
            string prefix,
            [FromQuery] PaginationParams pagination)
        {
            pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
            return Ok(await airportService.GetAirportsByPrefix(prefix, pagination.Cursor, pagination.Limit));
        }

        /// <summary>
        /// Gets runways for a specific airport, including dimensions, surface type, lighting,
        /// and detailed runway end information (approach types, markings, obstacles).
        /// Runway heading data can be used with the Performance crosswind calculator endpoint.
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
