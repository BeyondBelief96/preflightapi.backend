namespace PreflightApi.Infrastructure.Dtos.Performance;

/// <summary>
/// Request DTO for cloud base estimation.
/// </summary>
public record CloudBaseRequestDto
{
    /// <summary>
    /// Surface temperature in degrees Celsius
    /// </summary>
    public double TemperatureCelsius { get; init; }

    /// <summary>
    /// Dewpoint temperature in degrees Celsius (must be ≤ temperature)
    /// </summary>
    public double DewpointCelsius { get; init; }
}
