namespace PreflightApi.Domain.ValueObjects.GAirmets
{
    /// <summary>
    /// A geographic coordinate point forming part of a G-AIRMET area boundary polygon.
    /// </summary>
    public class GAirmetPoint
    {
        /// <summary>Longitude in decimal degrees.</summary>
        public double Longitude { get; set; }
        /// <summary>Latitude in decimal degrees.</summary>
        public double Latitude { get; set; }
    }
}
