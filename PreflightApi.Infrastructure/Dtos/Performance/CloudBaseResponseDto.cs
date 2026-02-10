namespace PreflightApi.Infrastructure.Dtos.Performance;

/// <summary>
/// Response DTO for cloud base estimation.
/// </summary>
public record CloudBaseResponseDto
{
    /// <summary>
    /// Estimated cloud base in feet AGL
    /// </summary>
    public double EstimatedCloudBaseFtAgl { get; init; }

    /// <summary>
    /// Temperature/dewpoint spread in degrees Celsius
    /// </summary>
    public double TemperatureDewpointSpreadCelsius { get; init; }

    /// <summary>
    /// Surface temperature used in calculation (°C)
    /// </summary>
    public double TemperatureCelsius { get; init; }

    /// <summary>
    /// Dewpoint used in calculation (°C)
    /// </summary>
    public double DewpointCelsius { get; init; }
}
