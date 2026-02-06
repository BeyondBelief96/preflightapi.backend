using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/airports")]
    public class AirportController(
        IAirportService airportService,
        IRunwayService runwayService)
        : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResponse<AirportDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResponse<AirportDto>>> GetAllAirports(
            [FromQuery] string? search,
            [FromQuery] PaginationParams pagination)
        {
            pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
            return Ok(await airportService.GetAllAirports(search, pagination.Cursor, pagination.Limit));
        }

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

        [HttpGet("{icaoCodeOrIdent}")]
        [ProducesResponseType(typeof(AirportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AirportDto>> GetAirportByIcaoCodeOrIdent(string icaoCodeOrIdent)
        {
            var airport = await airportService.GetAirportByIcaoCodeOrIdent(icaoCodeOrIdent);
            return Ok(airport);
        }

        [HttpGet("state/{stateCode}")]
        [ProducesResponseType(typeof(PaginatedResponse<AirportDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResponse<AirportDto>>> GetAirportsByState(
            string stateCode,
            [FromQuery] PaginationParams pagination)
        {
            pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
            return Ok(await airportService.GetAirportsByState(stateCode, pagination.Cursor, pagination.Limit));
        }

        [HttpGet("states/{stateCodes}")]
        [ProducesResponseType(typeof(PaginatedResponse<AirportDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResponse<AirportDto>>> GetAirportsByStates(
            string stateCodes,
            [FromQuery] PaginationParams pagination)
        {
            pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
            var stateCodeArray = stateCodes.Split(',')
                .Select(s => s.Trim())
                .ToArray();

            return Ok(await airportService.GetAirportsByStates(stateCodeArray, pagination.Cursor, pagination.Limit));
        }

        [HttpGet("batch/{icaoCodesOrIdents}")]
        [ProducesResponseType(typeof(IEnumerable<AirportDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<AirportDto>>> GetAirportsByIcaoCodesOrIdents(string icaoCodesOrIdents)
        {
            var codesArray = icaoCodesOrIdents.Split(',')
                .Select(s => s.Trim())
                .ToArray();

            var airports = await airportService.GetAirportsByIcaoCodesOrIdents(codesArray);
            return Ok(airports);
        }

        [HttpGet("prefix/{prefix}")]
        [ProducesResponseType(typeof(PaginatedResponse<AirportDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResponse<AirportDto>>> GetAirportsByPrefix(
            string prefix,
            [FromQuery] PaginationParams pagination)
        {
            pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
            return Ok(await airportService.GetAirportsByPrefix(prefix, pagination.Cursor, pagination.Limit));
        }

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
