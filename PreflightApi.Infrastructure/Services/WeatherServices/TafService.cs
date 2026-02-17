using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Mappers;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Infrastructure.Services.WeatherServices;

public class TafService : ITafService
{
    private readonly PreflightApiDbContext _dbContext;
    private readonly ILogger<TafService> _logger;

    public TafService(PreflightApiDbContext dbContext, ILogger<TafService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<TafDto> GetTafByIcaoCode(string icaoCodeOrIdent)
    {
        var taf = await _dbContext.Tafs.FirstOrDefaultAsync(t => t.StationId == icaoCodeOrIdent.ToUpperInvariant());

        if (taf == null)
        {
            var airport = await _dbContext.Airports
                .FirstOrDefaultAsync(a => a.ArptId == icaoCodeOrIdent.ToUpperInvariant() ||
                                          a.IcaoId == icaoCodeOrIdent.ToUpperInvariant());

            if (airport == null)
            {
                throw new AirportNotFoundException(icaoCodeOrIdent);
            }

            var modifiedIdent = airport.StateCode switch
            {
                "AK" or "HI" => $"P{airport.ArptId}",
                _ => $"K{airport.ArptId}"
            };

            taf = await _dbContext.Tafs
                .FirstOrDefaultAsync(t => t.StationId == modifiedIdent);
        }
        
        if (taf == null)
        {
            throw new TafNotFoundException(icaoCodeOrIdent);
        }

        return TafMapper.ToDto(taf);
    }

    public async Task<IEnumerable<TafDto>> GetTafsForAirports(string[] icaoCodesOrIdents)
    {
        var upperCodes = icaoCodesOrIdents
            .Select(c => c.ToUpperInvariant())
            .Distinct()
            .ToList();

        // Query 1: Direct StationId matches
        var directMatches = await _dbContext.Tafs
            .Where(t => t.StationId != null && upperCodes.Contains(t.StationId))
            .ToListAsync();

        var matchedStationIds = directMatches
            .Where(t => t.StationId != null)
            .Select(t => t.StationId!)
            .ToHashSet();

        // Identify input codes that didn't match any StationId directly
        var unmatchedCodes = upperCodes
            .Where(c => !matchedStationIds.Contains(c))
            .ToList();

        if (unmatchedCodes.Count == 0)
            return directMatches.Select(TafMapper.ToDto);

        // Query 2: Resolve unmatched codes via Airports table
        var airports = await _dbContext.Airports
            .Where(a => unmatchedCodes.Contains(a.IcaoId!) || unmatchedCodes.Contains(a.ArptId!))
            .ToListAsync();

        var resolvedStationIds = airports
            .Select(a => a.StateCode switch
            {
                "AK" or "HI" => $"P{a.ArptId}",
                _ => $"K{a.ArptId}"
            })
            .Where(id => !matchedStationIds.Contains(id))
            .Distinct()
            .ToList();

        if (resolvedStationIds.Count == 0)
            return directMatches.Select(TafMapper.ToDto);

        // Query 3: Fetch TAFs for resolved station IDs
        var resolvedMatches = await _dbContext.Tafs
            .Where(t => t.StationId != null && resolvedStationIds.Contains(t.StationId))
            .ToListAsync();

        return directMatches.Concat(resolvedMatches).Select(TafMapper.ToDto);
    }
}