using System.Text.Json.Serialization;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Dtos.Notam;

/// <summary>
/// Represents a NOTAM as a GeoJSON Feature from the NMS API.
/// See nms-api.yaml NmsNotamData schema for full specification.
/// </summary>
public record NotamDto
{
    /// <summary>GeoJSON type, always "Feature".</summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "Feature";

    /// <summary>Unique NOTAM feature identifier.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>GeoJSON geometry representing the NOTAM's geographic location or affected area (Point, Polygon, or GeometryCollection).</summary>
    [JsonPropertyName("geometry")]
    public NotamGeometryDto? Geometry { get; init; }

    /// <summary>NOTAM properties containing the core NOTAM data, detail fields, and translations.</summary>
    [JsonPropertyName("properties")]
    public NotamPropertiesDto? Properties { get; init; }
}

/// <summary>
/// GeoJSON geometry - can be Point, Polygon, or GeometryCollection
/// </summary>
public record NotamGeometryDto
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// For Point: [lon, lat] array
    /// For Polygon: array of coordinate rings
    /// For GeometryCollection: null (use Geometries instead)
    /// </summary>
    [JsonPropertyName("coordinates")]
    public object? Coordinates { get; init; }

    /// <summary>
    /// For GeometryCollection type - contains child geometries
    /// </summary>
    [JsonPropertyName("geometries")]
    public List<NotamGeometryDto>? Geometries { get; init; }
}

/// <summary>
/// Properties of a NOTAM GeoJSON Feature containing the core NOTAM data.
/// </summary>
public record NotamPropertiesDto
{
    /// <summary>Core NOTAM data including the event metadata, detail fields, and translations.</summary>
    [JsonPropertyName("coreNOTAMData")]
    public CoreNotamDataDto? CoreNotamData { get; init; }
}

/// <summary>
/// Core NOTAM data structure containing event metadata, the NOTAM detail, and any translations.
/// </summary>
public record CoreNotamDataDto
{
    /// <summary>NOTAM event metadata (encoding format and scenario).</summary>
    [JsonPropertyName("notamEvent")]
    public NotamEventDto? NotamEvent { get; init; }

    /// <summary>The NOTAM detail fields including identifier, text, effective dates, location, and classification.</summary>
    [JsonPropertyName("notam")]
    public NotamDetailDto? Notam { get; init; }

    /// <summary>NOTAM text translations in various formats (plain English, domestic format, ICAO format).</summary>
    [JsonPropertyName("notamTranslation")]
    public List<NotamTranslationDto>? NotamTranslation { get; init; }
}

/// <summary>
/// NOTAM event metadata
/// </summary>
public record NotamEventDto
{
    [JsonPropertyName("encoding")]
    public string? Encoding { get; init; }

    [JsonPropertyName("scenario")]
    public string? Scenario { get; init; }
}

/// <summary>
/// Core NOTAM detail fields per NMS API specification.
/// Classification values: INTERNATIONAL, MILITARY, LOCAL_MILITARY, DOMESTIC, FDC
/// Feature values: RWY, TWY, APRON, AD, OBST, NAV, COM, SVC, AIRSPACE, ODP, SID, STAR, CHART, DATA, DVA, IAP, VFP, ROUTE, SPECIAL, SECURITY
/// Series values: A, B, C, D, E, G, H, I, J, K, N, R, V, Z
/// </summary>
public record NotamDetailDto
{
    /// <summary>
    /// Unique 16-digit NMS identifier
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>
    /// NOTAM number as returned by the NMS API. Typically a bare sequence number (e.g., "420", "3997")
    /// but may include a month prefix (e.g., "01/123") for domestic NOTAMs or a series prefix
    /// (e.g., "A1234/25") for ICAO NOTAMs. Stored as-is from the source; the denormalized
    /// <c>notam_number</c> column on the entity strips any month prefix for indexed search.
    /// </summary>
    [JsonPropertyName("number")]
    public string? Number { get; init; }

    /// <summary>
    /// ICAO series code (A, B, C, D, E, G, H, I, J, K, N, R, V, Z)
    /// </summary>
    [JsonPropertyName("series")]
    public string? Series { get; init; }

    /// <summary>
    /// 4-digit NOTAM year (e.g., "2025"). Denormalized to the <c>notam_year</c> entity column for indexed search.
    /// </summary>
    [JsonPropertyName("year")]
    public string? Year { get; init; }

