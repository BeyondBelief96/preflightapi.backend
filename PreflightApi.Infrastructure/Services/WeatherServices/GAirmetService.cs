using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Mappers;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Infrastructure.Services.WeatherServices;

public class GAirmetService : IGAirmetService
{
    private readonly PreflightApiDbContext _context;
    private readonly ILogger<GAirmetService> _logger;

    public GAirmetService(
        PreflightApiDbContext context,
        ILogger<GAirmetService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<GAirmetDto>> GetAllGAirmets()
    {
        try
        {
            _logger.LogInformation("Retrieving all G-AIRMETs");

            var gairmets = await _context.GAirmets.ToListAsync();
            return gairmets.Select(GAirmetMapper.ToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all G-AIRMETs");
            throw;
        }
    }

    public async Task<List<GAirmetDto>> GetGAirmetsByProduct(GAirmetProduct product)
    {
        try
        {
            _logger.LogInformation("Retrieving G-AIRMETs for product {Product}", product);

            var productString = product.ToString().ToUpperInvariant();
            var gairmets = await _context.GAirmets
                .Where(g => g.Product == productString)
                .ToListAsync();

            return gairmets.Select(GAirmetMapper.ToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving G-AIRMETs for product {Product}", product);
            throw;
        }
    }

    public async Task<List<GAirmetDto>> GetGAirmetsByHazardType(GAirmetHazardType hazardType)
    {
        try
        {
            _logger.LogInformation("Retrieving G-AIRMETs for hazard type {HazardType}", hazardType);

            var hazardTypeStrings = GetHazardTypeStrings(hazardType);
            var gairmets = await _context.GAirmets
                .Where(g => g.HazardType != null && hazardTypeStrings.Contains(g.HazardType))
                .ToListAsync();

            return gairmets.Select(GAirmetMapper.ToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving G-AIRMETs for hazard type {HazardType}", hazardType);
            throw;
        }
    }

    private static List<string> GetHazardTypeStrings(GAirmetHazardType hazardType)
    {
        // Return list to handle cases where multiple string formats map to same enum
        return hazardType switch
        {
            GAirmetHazardType.MT_OBSC => ["MT_OBSC"],
            GAirmetHazardType.IFR => ["IFR"],
            GAirmetHazardType.TURB_LO => ["TURB-LO"],
            GAirmetHazardType.TURB_HI => ["TURB-HI"],
            GAirmetHazardType.LLWS => ["LLWS"],
            GAirmetHazardType.SFC_WIND => ["SFC_WIND", "SFC-WIND"], // Handle both formats
            GAirmetHazardType.ICE => ["ICE"],
            GAirmetHazardType.FZLVL => ["FZLVL"],
            GAirmetHazardType.M_FZLVL => ["M_FZLVL"],
            _ => []
        };
    }
}
