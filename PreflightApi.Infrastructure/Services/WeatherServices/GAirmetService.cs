using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Mappers;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Utilities;

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

    public async Task<PaginatedResponse<GAirmetDto>> GetAllGAirmets(string? cursor, int limit, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Retrieving G-AIRMETs, cursor: {Cursor}, limit: {Limit}", cursor, limit);

            var query = _context.GAirmets.AsNoTracking();
            return await query.ToPaginatedAsync(g => g.Id, GAirmetMapper.ToDto, cursor, limit, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all G-AIRMETs");
            throw;
        }
    }

    public async Task<PaginatedResponse<GAirmetDto>> GetGAirmetsByProduct(GAirmetProduct product, string? cursor, int limit, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Retrieving G-AIRMETs for product {Product}, cursor: {Cursor}, limit: {Limit}",
                product, cursor, limit);

            var productString = product.ToString().ToUpperInvariant();
            var query = _context.GAirmets.AsNoTracking()
                .Where(g => g.Product == productString);

            return await query.ToPaginatedAsync(g => g.Id, GAirmetMapper.ToDto, cursor, limit, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving G-AIRMETs for product {Product}", product);
            throw;
        }
    }

    public async Task<PaginatedResponse<GAirmetDto>> GetGAirmetsByHazardType(GAirmetHazardType hazardType, string? cursor, int limit, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Retrieving G-AIRMETs for hazard type {HazardType}, cursor: {Cursor}, limit: {Limit}",
                hazardType, cursor, limit);

            var hazardTypeStrings = GetHazardTypeStrings(hazardType);
            var query = _context.GAirmets.AsNoTracking()
                .Where(g => g.HazardType != null && hazardTypeStrings.Contains(g.HazardType));

            return await query.ToPaginatedAsync(g => g.Id, GAirmetMapper.ToDto, cursor, limit, ct);
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
