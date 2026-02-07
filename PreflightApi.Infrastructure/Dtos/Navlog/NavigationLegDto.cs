namespace PreflightApi.Infrastructure.Dtos.Navlog;

/// <summary>
/// A single navigation leg between two waypoints with computed flight data.
/// </summary>
public record NavigationLegDto
{
    /// <summary>Starting waypoint for this leg.</summary>
    public WaypointDto LegStartPoint { get; set; } = new();
    /// <summary>Ending waypoint for this leg.</summary>
    public WaypointDto LegEndPoint { get; set; } = new();
    /// <summary>True course in degrees.</summary>
    public double TrueCourse { get; set; }
    /// <summary>Magnetic heading in degrees (corrected for wind and variation).</summary>
    public double MagneticHeading { get; set; }
    /// <summary>Magnetic course in degrees (corrected for variation only).</summary>
    public double MagneticCourse { get; set; }
    /// <summary>Ground speed in knots.</summary>
    public double GroundSpeed { get; set; }
    /// <summary>Leg distance in nautical miles.</summary>
    public double LegDistance { get; set; }
    /// <summary>Remaining distance to destination in nautical miles.</summary>
    public double DistanceRemaining { get; set; }
    /// <summary>Estimated time at the start of this leg (UTC).</summary>
    public DateTime StartLegTime { get; set; }
    /// <summary>Estimated time at the end of this leg (UTC).</summary>
    public DateTime EndLegTime { get; set; }
    /// <summary>Fuel burned during this leg in gallons.</summary>
    public double LegFuelBurnGals { get; set; }
    /// <summary>Remaining fuel at the end of this leg in gallons.</summary>
    public double RemainingFuelGals { get; set; }
    /// <summary>Forecast wind direction in degrees true.</summary>
    public int WindDir { get; set; }
    /// <summary>Forecast wind speed in knots.</summary>
    public int WindSpeed { get; set; }
    /// <summary>Headwind component in knots (negative = tailwind).</summary>
    public double HeadwindComponent { get; set; }
    /// <summary>Forecast temperature in degrees Celsius.</summary>
    public float TempC { get; set; }
}
