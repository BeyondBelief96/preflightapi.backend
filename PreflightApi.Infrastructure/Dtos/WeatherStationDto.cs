using PreflightApi.Domain.Constants;

namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// ASOS/AWOS weather station data from the FAA NASR database (AWOS dataset).
/// Includes automated weather observation systems at airports and other landing facilities.
/// </summary>
public record WeatherStationDto()
{
    /// <summary>Internal unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Weather station identifier (typically matches the associated airport identifier).</summary>
    public string AsosAwosId { get; init; } = string.Empty;

    /// <summary>
    /// Weather system sensor type.
    /// </summary>
    /// <remarks>See <see cref="WeatherStationValues.SensorType"/> for known values (e.g., ASOS, AWOS-1, AWOS-2, AWOS-3, AWOS-3P, AWOS-3PT, AWOS-3T, AWOS-4, AWOS-A, AWOS-AV).</remarks>
    public string AsosAwosType { get; init; } = string.Empty;

    /// <summary>FAA 28-day NASR publication effective date.</summary>
    public DateTime EffectiveDate { get; init; }

    /// <summary>Two-letter state/territory code (e.g., TX, CA).</summary>
    public string? StateCode { get; init; }

    /// <summary>City where the weather station is located.</summary>
    public string? City { get; init; }

    /// <summary>Two-letter country code (e.g., US).</summary>
    public string? CountryCode { get; init; }

    /// <summary>Date the weather station was commissioned (placed into service).</summary>
    public DateTime? CommissionedDate { get; init; }

    /// <summary>Associated NAVAID flag (Y/N). Indicates whether the station is co-located with a NAVAID.</summary>
    public string? NavaidFlag { get; init; }

    /// <summary>Latitude in decimal degrees (positive = north).</summary>
    public decimal? LatDecimal { get; init; }

    /// <summary>Longitude in decimal degrees (negative = west).</summary>
    public decimal? LongDecimal { get; init; }

    /// <summary>Elevation in feet MSL.</summary>
    public decimal? Elevation { get; init; }

    /// <summary>
    /// Survey method code indicating how the station location was determined.
    /// </summary>
    /// <remarks>See <see cref="WeatherStationValues.SurveyMethod"/> for known values (E = Estimated, S = Surveyed).</remarks>
    public string? SurveyMethodCode { get; init; }

    /// <summary>Primary telephone number for the weather station.</summary>
    public string? PhoneNo { get; init; }

    /// <summary>Secondary telephone number for the weather station.</summary>
    public string? SecondPhoneNo { get; init; }

    /// <summary>FAA site number linking the station to its associated landing facility.</summary>
    public string? SiteNo { get; init; }

    /// <summary>
    /// Landing facility type code where the weather station is located.
    /// </summary>
    /// <remarks>See <see cref="WeatherStationValues.SiteType"/> for known values (A = Airport, B = Balloonport, C = Seaplane Base, G = Gliderport, H = Heliport, U = Ultralight).</remarks>
    public string? SiteTypeCode { get; init; }

    /// <summary>Remarks or notes about the weather station from FAA NASR data.</summary>
    public string? Remarks { get; init; }
}
