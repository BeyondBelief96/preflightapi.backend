using System.Globalization;
using NetTopologySuite.Geometries;
using PreflightApi.Domain.Entities;

namespace PreflightApi.Infrastructure.Utilities;

public static class ObstacleLineParser
{
    /// <summary>
    /// Parses a single fixed-width DOF line into an Obstacle entity.
    /// Returns null if the line cannot be parsed (malformed or too short).
    /// </summary>
    public static Obstacle? ParseObstacleLine(string line, GeometryFactory geometryFactory)
    {
        if (string.IsNullOrWhiteSpace(line) || line.Length < 100)
            return null;

        // Fixed-width parsing based on DOF format specification
        var oasCode = SafeSubstring(line, 0, 2).Trim();
        var obstacleNumber = SafeSubstring(line, 3, 6).Trim();
        var oasNumber = $"{oasCode}-{obstacleNumber}";

        var verificationStatus = SafeSubstring(line, 10, 1).Trim();
        var countryId = SafeSubstring(line, 12, 2).Trim();
        var stateId = SafeSubstring(line, 15, 2).Trim();
        var cityName = SafeSubstring(line, 18, 16).Trim();

        // Parse latitude (DMS)
        if (!int.TryParse(SafeSubstring(line, 35, 2).Trim(), out var latDeg))
            return null;
        if (!int.TryParse(SafeSubstring(line, 38, 2).Trim(), out var latMin))
            return null;
        if (!decimal.TryParse(SafeSubstring(line, 41, 5).Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var latSec))
            return null;
        var latHemi = SafeSubstring(line, 46, 1).Trim();

        // Parse longitude (DMS)
        if (!int.TryParse(SafeSubstring(line, 48, 3).Trim(), out var longDeg))
            return null;
        if (!int.TryParse(SafeSubstring(line, 52, 2).Trim(), out var longMin))
            return null;
        if (!decimal.TryParse(SafeSubstring(line, 55, 5).Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var longSec))
            return null;
        var longHemi = SafeSubstring(line, 60, 1).Trim();

        // Convert DMS to decimal
        var latDecimal = ConvertDmsToDecimal(latDeg, latMin, latSec, latHemi);
        var longDecimal = ConvertDmsToDecimal(longDeg, longMin, longSec, longHemi);

        var obstacleType = SafeSubstring(line, 62, 18).Trim();
        int.TryParse(SafeSubstring(line, 81, 1).Trim(), out var quantity);
        int.TryParse(SafeSubstring(line, 83, 5).Trim(), out var heightAgl);
        int.TryParse(SafeSubstring(line, 89, 5).Trim(), out var heightAmsl);

        var lighting = SafeSubstring(line, 95, 1).Trim();
        var horizontalAccuracy = SafeSubstring(line, 97, 1).Trim();
        var verticalAccuracy = SafeSubstring(line, 99, 1).Trim();
        var markIndicator = SafeSubstring(line, 101, 1).Trim();
        var faaStudyNumber = SafeSubstring(line, 103, 14).Trim();
        var action = SafeSubstring(line, 118, 1).Trim();
        var julianDate = line.Length >= 127 ? SafeSubstring(line, 120, 7).Trim() : null;

        return new Obstacle
        {
            OasNumber = oasNumber,
            OasCode = oasCode,
            ObstacleNumber = obstacleNumber,
            VerificationStatus = string.IsNullOrEmpty(verificationStatus) ? null : verificationStatus,
            CountryId = string.IsNullOrEmpty(countryId) ? null : countryId,
            StateId = string.IsNullOrEmpty(stateId) ? null : stateId,
            CityName = string.IsNullOrEmpty(cityName) ? null : cityName,
            LatDegrees = latDeg,
            LatMinutes = latMin,
            LatSeconds = latSec,
            LatHemisphere = latHemi,
            LongDegrees = longDeg,
            LongMinutes = longMin,
            LongSeconds = longSec,
            LongHemisphere = longHemi,
            LatDecimal = latDecimal,
            LongDecimal = longDecimal,
            ObstacleType = string.IsNullOrEmpty(obstacleType) ? null : obstacleType,
            Quantity = quantity > 0 ? quantity : null,
            HeightAgl = heightAgl > 0 ? heightAgl : null,
            HeightAmsl = heightAmsl > 0 ? heightAmsl : null,
            Lighting = string.IsNullOrEmpty(lighting) ? null : lighting,
            HorizontalAccuracy = string.IsNullOrEmpty(horizontalAccuracy) ? null : horizontalAccuracy,
            VerticalAccuracy = string.IsNullOrEmpty(verticalAccuracy) ? null : verticalAccuracy,
            MarkIndicator = string.IsNullOrEmpty(markIndicator) ? null : markIndicator,
            FaaStudyNumber = string.IsNullOrEmpty(faaStudyNumber) ? null : faaStudyNumber,
            Action = string.IsNullOrEmpty(action) ? null : action,
            JulianDate = string.IsNullOrEmpty(julianDate) ? null : julianDate,
            Location = geometryFactory.CreatePoint(new Coordinate((double)longDecimal, (double)latDecimal))
        };
    }

    public static string SafeSubstring(string str, int startIndex, int length)
    {
        if (startIndex >= str.Length)
            return string.Empty;

        var actualLength = Math.Min(length, str.Length - startIndex);
        return str.Substring(startIndex, actualLength);
    }

    public static decimal ConvertDmsToDecimal(int degrees, int minutes, decimal seconds, string hemisphere)
    {
        var result = degrees + (minutes / 60.0m) + (seconds / 3600.0m);
        return (hemisphere == "S" || hemisphere == "W") ? -result : result;
    }
}
