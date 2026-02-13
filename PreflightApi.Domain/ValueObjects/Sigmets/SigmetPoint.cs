namespace PreflightApi.Domain.ValueObjects.Sigmets
{
    /// <summary>
    /// A geographic coordinate point forming part of a SIGMET area boundary polygon.
    /// </summary>
    public class SigmetPoint
    {
        /// <summary>Longitude in decimal degrees.</summary>
        public float Longitude { get; set; }
        /// <summary>Latitude in decimal degrees.</summary>
        public float Latitude { get; set; }
    }
}
