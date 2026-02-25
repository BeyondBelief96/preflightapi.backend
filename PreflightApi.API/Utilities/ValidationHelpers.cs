using PreflightApi.Domain.Exceptions;

namespace PreflightApi.API.Utilities;

/// <summary>
/// Shared validation helpers for controller input validation.
/// All methods throw <see cref="ValidationException"/> on failure.
/// </summary>
public static class ValidationHelpers
{
    public static void ValidateCoordinates(decimal lat, decimal lon)
    {
        if (lat < -90 || lat > 90)
            throw new ValidationException("lat", "Latitude must be between -90 and 90 degrees");
        if (lon < -180 || lon > 180)
            throw new ValidationException("lon", "Longitude must be between -180 and 180 degrees");
    }

    public static void ValidateCoordinates(double lat, double lon)
    {
        if (lat < -90 || lat > 90)
            throw new ValidationException("lat", "Latitude must be between -90 and 90 degrees");
        if (lon < -180 || lon > 180)
            throw new ValidationException("lon", "Longitude must be between -180 and 180 degrees");
    }

    public static void ValidateRadius(double radiusNm, double maxRadiusNm)
    {
        if (radiusNm <= 0)
            throw new ValidationException("radiusNm", "Radius must be greater than 0");
        if (radiusNm > maxRadiusNm)
            throw new ValidationException("radiusNm", $"Radius cannot exceed {maxRadiusNm} nautical miles");
    }

    public static void ValidateRequiredString(string? value, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException(paramName, message);
    }

    public static void ValidateBatchSize(int count, int max, string paramName)
    {
        if (count > max)
            throw new ValidationException(paramName, $"Maximum of {max} identifiers allowed per request");
    }
}
