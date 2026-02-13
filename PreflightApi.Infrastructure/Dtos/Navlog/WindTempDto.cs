namespace PreflightApi.Infrastructure.Dtos.Navlog;

/// <summary>
/// Wind direction, speed, and temperature at a specific altitude level.
/// </summary>
public record WindTempDto
{
    /// <summary>Wind direction in degrees true (null if calm or light/variable).</summary>
    public int? Direction { get; init; }
    /// <summary>Wind speed in knots.</summary>
    public int Speed { get; init; }
    /// <summary>Temperature in degrees Celsius (not available at all altitudes).</summary>
    public float? Temperature { get; init; }
}
