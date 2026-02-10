namespace PreflightApi.Domain.ValueObjects.Airsigmets
{
    /// <summary>
    /// A geographic coordinate point forming part of an AIRMET/SIGMET area boundary polygon.
    /// </summary>
    public class AirsigmetPoint
    {
        /// <summary>Longitude in decimal degrees.</summary>
        public float Longitude { get; set; }
        /// <summary>Latitude in decimal degrees.</summary>
        public float Latitude { get; set; }
    }
}
