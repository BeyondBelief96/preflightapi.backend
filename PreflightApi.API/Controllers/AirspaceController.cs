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
[Route("api/v{version:apiVersion}/airspaces")]
[Tags("Airspace")]
public class AirspaceController(IAirspaceService airspaceService)
    : ControllerBase
{
    /// <summary>
    /// Gets airspaces filtered by airspace classes
    /// </summary>
    /// <param name="classes">Comma-separated airspace classes (e.g., B,C,D)</param>
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
    /// Gets airspaces filtered by city names
    /// </summary>
    /// <param name="cities">Comma-separated city names</param>
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
    /// Gets airspaces filtered by state codes
    /// </summary>
    /// <param name="states">Comma-separated state codes (e.g., TX,OK)</param>
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
    /// Gets special use airspaces filtered by type codes
    /// </summary>
    /// <param name="typeCodes">Comma-separated type codes (e.g., R,P,W for restricted, prohibited, warning)</param>
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
    /// Gets airspaces by ICAO codes or FAA identifiers
    /// </summary>
    /// <param name="icaoOrIdents">Comma-separated ICAO codes or identifiers</param>
    /// <returns>Airspaces matching the specified identifiers</returns>
    /// <response code="200">Returns the matching airspaces</response>
    /// <response code="400">If the identifiers parameter is empty</response>
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
    /// Gets airspaces by their global IDs
    /// </summary>
    /// <param name="globalIds">Comma-separated global IDs</param>
    /// <returns>Airspaces matching the specified global IDs</returns>
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
    /// Gets special use airspaces by their global IDs
    /// </summary>
    /// <param name="globalIds">Comma-separated global IDs</param>
    /// <returns>Special use airspaces matching the specified global IDs</returns>
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
