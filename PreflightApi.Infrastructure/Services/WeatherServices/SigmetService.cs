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

    public async Task<PaginatedResponse<SigmetDto>> GetAllSigmets(string? cursor, int limit, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Retrieving SIGMETs, cursor: {Cursor}, limit: {Limit}", cursor, limit);

            var query = _context.Sigmets.AsNoTracking();
            return await query.ToPaginatedAsync(s => s.Id, SigmetMapper.ToDto, cursor, limit, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all SIGMETs");
            throw;
        }
    }

    public async Task<PaginatedResponse<SigmetDto>> GetSigmetsByHazardType(SigmetHazardType hazardType, string? cursor, int limit, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Retrieving SIGMETs for hazard type {HazardType}, cursor: {Cursor}, limit: {Limit}",
                hazardType, cursor, limit);

            var hazardTypeString = ConvertHazardTypeToString(hazardType);
            // Hazard is stored as a JSON column via value converter, so property access
            // can't be translated to SQL. Load all and filter in memory (small dataset).
            var allSigmets = await _context.Sigmets.AsNoTracking()
                .OrderBy(s => s.Id)
                .ToListAsync(ct);

            var filtered = allSigmets
                .Where(a => a.Hazard != null && a.Hazard.Type == hazardTypeString)
                .ToList();

            var decodedCursor = CursorHelper.DecodeInt(cursor);
            if (decodedCursor.HasValue)
                filtered = filtered.Where(s => s.Id > decodedCursor.Value).ToList();

            var hasMore = filtered.Count > limit;
            var page = filtered.Take(limit).ToList();
            var nextCursor = hasMore && page.Count > 0
                ? CursorHelper.Encode(page[^1].Id)
                : null;

            return new PaginatedResponse<SigmetDto>
            {
                Data = page.Select(SigmetMapper.ToDto),
                Pagination = new PaginationMetadata
                {
                    NextCursor = nextCursor,
                    HasMore = hasMore,
                    Limit = limit
                }
            };
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
