using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/metars")]
public class MetarController(IMetarService metarService) : ControllerBase
{
    [HttpGet("{icaoCodeOrIdent}")]
    [ProducesResponseType(typeof(MetarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MetarDto>> GetMetarForAirport(string icaoCodeOrIdent)
    {
        var metar = await metarService.GetMetarForAirport(icaoCodeOrIdent);
        return Ok(metar);
    }

    [HttpGet("state/{stateCode}")]
    [ProducesResponseType(typeof(PaginatedResponse<MetarDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<MetarDto>>> GetMetarsByState(
        string stateCode,
        [FromQuery] PaginationParams pagination)
    {
        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await metarService.GetMetarsByState(stateCode, pagination.Cursor, pagination.Limit));
    }

    [HttpGet("states/{stateCodes}")]
    [ProducesResponseType(typeof(PaginatedResponse<MetarDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<MetarDto>>> GetMetarsByStates(
        string stateCodes,
        [FromQuery] PaginationParams pagination)
    {
        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        var stateCodeArray = stateCodes.Split(',')
            .Select(s => s.Trim())
            .ToArray();

        return Ok(await metarService.GetMetarsByStates(stateCodeArray, pagination.Cursor, pagination.Limit));
    }
}
