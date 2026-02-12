using System.Text.Json;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using PreflightApi.Infrastructure.Dtos.Notam;

namespace PreflightApi.Infrastructure.Services.NotamServices;

/// <summary>
/// Converts NotamGeometryDto (GeoJSON geometry) to NTS Geometry objects for PostGIS storage.
/// </summary>
public static class NotamGeometryParser
{
    private static readonly GeometryFactory GeometryFactory =
        new(new PrecisionModel(), 4326);

    /// <summary>
    /// Parses a NotamGeometryDto into an NTS Geometry.
    /// Returns null if geometry is missing, null, or unparseable.
    /// </summary>
    public static Geometry? Parse(NotamGeometryDto? geometry, ILogger? logger = null)
    {
        if (geometry == null)
            return null;

        try
        {
            return geometry.Type?.ToUpperInvariant() switch
            {
                "POINT" => ParsePoint(geometry),
                "POLYGON" => ParsePolygon(geometry),
                "GEOMETRYCOLLECTION" => ParseGeometryCollection(geometry, logger),
                _ => null
            };
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to parse NOTAM geometry of type {GeometryType}", geometry.Type);
            return null;
        }
    }

    private static Point? ParsePoint(NotamGeometryDto geometry)
    {
        if (geometry.Coordinates == null)
            return null;

        var coords = ParseCoordinateArray(geometry.Coordinates);
        if (coords == null || coords.Length < 2)
            return null;

        return GeometryFactory.CreatePoint(new Coordinate(coords[0], coords[1]));
    }

    private static Polygon? ParsePolygon(NotamGeometryDto geometry)
    {
        if (geometry.Coordinates == null)
            return null;

        var rings = ParsePolygonRings(geometry.Coordinates);
        if (rings == null || rings.Length == 0)
            return null;

        var shell = GeometryFactory.CreateLinearRing(rings[0]);
        var holes = rings.Length > 1
            ? rings.Skip(1).Select(r => GeometryFactory.CreateLinearRing(r)).ToArray()
            : null;

        return GeometryFactory.CreatePolygon(shell, holes);
    }

    private static GeometryCollection? ParseGeometryCollection(NotamGeometryDto geometry, ILogger? logger)
    {
        if (geometry.Geometries == null || geometry.Geometries.Count == 0)
            return null;

        var geometries = new List<Geometry>();
        foreach (var child in geometry.Geometries)
        {
            var parsed = Parse(child, logger);
            if (parsed != null)
            {
                geometries.Add(parsed);
            }
        }

        return geometries.Count > 0
            ? GeometryFactory.CreateGeometryCollection(geometries.ToArray())
            : null;
    }

    private static double[]? ParseCoordinateArray(object coordinates)
    {
        if (coordinates is JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Array)
                return null;

            var result = new double[element.GetArrayLength()];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = element[i].GetDouble();
            }
            return result;
        }

        return null;
    }

    private static Coordinate[][]? ParsePolygonRings(object coordinates)
    {
        if (coordinates is not JsonElement element || element.ValueKind != JsonValueKind.Array)
            return null;

        var rings = new List<Coordinate[]>();
        foreach (var ring in element.EnumerateArray())
        {
            if (ring.ValueKind != JsonValueKind.Array)
                continue;

            var coords = new List<Coordinate>();
            foreach (var point in ring.EnumerateArray())
            {
                if (point.ValueKind != JsonValueKind.Array || point.GetArrayLength() < 2)
                    continue;

                coords.Add(new Coordinate(point[0].GetDouble(), point[1].GetDouble()));
            }

            if (coords.Count >= 4) // Minimum for a valid ring
            {
                rings.Add(coords.ToArray());
            }
        }

        return rings.Count > 0 ? rings.ToArray() : null;
    }
}
