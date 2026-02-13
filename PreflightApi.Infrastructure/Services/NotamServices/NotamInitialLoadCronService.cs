using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos.Notam;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Infrastructure.Services.NotamServices;

public class NotamInitialLoadCronService : INotamInitialLoadCronService
{
    private readonly INmsApiClient _nmsApiClient;
    private readonly PreflightApiDbContext _dbContext;
    private readonly ILogger<NotamInitialLoadCronService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NotamInitialLoadCronService(
        INmsApiClient nmsApiClient,
        PreflightApiDbContext dbContext,
        ILogger<NotamInitialLoadCronService> logger)
    {
        _nmsApiClient = nmsApiClient;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task LoadAllClassificationsAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting NOTAM initial load via bulk /v1/notams/il endpoint");

            var notamDtos = await _nmsApiClient.GetAllNotamsInitialLoadAsync(ct);

            _logger.LogInformation("Fetched {Count} NOTAMs from initial load", notamDtos.Count);

            var (newCount, updatedCount) = await UpsertBatchAsync(notamDtos, ct);
            await _dbContext.SaveChangesAsync(ct);

            // Purge NOTAMs whose effective end has passed
            await CleanupExpiredAsync(ct);

            _logger.LogInformation(
                "NOTAM initial load complete: {NewCount} new, {UpdatedCount} updated out of {TotalFetched} fetched",
                newCount, updatedCount, notamDtos.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error during NOTAM initial load");
            throw;
        }
    }

    private async Task<(int NewCount, int UpdatedCount)> UpsertBatchAsync(
        List<NotamDto> notamDtos, CancellationToken ct)
    {
        var nmsIds = notamDtos
            .Select(NotamDeltaSyncCronService.ExtractNmsId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        var existingNotams = await _dbContext.Notams
            .Where(n => nmsIds.Contains(n.NmsId))
            .ToDictionaryAsync(n => n.NmsId, ct);

        var newCount = 0;
        var updatedCount = 0;

        foreach (var dto in notamDtos)
        {
            try
            {
                var nmsId = NotamDeltaSyncCronService.ExtractNmsId(dto);
                if (string.IsNullOrEmpty(nmsId))
                    continue;

                var entity = MapToEntity(dto, nmsId);

                if (existingNotams.TryGetValue(nmsId, out var existing))
                {
                    UpdateEntity(existing, entity);
                    updatedCount++;
                }
                else
                {
                    await _dbContext.Notams.AddAsync(entity, ct);
                    existingNotams[nmsId] = entity;
                    newCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process NOTAM {NotamId}", dto.Id);
            }
        }

        return (newCount, updatedCount);
    }

    private async Task CleanupExpiredAsync(CancellationToken ct)
    {
        try
        {
            var now = DateTime.UtcNow;

            var staleNotams = await _dbContext.Notams
                .Where(n =>
                    (n.CancelationDate != null && n.CancelationDate <= now) || // Manually cancelled
                    (n.EffectiveEnd != null && n.EffectiveEnd < now))          // Naturally expired
                .ToListAsync(ct);

            if (staleNotams.Count > 0)
            {
                _dbContext.Notams.RemoveRange(staleNotams);
                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Purged {Count} stale NOTAMs during initial load cleanup (expired or cancelled)", staleNotams.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cleanup of stale NOTAMs failed (non-fatal)");
        }
    }

    private Notam MapToEntity(NotamDto dto, string nmsId)
    {
        var detail = dto.Properties?.CoreNotamData?.Notam;

        return new Notam
        {
            NmsId = nmsId,
            Location = detail?.Location,
            IcaoLocation = detail?.IcaoLocation,
            Classification = detail?.Classification,
            NotamType = detail?.Type,
            EffectiveStart = ParseDateTime(detail?.EffectiveStart),
            EffectiveEnd = ParseDateTime(detail?.EffectiveEnd),
            CancelationDate = ParseDateTime(detail?.CancelationDate),
            Text = detail?.Text,
            LastUpdated = ParseDateTime(detail?.LastUpdated),
            SyncedAt = DateTime.UtcNow,
            FeatureJson = JsonSerializer.Serialize(dto, JsonOptions),
            Geometry = NotamGeometryParser.Parse(dto.Geometry, _logger)
        };
    }

    private static void UpdateEntity(Notam existing, Notam updated)
    {
        existing.Location = updated.Location;
        existing.IcaoLocation = updated.IcaoLocation;
        existing.Classification = updated.Classification;
        existing.NotamType = updated.NotamType;
        existing.EffectiveStart = updated.EffectiveStart;
        existing.EffectiveEnd = updated.EffectiveEnd;
        existing.CancelationDate = updated.CancelationDate;
        existing.Text = updated.Text;
        existing.LastUpdated = updated.LastUpdated;
        existing.SyncedAt = updated.SyncedAt;
        existing.FeatureJson = updated.FeatureJson;
        existing.Geometry = updated.Geometry;
    }

    private static DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var result)
            ? result
            : null;
    }
}
