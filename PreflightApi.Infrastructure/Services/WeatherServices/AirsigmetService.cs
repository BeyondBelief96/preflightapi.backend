using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Mappers;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Infrastructure.Services.WeatherServices;

public class AirsigmetService : IAirsigmetService
{
    private readonly PreflightApiDbContext _context;
    private readonly ILogger<AirsigmetService> _logger;

    public AirsigmetService(
        PreflightApiDbContext context,
        ILogger<AirsigmetService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<AirsigmetDto>> GetAllAirsigmets()
    {
        try
        {
            _logger.LogInformation("Retrieving all AIRSIGMETs");

            var airsigmets = await _context.Airsigmets.ToListAsync();
            return airsigmets.Select(AirsigmetMapper.ToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all AIRSIGMETs");
            throw;
        }
    }

    public async Task<List<AirsigmetDto>> GetAirsigmetsByHazardType(AirsigmetHazardType hazardType)
    {
        try
        {
            _logger.LogInformation("Retrieving AIRSIGMETs for hazard type {HazardType}", hazardType);

            var hazardTypeString = ConvertHazardTypeToString(hazardType);
            var airsigmets = await _context.Airsigmets
                .Where(a => a.Hazard != null && a.Hazard.Type == hazardTypeString)
                .ToListAsync();

            return airsigmets.Select(AirsigmetMapper.ToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving AIRSIGMETs for hazard type {HazardType}", hazardType);
            throw;
        }
    }

    private static string ConvertHazardTypeToString(AirsigmetHazardType hazardType)
    {
        return hazardType switch
        {
            AirsigmetHazardType.CONVECTIVE => "CONVECTIVE",
            AirsigmetHazardType.ICE => "ICE",
            AirsigmetHazardType.TURB => "TURB",
            AirsigmetHazardType.IFR => "IFR",
            AirsigmetHazardType.MTN_OBSCN => "MTN OBSCN",
            _ => throw new ArgumentOutOfRangeException(nameof(hazardType))
        };
    }
}