    /// <summary>
    /// NOTAM type (N=New, R=Replace, C=Cancel)
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(SafeEnumJsonConverter<NotamType>))]
    public NotamType? Type { get; init; }

    /// <summary>
    /// Feature category: RWY, TWY, APRON, AD, OBST, NAV, COM, SVC, AIRSPACE, ODP, SID, STAR, CHART, DATA, DVA, IAP, VFP, ROUTE, SPECIAL, SECURITY
    /// </summary>
    [JsonPropertyName("feature")]
    [JsonConverter(typeof(SafeEnumJsonConverter<NotamFeature>))]
    public NotamFeature? Feature { get; init; }

    /// <summary>
    /// Issuance timestamp (ISO 8601)
    /// </summary>
    [JsonPropertyName("issued")]
    public string? Issued { get; init; }

    /// <summary>
    /// Affected FIR/ARTCC (e.g., "ZTL")
    /// </summary>
    [JsonPropertyName("affectedFir")]
    public string? AffectedFir { get; init; }

    /// <summary>
    /// Q-code selection code (e.g., "QXXX")
    /// </summary>
    [JsonPropertyName("selectionCode")]
    public string? SelectionCode { get; init; }

    /// <summary>
    /// Traffic type (I=IFR, V=VFR, IV=Both)
    /// </summary>
    [JsonPropertyName("traffic")]
    [JsonConverter(typeof(SafeEnumJsonConverter<NotamTraffic>))]
    public NotamTraffic? Traffic { get; init; }

    /// <summary>
    /// Purpose code (e.g., "BO")
    /// </summary>
    [JsonPropertyName("purpose")]
    public string? Purpose { get; init; }

    /// <summary>
    /// Scope (A=Aerodrome, E=En-route, W=Navigation warning)
    /// </summary>
    [JsonPropertyName("scope")]
    [JsonConverter(typeof(SafeEnumJsonConverter<NotamScope>))]
    public NotamScope? Scope { get; init; }

    /// <summary>
    /// Minimum flight level (e.g., "000")
    /// </summary>
    [JsonPropertyName("minimumFl")]
    public string? MinimumFl { get; init; }

    /// <summary>
    /// Maximum flight level (e.g., "999")
    /// </summary>
    [JsonPropertyName("maximumFl")]
    public string? MaximumFl { get; init; }

    /// <summary>
    /// Domestic location identifier (e.g., "CLT")
    /// </summary>
    [JsonPropertyName("location")]
    public string? Location { get; init; }

    /// <summary>
    /// ICAO location identifier (e.g., "KCLT")
    /// </summary>
    [JsonPropertyName("icaoLocation")]
    public string? IcaoLocation { get; init; }

    /// <summary>
    /// Airport/facility name (AIXM-only, e.g., "JOHN C TUNE"). Null for GeoJSON-sourced NOTAMs.
    /// </summary>
    [JsonPropertyName("airportName")]
    public string? AirportName { get; init; }

    /// <summary>
    /// Effective start timestamp (ISO 8601)
    /// </summary>
    [JsonPropertyName("effectiveStart")]
    public string? EffectiveStart { get; init; }

    /// <summary>
    /// Effective end timestamp (ISO 8601)
    /// </summary>
    [JsonPropertyName("effectiveEnd")]
    public string? EffectiveEnd { get; init; }

    /// <summary>
    /// Whether end time is estimated.
    /// </summary>
    [JsonPropertyName("estimated")]
    [JsonConverter(typeof(StringBoolJsonConverter))]
    public bool? Estimated { get; init; }

    /// <summary>
    /// NOTAM text content
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; init; }

    /// <summary>
    /// Classification: INTERNATIONAL, MILITARY, LOCAL_MILITARY, DOMESTIC, FDC
    /// </summary>
    [JsonPropertyName("classification")]
    [JsonConverter(typeof(SafeEnumJsonConverter<NotamClassification>))]
    public NotamClassification? Classification { get; init; }

    /// <summary>
    /// Cancellation date timestamp (ISO 8601)
    /// </summary>
    [JsonPropertyName("cancelationDate")]
    public string? CancelationDate { get; init; }

    /// <summary>
    /// Accountability ID — the issuing office code (e.g., "BNA", "FDC", "CLT").
    /// 3-4 characters for domestic NOTAMs, up to 8 characters for AFTN.
    /// Denormalized to the <c>account_id</c> entity column for indexed search.
    /// </summary>
    [JsonPropertyName("accountId")]
    public string? AccountId { get; init; }

    /// <summary>
    /// Origin identifier from FAA FNSE extension
    /// </summary>
    [JsonPropertyName("originId")]
    public string? OriginId { get; init; }

    /// <summary>
    /// Last updated timestamp (ISO 8601)
    /// </summary>
    [JsonPropertyName("lastUpdated")]
    public string? LastUpdated { get; init; }

    /// <summary>
    /// Schedule string (e.g., "Daily:1200-1230~DLY 1200-1230")
    /// </summary>
    [JsonPropertyName("schedule")]
    public string? Schedule { get; init; }

    /// <summary>
    /// Lower altitude limit (e.g., "SFC", "3000FT")
    /// </summary>
    [JsonPropertyName("lowerLimit")]
    public string? LowerLimit { get; init; }

    /// <summary>
    /// Upper altitude limit (e.g., "280M", "FL180")
    /// </summary>
    [JsonPropertyName("upperLimit")]
    public string? UpperLimit { get; init; }

    /// <summary>
    /// ICAO coordinates string (e.g., "3939N04302E")
    /// </summary>
    [JsonPropertyName("coordinates")]
    public string? Coordinates { get; init; }

    /// <summary>
    /// Radius in nautical miles
    /// </summary>
    [JsonPropertyName("radius")]
    public string? Radius { get; init; }
}

/// <summary>
/// NOTAM translation in various formats.
/// Type values: LOCAL_FORMAT, ICAO
/// </summary>
public record NotamTranslationDto
{
    /// <summary>
    /// Translation type: LOCAL_FORMAT or ICAO
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>
    /// Simple text translation (plain English)
    /// </summary>
    [JsonPropertyName("simpleText")]
    public string? SimpleText { get; init; }

    /// <summary>
    /// Domestic format message (for LOCAL_FORMAT type)
    /// </summary>
    [JsonPropertyName("domestic_message")]
    public string? DomesticMessage { get; init; }

    /// <summary>
    /// ICAO format message (for ICAO type)
    /// </summary>
    [JsonPropertyName("icao_message")]
    public string? IcaoMessage { get; init; }

    /// <summary>
    /// Formatted text (HTML or rich text)
    /// </summary>
    [JsonPropertyName("formattedText")]
    public string? FormattedText { get; init; }
}
