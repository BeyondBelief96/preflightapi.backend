using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides access to controlled airspace (Class B, C, D) and special use airspace
/// (restricted, prohibited, warning, MOA, alert) data sourced from FAA ArcGIS services.
/// Each airspace includes boundary geometry (GeoJSON), altitude limits, and classification.
/// Airspace GlobalIds are returned by the navigation log endpoint for airspaces along a planned route —
/// use the by-global-ids endpoints to retrieve full details for those airspaces.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/airspaces")]
[Tags("Airspace")]
public class AirspaceController(IAirspaceService airspaceService)
    : ControllerBase
{
    /// <summary>
    /// Gets controlled airspaces filtered by airspace class (B, C, or D).
    /// </summary>
    /// <remarks>
    /// <code>
    /// GET /api/v1/airspaces/by-classes?classes=B        — all Class B airspaces
    /// GET /api/v1/airspaces/by-classes?classes=B,C,D    — Class B, C, and D airspaces
    /// </code>
    /// </remarks>
    /// <param name="classes">Comma-separated airspace classes: <c>B</c>, <c>C</c>, or <c>D</c></param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of airspaces matching the specified classes</returns>
    /// <response code="200">Returns the paginated airspaces</response>
    /// <response code="400">If the classes parameter is empty</response>
    [HttpGet("by-classes")]
    [ProducesResponseType(typeof(PaginatedResponse<AirspaceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<AirspaceDto>>> GetByClasses(
        [FromQuery] string classes,
        [FromQuery] PaginationParams pagination)
    {
        if (string.IsNullOrWhiteSpace(classes))
            throw new ValidationException("classes", "Airspace classes are required");

        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        var classArray = classes.Split(',').Select(c => c.Trim()).ToArray();
        return Ok(await airspaceService.GetByClasses(classArray, pagination.Cursor, pagination.Limit));
    }

    /// <summary>
    /// Gets controlled airspaces filtered by city name.
    /// </summary>
    /// <remarks>
    /// <code>
    /// GET /api/v1/airspaces/by-cities?cities=Dallas          — airspaces for Dallas
    /// GET /api/v1/airspaces/by-cities?cities=Dallas,Houston   — multiple cities
    /// </code>
    /// </remarks>
    /// <param name="cities">Comma-separated city names (e.g., <c>Dallas</c> or <c>Dallas,Houston</c>)</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of airspaces in the specified cities</returns>
    /// <response code="200">Returns the paginated airspaces</response>
    /// <response code="400">If the cities parameter is empty</response>
    [HttpGet("by-cities")]
    [ProducesResponseType(typeof(PaginatedResponse<AirspaceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<AirspaceDto>>> GetByCity(
        [FromQuery] string cities,
        [FromQuery] PaginationParams pagination)
    {
        if (string.IsNullOrWhiteSpace(cities))
            throw new ValidationException("cities", "Cities are required");

        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        var cityArray = cities.Split(',').Select(c => c.Trim()).ToArray();
        return Ok(await airspaceService.GetByCities(cityArray, pagination.Cursor, pagination.Limit));
    }

    /// <summary>
    /// Gets controlled airspaces filtered by two-letter state code.
    /// </summary>
    /// <remarks>
    /// <code>
    /// GET /api/v1/airspaces/by-states?states=TX       — airspaces in Texas
    /// GET /api/v1/airspaces/by-states?states=TX,OK    — airspaces in Texas and Oklahoma
    /// </code>
    /// </remarks>
    /// <param name="states">Comma-separated two-letter state codes (e.g., <c>TX</c> or <c>TX,OK</c>)</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of airspaces in the specified states</returns>
    /// <response code="200">Returns the paginated airspaces</response>
    /// <response code="400">If the states parameter is empty</response>
    [HttpGet("by-states")]
    [ProducesResponseType(typeof(PaginatedResponse<AirspaceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<AirspaceDto>>> GetByState(
        [FromQuery] string states,
        [FromQuery] PaginationParams pagination)
    {
        if (string.IsNullOrWhiteSpace(states))
            throw new ValidationException("states", "States are required");

        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        var stateArray = states.Split(',').Select(s => s.Trim()).ToArray();
        return Ok(await airspaceService.GetByStates(stateArray, pagination.Cursor, pagination.Limit));
    }

    /// <summary>
    /// Gets special use airspaces filtered by type code.
    /// </summary>
    /// <remarks>
    /// <para><strong>Type Codes</strong></para>
    /// <list type="bullet">
    ///   <item><description><c>R</c> — Restricted</description></item>
    ///   <item><description><c>P</c> — Prohibited</description></item>
    ///   <item><description><c>W</c> — Warning</description></item>
    ///   <item><description><c>A</c> — Alert</description></item>
    ///   <item><description><c>M</c> — MOA (Military Operations Area)</description></item>
    /// </list>
    /// <code>
    /// GET /api/v1/airspaces/special-use/by-type-codes?typeCodes=R,P    — restricted and prohibited
    /// GET /api/v1/airspaces/special-use/by-type-codes?typeCodes=M      — MOAs only
    /// </code>
    /// </remarks>
    /// <param name="typeCodes">Comma-separated type codes: <c>R</c> (restricted), <c>P</c> (prohibited), <c>W</c> (warning), <c>A</c> (alert), <c>M</c> (MOA)</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of special use airspaces matching the type codes</returns>
    /// <response code="200">Returns the paginated special use airspaces</response>
    /// <response code="400">If the type codes parameter is empty</response>
    [HttpGet("special-use/by-type-codes")]
    [ProducesResponseType(typeof(PaginatedResponse<SpecialUseAirspaceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<SpecialUseAirspaceDto>>> GetByTypeCode(
        [FromQuery] string typeCodes,
        [FromQuery] PaginationParams pagination)
    {
        if (string.IsNullOrWhiteSpace(typeCodes))
            throw new ValidationException("typeCodes", "Type codes are required");

        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        var typeCodeArray = typeCodes.Split(',').Select(tc => tc.Trim()).ToArray();
        return Ok(await airspaceService.GetByTypeCodes(typeCodeArray, pagination.Cursor, pagination.Limit));
    }

    /// <summary>
    /// Gets controlled airspaces associated with specific airports by ICAO code or FAA identifier.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns all controlled airspace boundaries (Class B, C, D) associated with the given airport
    /// identifiers. Each result includes the airspace classification, altitude limits, and boundary
    /// geometry. Pass multiple identifiers as a comma-separated list to retrieve airspaces for
    /// several airports in a single request.
    /// </para>
    /// </remarks>
    /// <param name="icaoOrIdents">Comma-separated ICAO codes or FAA identifiers (e.g., <c>KDFW,KORD,KJFK</c>)</param>
    /// <returns>All controlled airspaces matching the specified identifiers</returns>
    /// <response code="200">Returns the matching airspaces</response>
    /// <response code="400">The identifiers parameter is empty</response>
    [HttpGet("by-icao-or-idents")]
    [ProducesResponseType(typeof(IEnumerable<AirspaceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<AirspaceDto>>> GetByIcaoOrIdent([FromQuery] string icaoOrIdents)
    {
        if (string.IsNullOrWhiteSpace(icaoOrIdents))
            throw new ValidationException("icaoOrIdents", "ICAO codes or identifiers are required");

        var idArray = icaoOrIdents.Split(',').Select(id => id.Trim()).ToArray();
        var airspaces = await airspaceService.GetByIcaoOrIdents(idArray);
        return Ok(airspaces);
    }

    /// <summary>
    /// Gets controlled airspaces by their global IDs. This endpoint is designed to be used with the
    /// AirspaceGlobalIds returned by the navigation log endpoint (<c>POST /api/v1/navlog/calculate</c>)
    /// to retrieve full details for airspaces along a planned route.
    /// </summary>
    /// <remarks>
    /// <code>
    /// GET /api/v1/airspaces/by-global-ids?globalIds={guid1},{guid2}
    /// </code>
    /// </remarks>
    /// <param name="globalIds">Comma-separated global IDs (GUIDs from the navlog response's <c>AirspaceGlobalIds</c> field)</param>
    /// <returns>Airspaces matching the specified global IDs with full boundary geometry and altitude data</returns>
    /// <response code="200">Returns the matching airspaces</response>
    /// <response code="400">If the global IDs parameter is empty</response>
    [HttpGet("by-global-ids")]
    [ProducesResponseType(typeof(IEnumerable<AirspaceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<AirspaceDto>>> GetByGlobalIds([FromQuery] string globalIds)
    {
        if (string.IsNullOrWhiteSpace(globalIds))
            throw new ValidationException("globalIds", "Global IDs are required");

        var idArray = globalIds.Split(',').Select(id => id.Trim()).ToArray();
        var airspaces = await airspaceService.GetByGlobalIds(idArray);
        return Ok(airspaces);
    }

    /// <summary>
    /// Gets special use airspaces by their global IDs. This endpoint is designed to be used with the
    /// SpecialUseAirspaceGlobalIds returned by the navigation log endpoint (<c>POST /api/v1/navlog/calculate</c>)
    /// to retrieve full details for special use airspaces along a planned route.
    /// </summary>
    /// <remarks>
    /// <code>
    /// GET /api/v1/airspaces/special-use/by-global-ids?globalIds={guid1},{guid2}
    /// </code>
    /// </remarks>
    /// <param name="globalIds">Comma-separated global IDs (GUIDs from the navlog response's <c>SpecialUseAirspaceGlobalIds</c> field)</param>
    /// <returns>Special use airspaces matching the specified global IDs with full boundary geometry and altitude data</returns>
    /// <response code="200">Returns the matching special use airspaces</response>
    /// <response code="400">If the global IDs parameter is empty</response>
    [HttpGet("special-use/by-global-ids")]
    [ProducesResponseType(typeof(IEnumerable<SpecialUseAirspaceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<SpecialUseAirspaceDto>>> GetSpecialUseByGlobalIds([FromQuery] string globalIds)
    {
        if (string.IsNullOrWhiteSpace(globalIds))
            throw new ValidationException("globalIds", "Global IDs are required");

        var idArray = globalIds.Split(',').Select(id => id.Trim()).ToArray();
        var airspaces = await airspaceService.GetSpecialUseByGlobalIds(idArray);
        return Ok(airspaces);
    }
}
