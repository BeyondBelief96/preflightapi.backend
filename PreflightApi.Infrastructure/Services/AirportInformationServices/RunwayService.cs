using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Mappers;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Infrastructure.Services.AirportInformationServices;

public class RunwayService : IRunwayService
{
    private readonly PreflightApiDbContext _context;
    private readonly ILogger<RunwayService> _logger;

    public RunwayService(
        PreflightApiDbContext context,
        ILogger<RunwayService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<RunwayDto>> GetRunwaysByAirportAsync(string icaoCodeOrIdent)
    {
        try
        {
            _logger.LogInformation("Getting runways for airport: {IcaoCodeOrIdent}", icaoCodeOrIdent);

            // First find the airport to get its SiteNo
            var airport = await _context.Airports
                .FirstOrDefaultAsync(a =>
                    a.IcaoId == icaoCodeOrIdent.ToUpperInvariant() ||
                    a.ArptId == icaoCodeOrIdent.ToUpperInvariant());

            if (airport == null)
            {
                throw new AirportNotFoundException(icaoCodeOrIdent);
            }

            // Get all runways for this airport with their runway ends
            var runways = await _context.Runways
                .Include(r => r.RunwayEnds)
                .Where(r => r.SiteNo == airport.SiteNo)
                .OrderBy(r => r.RunwayId)
                .ToListAsync();

            return runways.Select(RunwayMapper.ToDto);
        }
        catch (Exception ex) when (ex is not AirportNotFoundException)
        {
            _logger.LogError(ex, "Error getting runways for airport: {IcaoCodeOrIdent}", icaoCodeOrIdent);
            throw;
        }
    }
}
