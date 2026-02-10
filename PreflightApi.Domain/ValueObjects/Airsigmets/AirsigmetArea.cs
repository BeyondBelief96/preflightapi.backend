namespace PreflightApi.Domain.ValueObjects.Airsigmets
{
    /// <summary>
    /// Geographic area affected by an AIRMET or SIGMET, defined as a polygon of lat/lon points.
    /// </summary>
    public class AirsigmetArea
    {
        /// <summary>Number of points defining the polygon boundary.</summary>
        public int NumPoints { get; set; }
        /// <summary>Ordered list of lat/lon points forming the polygon boundary of the affected area.</summary>
        public List<AirsigmetPoint> Points { get; set; } = new();
    }
}
