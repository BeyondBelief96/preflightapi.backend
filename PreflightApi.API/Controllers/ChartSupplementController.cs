using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides access to FAA Chart Supplement (formerly Airport/Facility Directory) PDFs.
/// Chart supplements contain comprehensive airport information including detailed runway data,
/// lighting, available services, airspace, and other operational details.
/// PDFs are returned as time-limited pre-signed URLs.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/chart-supplements")]
[Tags("Chart Supplements")]
public class ChartSupplementController(IChartSupplementService chartSupplementService) : ControllerBase
{
    /// <summary>
    /// Gets time-limited pre-signed URLs for all chart supplement pages for an airport.
    /// Multi-page supplements will have one URL per page. The URLs expire after a limited period;
    /// request new URLs if they have expired.
    /// </summary>
    /// <param name="icaoCodeOrIdent">ICAO code or FAA identifier (e.g., KDFW, DFW)</param>
    /// <returns>Airport metadata and pre-signed URLs for each chart supplement PDF page</returns>
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
