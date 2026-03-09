using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Mappers;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Utilities;

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
    
    public async Task<TafDto> GetTafByIcaoCode(string icaoCodeOrIdent, CancellationToken ct = default)
    {
        var candidates = AirportIdentifierResolver.GetCandidateIdentifiers(icaoCodeOrIdent);

        var taf = await _dbContext.Tafs
            .FirstOrDefaultAsync(t => t.StationId != null && candidates.Contains(t.StationId), ct);

        if (taf == null)
        {
            var airport = await _dbContext.Airports
                .FirstOrDefaultAsync(a =>
                    (a.IcaoId != null && candidates.Contains(a.IcaoId)) ||
                    (a.ArptId != null && candidates.Contains(a.ArptId)), ct);

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
                .FirstOrDefaultAsync(t => t.StationId == modifiedIdent, ct);
        }
        
        if (taf == null)
        {
            throw new TafNotFoundException(icaoCodeOrIdent);
        }

        return TafMapper.ToDto(taf);
    }

    public async Task<IEnumerable<TafDto>> GetTafsForAirports(string[] icaoCodesOrIdents, CancellationToken ct = default)
    {
        if (icaoCodesOrIdents.Length > 100)
            throw new ValidationException("ids", "Maximum of 100 identifiers allowed per batch request");

        var expandedCodes = AirportIdentifierResolver.ExpandCandidates(icaoCodesOrIdents);

        // Query 1: Direct StationId matches (using expanded candidates)
        var directMatches = await _dbContext.Tafs
            .AsNoTracking()
            .Where(t => t.StationId != null && expandedCodes.Contains(t.StationId))
            .ToListAsync(ct);

        var matchedStationIds = directMatches
            .Where(t => t.StationId != null)
            .Select(t => t.StationId!)
            .ToHashSet();

        // Identify expanded candidates that didn't match any StationId directly
        var unmatchedCandidates = expandedCodes
            .Where(c => !matchedStationIds.Contains(c))
            .ToList();

        if (unmatchedCandidates.Count == 0)
            return directMatches.Select(TafMapper.ToDto);

        // Query 2: Resolve unmatched codes via Airports table
        var airports = await _dbContext.Airports
            .AsNoTracking()
            .Where(a => (a.IcaoId != null && unmatchedCandidates.Contains(a.IcaoId)) ||
                        (a.ArptId != null && unmatchedCandidates.Contains(a.ArptId)))
            .ToListAsync(ct);

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
            .AsNoTracking()
            .Where(t => t.StationId != null && resolvedStationIds.Contains(t.StationId))
            .ToListAsync(ct);

        return directMatches.Concat(resolvedMatches).Select(TafMapper.ToDto);
    }
}