using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos.Notam;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Settings;

namespace PreflightApi.Infrastructure.Services.NotamServices;

public class NotamDeltaSyncCronService : INotamDeltaSyncCronService
{
    private readonly INmsApiClient _nmsApiClient;
    private readonly PreflightApiDbContext _dbContext;
    private readonly NmsSettings _settings;
    private readonly ILogger<NotamDeltaSyncCronService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NotamDeltaSyncCronService(
        INmsApiClient nmsApiClient,
        PreflightApiDbContext dbContext,
        IOptions<NmsSettings> settings,
        ILogger<NotamDeltaSyncCronService> logger)
    {
        _nmsApiClient = nmsApiClient;
        _dbContext = dbContext;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SyncDeltaAsync(CancellationToken ct = default)
    {
        try
        {
            // Add 1-minute overlap to avoid gaps
            var lastUpdatedDate = DateTime.UtcNow.AddMinutes(-_settings.DeltaSyncIntervalMinutes - 1);
            _logger.LogInformation("Starting NOTAM delta sync for updates since {LastUpdatedDate}", lastUpdatedDate);

            var notamDtos = await _nmsApiClient.GetNotamsByLastUpdatedDateAsync(lastUpdatedDate, ct);

            if (notamDtos.Count == 0)
            {
                _logger.LogInformation("No NOTAM updates found since {LastUpdatedDate}", lastUpdatedDate);
                return;
            }

            var (newCount, updatedCount, errorCount) = await UpsertNotamsAsync(notamDtos, ct);

            await _dbContext.SaveChangesAsync(ct);

            var purgedCount = await PurgeExpiredAsync(ct);

            _logger.LogInformation(
                "NOTAM delta sync complete: {NewCount} new, {UpdatedCount} updated, {ErrorCount} errors, {PurgedCount} purged, {TotalFetched} fetched",
                newCount, updatedCount, errorCount, purgedCount, notamDtos.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during NOTAM delta sync");
            throw;
        }
    }

    public async Task<(int NewCount, int UpdatedCount, int ErrorCount)> UpsertNotamsAsync(
        List<NotamDto> notamDtos, CancellationToken ct)
    {
        // Extract NMS IDs and load existing records
        var nmsIds = notamDtos
            .Select(ExtractNmsId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        var existingNotams = await _dbContext.Notams
            .Where(n => nmsIds.Contains(n.NmsId))
            .ToDictionaryAsync(n => n.NmsId, ct);

        var newCount = 0;
        var updatedCount = 0;
        var errorCount = 0;

        foreach (var dto in notamDtos)
        {
            try
            {
                var nmsId = ExtractNmsId(dto);
                if (string.IsNullOrEmpty(nmsId))
                {
                    errorCount++;
                    continue;
                }

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
                errorCount++;
                _logger.LogWarning(ex, "Failed to process NOTAM {NotamId}", dto.Id);
            }
        }

        return (newCount, updatedCount, errorCount);
    }

    public static string? ExtractNmsId(NotamDto dto)
    {
        // Try top-level id first, then nested path
        if (!string.IsNullOrEmpty(dto.Id))
            return dto.Id;

        return dto.Properties?.CoreNotamData?.Notam?.Id;
    }

    public Notam MapToEntity(NotamDto dto, string nmsId)
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

    public async Task<int> PurgeExpiredAsync(CancellationToken ct = default)
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
            _logger.LogInformation("Purged {Count} stale NOTAMs (expired or cancelled)", staleNotams.Count);
        }

        return staleNotams.Count;
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
