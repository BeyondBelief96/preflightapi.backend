namespace PreflightApi.Domain.ValueObjects.GAirmets;

/// <summary>
/// Altitude information for a G-AIRMET advisory. Bottom/top levels valid in feet MSL; 0 indicates surface, -1 indicates freezing level.
/// </summary>
public class GAirmetAltitude
{
    /// <summary>
    /// Minimum altitude in feet MSL. 0 indicates surface, -1 indicates freezing level.
    /// </summary>
    public string? MinFtMsl { get; set; }

    /// <summary>
    /// Maximum altitude in feet MSL.
    /// </summary>
    public string? MaxFtMsl { get; set; }

    /// <summary>
    /// Single level altitude in feet MSL (used for freezing level lines).
    /// </summary>
    public string? LevelFtMsl { get; set; }

    /// <summary>
    /// Range of altitudes for freezing level within an icing AIRMET.
    /// </summary>
    public GAirmetFzlAltitude? FzlAltitude { get; set; }
}

/// <summary>
/// Range of altitudes for the freezing level within an icing G-AIRMET.
/// </summary>
public class GAirmetFzlAltitude
{
    /// <summary>
    /// Minimum freezing level altitude in feet MSL.
    /// </summary>
    public string? MinFtMsl { get; set; }

    /// <summary>
    /// Maximum freezing level altitude in feet MSL.
    /// </summary>
    public string? MaxFtMsl { get; set; }
}
