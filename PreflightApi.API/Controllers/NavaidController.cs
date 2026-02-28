using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.API.Utilities;
using PreflightApi.Domain.Enums;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Mappers;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides access to FAA NASR navigation aid (NAVAID) data including VOR, VORTAC, NDB, DME, and TACAN facilities.
/// Note: NavId is not globally unique — the same identifier can exist for different facility types.
/// The identifier lookup endpoint returns all matches.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/navaids")]
[Tags("Navaids")]
public class NavaidController(INavaidService navaidService) : ControllerBase
{
    /// <summary>
    /// Gets a paginated list of navaids with optional filtering.
    /// </summary>
    /// <remarks>
    /// <code>
    /// GET /api/v1/navaids                                    — all navaids (paginated)
    /// GET /api/v1/navaids?search=DFW                         — search by identifier, name, or city
    /// GET /api/v1/navaids?type=VOR                           — filter by facility type
    /// GET /api/v1/navaids?state=TX                           — filter by state
    /// GET /api/v1/navaids?search=Dallas&amp;type=VORTAC&amp;state=TX — combine filters
    /// </code>
    /// </remarks>
    /// <param name="search">Search across NavId (starts with), Name (contains), and City (contains)</param>
    /// <param name="type">Filter by navaid facility type (e.g., VOR, VORTAC, VOR/DME, NDB, NDB/DME, TACAN, DME)</param>
    /// <param name="state">Filter by two-letter state code (e.g., TX, CA)</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of navaids</returns>
    /// <response code="200">Returns the navaids</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<NavaidDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<NavaidDto>>> GetNavaids(
        [FromQuery] string? search = null,
        [FromQuery] string? type = null,
        [FromQuery] string? state = null,
        [FromQuery] PaginationParams? pagination = null)
    {
        pagination ??= new PaginationParams();
        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await navaidService.GetNavaids(search, type, state, pagination.Cursor, pagination.Limit));
    }

    /// <summary>
    /// Gets a paginated list of navaids filtered by facility type.
    /// </summary>
    /// <remarks>
    /// <code>
    /// GET /api/v1/navaids/type/Vor           — all VOR facilities
    /// GET /api/v1/navaids/type/Vortac        — all VORTAC facilities
    /// GET /api/v1/navaids/type/NdbDme        — all NDB/DME facilities
    /// </code>
    /// </remarks>
    /// <param name="type">NAVAID facility type enum value</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of navaids of the specified type</returns>
    /// <response code="200">Returns the navaids</response>
    [HttpGet("type/{type}")]
    [ProducesResponseType(typeof(PaginatedResponse<NavaidDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<NavaidDto>>> GetByType(
        NavaidType type,
        [FromQuery] PaginationParams? pagination = null)
    {
        pagination ??= new PaginationParams();
        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        var dbType = NavaidMapper.ToDbString(type);
        return Ok(await navaidService.GetNavaids(null, dbType, null, pagination.Cursor, pagination.Limit));
    }

    /// <summary>
    /// Gets all navaids matching an identifier. Because NavId is not globally unique
    /// (e.g., "DFW" can be both a VOR and an NDB), this endpoint returns a list.
    /// </summary>
    /// <remarks>
    /// <code>
    /// GET /api/v1/navaids/DFW     — returns all navaids with identifier "DFW"
    /// GET /api/v1/navaids/BIE     — returns all navaids with identifier "BIE"
    /// </code>
    /// </remarks>
    /// <param name="navId">NAVAID facility identifier (e.g., DFW, AUS, BIE)</param>
    /// <returns>List of navaids matching the identifier</returns>
    /// <response code="200">Returns the matching navaids</response>
    /// <response code="400">If the identifier is empty</response>
    /// <response code="404">If no navaids match the identifier</response>
    [HttpGet("{navId}")]
    [ProducesResponseType(typeof(IEnumerable<NavaidDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<NavaidDto>>> GetByIdentifier(string navId)
    {
        ValidationHelpers.ValidateRequiredString(navId, "navId", "NAVAID identifier is required");

        var navaids = await navaidService.GetNavaidsByIdentifier(navId);
        var result = navaids.ToList();

        if (result.Count == 0)
            throw new NavaidNotFoundException(navId);

        return Ok(result);
    }

    /// <summary>
    /// Gets navaids for multiple identifiers in a single request.
    /// </summary>
    /// <remarks>
    /// <code>
    /// GET /api/v1/navaids/batch?ids=DFW,BIE,AUS
    /// </code>
    /// </remarks>
    /// <param name="ids">Comma-separated list of NAVAID identifiers (maximum 100)</param>
    /// <returns>List of navaids matching any of the provided identifiers</returns>
    /// <response code="200">Returns the matching navaids (may include multiple per identifier)</response>
    /// <response code="400">If the list is empty or exceeds 100 identifiers</response>
    [HttpGet("batch")]
    [ProducesResponseType(typeof(IEnumerable<NavaidDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<NavaidDto>>> GetBatch([FromQuery] string ids)
    {
        ValidationHelpers.ValidateRequiredString(ids, "ids", "At least one NAVAID identifier is required");

        var idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (idList.Length == 0)
            throw new ValidationException("ids", "At least one NAVAID identifier is required");

        ValidationHelpers.ValidateBatchSize(idList.Length, 100, "ids");

        return Ok(await navaidService.GetNavaidsByIdentifiers(idList));
    }

    /// <summary>
    /// Searches for navaids near a geographic point. Results are filtered by radius but not sorted by distance;
    /// pagination order is deterministic but arbitrary.
    /// </summary>
    /// <remarks>
    /// <code>
    /// GET /api/v1/navaids/nearby?lat=32.897&amp;lon=-97.038                     — default 30 NM radius
    /// GET /api/v1/navaids/nearby?lat=32.897&amp;lon=-97.038&amp;radiusNm=50&amp;type=VOR — VORs within 50 NM
    /// </code>
    /// </remarks>
    /// <param name="lat">Latitude in decimal degrees (-90 to 90)</param>
    /// <param name="lon">Longitude in decimal degrees (-180 to 180)</param>
    /// <param name="radiusNm">Search radius in nautical miles (default 30, must be greater than 0)</param>
    /// <param name="type">Optional navaid type filter (e.g., VOR, VORTAC, NDB)</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of navaids within the search radius</returns>
    /// <response code="200">Returns the navaids found</response>
    /// <response code="400">If coordinates or radius are invalid</response>
    [HttpGet("nearby")]
    [ProducesResponseType(typeof(PaginatedResponse<NavaidDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<NavaidDto>>> SearchNearby(
        [FromQuery] decimal lat,
        [FromQuery] decimal lon,
        [FromQuery] double radiusNm = 30,
        [FromQuery] string? type = null,
        [FromQuery] PaginationParams? pagination = null)
    {
        ValidationHelpers.ValidateCoordinates(lat, lon);
        ValidationHelpers.ValidateRadius(radiusNm, 500);

        pagination ??= new PaginationParams();
        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await navaidService.SearchNearby(lat, lon, radiusNm, type, pagination.Cursor, pagination.Limit));
    }
}
