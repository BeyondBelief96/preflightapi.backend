using PreflightApi.Domain.Constants;

namespace PreflightApi.API;

public static class DataRouteMapping
{
    public static readonly Dictionary<string, string[]> RouteToSyncTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["metars"] = [SyncTypes.Metar],
        ["tafs"] = [SyncTypes.Taf],
        ["pireps"] = [SyncTypes.Pirep],
        ["sigmets"] = [SyncTypes.Sigmet],
        ["g-airmets"] = [SyncTypes.GAirmet],
        ["notams"] = [SyncTypes.NotamDelta],
        ["airports"] = [SyncTypes.Airport],
        ["communication-frequencies"] = [SyncTypes.Frequency],
        ["airspaces"] = [SyncTypes.Airspace, SyncTypes.SpecialUseAirspace],
        ["obstacles"] = [SyncTypes.Obstacle],
        ["chart-supplements"] = [SyncTypes.ChartSupplement],
        ["terminal-procedures"] = [SyncTypes.TerminalProcedure],
    };

    public static string? ExtractDataRouteSegment(string? path)
    {
        if (path == null || !path.StartsWith("/api/v", StringComparison.OrdinalIgnoreCase))
            return null;

        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        // parts[0] = "api", parts[1] = "v1" (or similar), parts[2] = route segment
        return parts.Length >= 3 ? parts[2] : null;
    }
}
