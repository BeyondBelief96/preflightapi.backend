namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// GeoJSON geometry object with a type and coordinate array.
/// </summary>
public record GeoJsonGeometry
{
    /// <summary>Geometry type (e.g., Polygon, MultiPolygon).</summary>
    public string Type { get; init; } = string.Empty;
    /// <summary>Coordinate array defining the geometry boundary.</summary>
    public double[][][] Coordinates { get; init; } = [];
}
