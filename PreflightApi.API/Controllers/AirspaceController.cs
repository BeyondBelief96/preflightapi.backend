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
public class AirspaceController(IAirspaceService airspaceService)
    : ControllerBase
{
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
