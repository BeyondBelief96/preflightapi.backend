namespace PreflightApi.Domain.ValueObjects.GAirmets
{
    /// <summary>
    /// Geographic area affected by a G-AIRMET, defined as a polygon of lat/lon points.
    /// </summary>
    public class GAirmetArea
    {
        /// <summary>Number of points defining the polygon boundary.</summary>
        public int NumPoints { get; set; }
        /// <summary>Ordered list of lat/lon points forming the polygon boundary of the affected area.</summary>
        public List<GAirmetPoint> Points { get; set; } = new();
    }
}
