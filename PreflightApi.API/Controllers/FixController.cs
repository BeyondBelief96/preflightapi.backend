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
[Route("api/v{version:apiVersion}/fixes")]
[Tags("Fixes")]
public class FixController(IFixService fixService)
    : ControllerBase
{
    /// <summary>
    /// Gets all fixes/reporting points with optional search filter
    /// </summary>
    /// <param name="search">Optional search by fix identifier</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of fixes</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<FixDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<FixDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] PaginationParams pagination)
    {
        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await fixService.GetAllAsync(search, pagination.Cursor, pagination.Limit));
    }

    /// <summary>
    /// Gets fixes by identifier
    /// </summary>
    /// <param name="identifier">Fix identifier</param>
    /// <returns>List of fixes matching the identifier</returns>
    [HttpGet("{identifier}")]
    [ProducesResponseType(typeof(IEnumerable<FixDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<FixDto>>> GetByIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ValidationException("identifier", "Fix identifier is required");

        return Ok(await fixService.GetByIdentifierAsync(identifier));
    }

    /// <summary>
    /// Gets fixes by state
    /// </summary>
    /// <param name="stateCode">Two-letter state code (e.g., TX, CA)</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of fixes in the specified state</returns>
    [HttpGet("state/{stateCode}")]
    [ProducesResponseType(typeof(PaginatedResponse<FixDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<FixDto>>> GetByState(
        string stateCode,
        [FromQuery] PaginationParams pagination)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            throw new ValidationException("stateCode", "State code is required");

        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await fixService.GetByStateAsync(stateCode, pagination.Cursor, pagination.Limit));
    }

    /// <summary>
    /// Gets fixes by use code
    /// </summary>
    /// <param name="useCode">Fix use code</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of fixes with the specified use code</returns>
    [HttpGet("use/{useCode}")]
    [ProducesResponseType(typeof(PaginatedResponse<FixDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<FixDto>>> GetByUseCode(
        string useCode,
        [FromQuery] PaginationParams pagination)
    {
        if (string.IsNullOrWhiteSpace(useCode))
            throw new ValidationException("useCode", "Use code is required");

        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await fixService.GetByUseCodeAsync(useCode, pagination.Cursor, pagination.Limit));
    }
}
