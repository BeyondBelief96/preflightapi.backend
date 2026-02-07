using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/chart-supplements")]
public class ChartSupplementController(IChartSupplementService chartSupplementService) : ControllerBase
{
    /// <summary>
    /// Gets pre-signed URLs for chart supplements by ICAO code or identifier
    /// </summary>
    /// <param name="icaoCodeOrIdent">ICAO code or airport identifier</param>
    /// <returns>Chart supplement metadata and pre-signed URLs for all supplement PDFs</returns>
    /// <response code="200">Returns the chart supplements for the airport</response>
    /// <response code="404">If no chart supplements are found</response>
    [HttpGet("{icaoCodeOrIdent}")]
    [ProducesResponseType(typeof(ChartSupplementsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChartSupplementsResponseDto>> GetChartSupplements(string icaoCodeOrIdent)
    {
        var supplements = await chartSupplementService.GetChartSupplementsByAirportCode(icaoCodeOrIdent);
        return Ok(supplements);
    }
}
