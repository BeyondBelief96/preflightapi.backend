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
[Route("api/v{version:apiVersion}/navaids")]
[Tags("Navigational Aids")]
public class NavigationalAidController(INavigationalAidService navAidService)
    : ControllerBase
{
    /// <summary>
    /// Gets all navigational aids with optional search filter
    /// </summary>
    /// <param name="search">Optional search by NAVAID identifier or name</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of navigational aids</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<NavigationalAidDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<NavigationalAidDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] PaginationParams pagination)
    {
        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await navAidService.GetAllAsync(search, pagination.Cursor, pagination.Limit));
    }

    /// <summary>
    /// Gets navigational aids by identifier
    /// </summary>
    /// <param name="identifier">NAVAID identifier (e.g., DFW, BUJ)</param>
    /// <returns>List of navigational aids matching the identifier</returns>
    [HttpGet("{identifier}")]
    [ProducesResponseType(typeof(IEnumerable<NavigationalAidDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<NavigationalAidDto>>> GetByIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ValidationException("identifier", "NAVAID identifier is required");

        return Ok(await navAidService.GetByIdentifierAsync(identifier));
    }

    /// <summary>
    /// Gets navigational aids by facility type
    /// </summary>
    /// <param name="facilityType">Facility type (e.g., VOR, VORTAC, NDB, TACAN, DME)</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of navigational aids of the specified type</returns>
    [HttpGet("type/{facilityType}")]
    [ProducesResponseType(typeof(PaginatedResponse<NavigationalAidDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<NavigationalAidDto>>> GetByType(
        string facilityType,
        [FromQuery] PaginationParams pagination)
    {
        if (string.IsNullOrWhiteSpace(facilityType))
            throw new ValidationException("facilityType", "Facility type is required");

        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await navAidService.GetByTypeAsync(facilityType, pagination.Cursor, pagination.Limit));
    }

    /// <summary>
    /// Gets navigational aids by state
    /// </summary>
    /// <param name="stateCode">Two-letter state code (e.g., TX, CA)</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of navigational aids in the specified state</returns>
    [HttpGet("state/{stateCode}")]
    [ProducesResponseType(typeof(PaginatedResponse<NavigationalAidDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<NavigationalAidDto>>> GetByState(
        string stateCode,
        [FromQuery] PaginationParams pagination)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            throw new ValidationException("stateCode", "State code is required");

        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await navAidService.GetByStateAsync(stateCode, pagination.Cursor, pagination.Limit));
    }
}
