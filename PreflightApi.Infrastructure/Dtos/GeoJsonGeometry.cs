namespace PreflightApi.Infrastructure.Dtos
{
    /// <summary>
    /// GeoJSON geometry object with a type and coordinate array.
    /// </summary>
    public class GeoJsonGeometry
    {
        /// <summary>Geometry type (e.g., Polygon, MultiPolygon).</summary>
        public string Type { get; set; } = string.Empty;
        /// <summary>Coordinate array defining the geometry boundary.</summary>
        public double[][][] Coordinates { get; set; } = [];
    }
}
