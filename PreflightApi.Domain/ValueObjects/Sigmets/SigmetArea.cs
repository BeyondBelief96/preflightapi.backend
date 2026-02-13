namespace PreflightApi.Domain.ValueObjects.Sigmets
{
    /// <summary>
    /// Geographic area affected by a SIGMET, defined as a polygon of lat/lon points.
    /// </summary>
    public class SigmetArea
    {
        /// <summary>Number of points defining the polygon boundary.</summary>
        public int NumPoints { get; set; }
        /// <summary>Ordered list of lat/lon points forming the polygon boundary of the affected area.</summary>
        public List<SigmetPoint> Points { get; set; } = new();
    }
}
