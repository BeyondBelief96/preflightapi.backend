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
        var taf = await _dbContext.Tafs.FirstOrDefaultAsync(t => t.StationId == icaoCodeOrIdent.ToUpper());

        if (taf == null)
        {
            var airport = await _dbContext.Airports
                .FirstOrDefaultAsync(a => a.ArptId == icaoCodeOrIdent.ToUpper() ||
                                          a.IcaoId == icaoCodeOrIdent.ToUpper());

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
}