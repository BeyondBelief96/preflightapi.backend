namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// VOR receiver checkpoint for a navigational aid, from the NAV_CKPT supplementary dataset.
/// </summary>
public record NavaidCheckpointDto
{
    /// <summary>Altitude in feet MSL at which the checkpoint is valid.</summary>
    public int? Altitude { get; init; }

    /// <summary>Bearing from the NAVAID to the checkpoint in degrees magnetic.</summary>
    public string? Bearing { get; init; }

    /// <summary>Air/ground code: A = airborne, G = ground, B = both.</summary>
    public string? AirGroundCode { get; init; }

    /// <summary>Description of the checkpoint location.</summary>
    public string? Description { get; init; }

    /// <summary>Airport identifier where the checkpoint is located.</summary>
    public string? AirportId { get; init; }

    /// <summary>State code where the checkpoint is located.</summary>
    public string? StateCheckCode { get; init; }
}
