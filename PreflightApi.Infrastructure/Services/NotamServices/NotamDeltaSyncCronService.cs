using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos.Notam;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Settings;

namespace PreflightApi.Infrastructure.Services.NotamServices;

public partial class NotamDeltaSyncCronService : INotamDeltaSyncCronService
{
    private readonly INmsApiClient _nmsApiClient;
    private readonly PreflightApiDbContext _dbContext;
    private readonly NmsSettings _settings;
    private readonly ILogger<NotamDeltaSyncCronService> _logger;
    private readonly ISyncTelemetryService _telemetry;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NotamDeltaSyncCronService(
        INmsApiClient nmsApiClient,
        PreflightApiDbContext dbContext,
        IOptions<NmsSettings> settings,
        ILogger<NotamDeltaSyncCronService> logger,
        ISyncTelemetryService telemetry)
    {
        _nmsApiClient = nmsApiClient;
        _dbContext = dbContext;
        _settings = settings.Value;
        _logger = logger;
        _telemetry = telemetry;
    }

    public async Task SyncDeltaAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            // Add 1-minute overlap to avoid gaps
            var lastUpdatedDate = DateTime.UtcNow.AddMinutes(-_settings.DeltaSyncIntervalMinutes - 1);
            _logger.LogInformation("Starting NOTAM delta sync for updates since {LastUpdatedDate}", lastUpdatedDate);

            var notamDtos = await _nmsApiClient.GetNotamsByLastUpdatedDateAsync(lastUpdatedDate, ct);

            if (notamDtos.Count == 0)
            {
                _logger.LogInformation("No NOTAM updates found since {LastUpdatedDate}", lastUpdatedDate);
                _telemetry.TrackSyncCompleted("NotamDelta", 0, 0, sw.ElapsedMilliseconds);
                return;
            }

            var (newCount, updatedCount, errorCount) = await UpsertNotamsAsync(notamDtos, ct);

            await _dbContext.SaveChangesAsync(ct);

            var purgedCount = await PurgeExpiredAsync(ct);

            _logger.LogInformation(
                "NOTAM delta sync complete: {NewCount} new, {UpdatedCount} updated, {ErrorCount} errors, {PurgedCount} purged, {TotalFetched} fetched",
                newCount, updatedCount, errorCount, purgedCount, notamDtos.Count);
            _telemetry.TrackSyncCompleted("NotamDelta", notamDtos.Count, errorCount, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during NOTAM delta sync");
            _telemetry.TrackSyncFailed("NotamDelta", ex, sw.ElapsedMilliseconds);
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
            Classification = detail?.Classification?.ToString(),
            NotamType = detail?.Type?.ToString(),
            NotamNumber = NormalizeNotamNumber(detail?.Number),
            NotamYear = detail?.Year,
            Series = detail?.Series,
            AccountId = detail?.AccountId,
            AirportName = detail?.AirportName,
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
        existing.NotamNumber = updated.NotamNumber;
        existing.NotamYear = updated.NotamYear;
        existing.Series = updated.Series;
        existing.AccountId = updated.AccountId;
        existing.AirportName = updated.AirportName;
        existing.EffectiveStart = updated.EffectiveStart;
        existing.EffectiveEnd = updated.EffectiveEnd;
        existing.CancelationDate = updated.CancelationDate;
        existing.Text = updated.Text;
        existing.LastUpdated = updated.LastUpdated;
        existing.SyncedAt = updated.SyncedAt;
        existing.FeatureJson = updated.FeatureJson;
        existing.Geometry = updated.Geometry;
    }

    /// <summary>
    /// Strips any "mm/" month prefix from the NOTAM number, returning just the bare sequence number.
    /// e.g., "01/123" → "123", "420" → "420"
    /// </summary>
    public static string? NormalizeNotamNumber(string? number)
    {
        if (string.IsNullOrWhiteSpace(number))
            return null;

        var trimmed = number.Trim();
        var match = MonthPrefixRegex().Match(trimmed);
        return match.Success ? match.Groups[1].Value : trimmed;
    }

    [GeneratedRegex(@"^\d{1,2}/(\d+)$")]
    private static partial Regex MonthPrefixRegex();

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
