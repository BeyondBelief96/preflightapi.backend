using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Mappers;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Infrastructure.Services.WeatherServices;

public class SigmetService : ISigmetService
{
    private readonly PreflightApiDbContext _context;
    private readonly ILogger<SigmetService> _logger;

    public SigmetService(
        PreflightApiDbContext context,
        ILogger<SigmetService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SigmetDto>> GetAllSigmets()
    {
        try
        {
            _logger.LogInformation("Retrieving all SIGMETs");

            var sigmets = await _context.Sigmets.ToListAsync();
            return sigmets.Select(SigmetMapper.ToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all SIGMETs");
            throw;
        }
    }

    public async Task<List<SigmetDto>> GetSigmetsByHazardType(SigmetHazardType hazardType)
    {
        try
        {
            _logger.LogInformation("Retrieving SIGMETs for hazard type {HazardType}", hazardType);

            var hazardTypeString = ConvertHazardTypeToString(hazardType);
            // Hazard is stored as a JSON column via value converter, so property access
            // can't be translated to SQL. Load all and filter in memory (small dataset).
            var allSigmets = await _context.Sigmets.ToListAsync();
            var sigmets = allSigmets
                .Where(a => a.Hazard != null && a.Hazard.Type == hazardTypeString)
                .ToList();

            return sigmets.Select(SigmetMapper.ToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SIGMETs for hazard type {HazardType}", hazardType);
            throw;
        }
    }

    private static string ConvertHazardTypeToString(SigmetHazardType hazardType)
    {
        return hazardType switch
        {
            SigmetHazardType.CONVECTIVE => "CONVECTIVE",
            SigmetHazardType.ICE => "ICE",
            SigmetHazardType.TURB => "TURB",
            SigmetHazardType.IFR => "IFR",
            SigmetHazardType.MTN_OBSCN => "MTN OBSCN",
            _ => throw new ArgumentOutOfRangeException(nameof(hazardType))
        };
    }
}
