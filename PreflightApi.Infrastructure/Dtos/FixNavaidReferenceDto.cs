namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// NAVAID reference for a fix, from the FIX_NAV supplementary dataset.
/// Defines the fix position relative to a NAVAID via bearing and distance.
/// </summary>
public record FixNavaidReferenceDto
{
    /// <summary>NAVAID identifier (e.g., DFW, ABQ).</summary>
    public string? NavId { get; init; }

    /// <summary>NAVAID facility type (e.g., VOR, VORTAC, VOR/DME, NDB).</summary>
    public string? NavType { get; init; }

    /// <summary>Bearing from the NAVAID to the fix in degrees magnetic.</summary>
    public string? Bearing { get; init; }

    /// <summary>Distance from the NAVAID to the fix in nautical miles.</summary>
    public string? Distance { get; init; }
}
