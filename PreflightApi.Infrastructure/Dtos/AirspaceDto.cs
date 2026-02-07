using System.Text.Json.Serialization;

namespace PreflightApi.Infrastructure.Dtos
{
    /// <summary>
    /// GeoJSON geometry representing an airspace boundary.
    /// </summary>
    public class GeoJsonGeometry
    {
        /// <summary>Geometry type (e.g., Polygon, MultiPolygon).</summary>
        public string Type { get; set; } = string.Empty;
        /// <summary>Coordinate array defining the geometry boundary.</summary>
        public double[][][] Coordinates { get; set; } = [];
    }

    /// <summary>
    /// Controlled airspace data (Class B, C, D, E) from ArcGIS.
    /// </summary>
    public class AirspaceDto
    {
        /// <summary>ArcGIS global unique identifier.</summary>
        public string? GlobalId { get; set; }
        /// <summary>FAA identifier.</summary>
        public string? Ident { get; set; }
        /// <summary>ICAO identifier.</summary>
        public string? IcaoId { get; set; }
        /// <summary>Airspace name.</summary>
        public string? Name { get; set; }
        /// <summary>Upper altitude limit description.</summary>
        public string? UpperDesc { get; set; }
        /// <summary>Upper altitude limit value.</summary>
        public double? UpperVal { get; set; }
        /// <summary>Upper altitude unit of measure (e.g., FT, FL).</summary>
        public string? UpperUom { get; set; }
        /// <summary>Upper altitude reference code (e.g., MSL, AGL).</summary>
        public string? UpperCode { get; set; }
        /// <summary>Lower altitude limit description.</summary>
        public string? LowerDesc { get; set; }
        /// <summary>Lower altitude limit value.</summary>
        public double? LowerVal { get; set; }
        /// <summary>Lower altitude unit of measure (e.g., FT, FL).</summary>
        public string? LowerUom { get; set; }
        /// <summary>Lower altitude reference code (e.g., MSL, AGL).</summary>
        public string? LowerCode { get; set; }
        /// <summary>Airspace type code (e.g., CLASS_B, CLASS_C).</summary>
        public string? TypeCode { get; set; }
        /// <summary>Local airspace type.</summary>
        public string? LocalType { get; set; }
        /// <summary>Airspace class (e.g., B, C, D, E).</summary>
        public string? Class { get; set; }
        /// <summary>Military use code.</summary>
        public string? MilCode { get; set; }
        /// <summary>Communications facility name.</summary>
        public string? CommName { get; set; }
        /// <summary>Level designation (e.g., SURFACE, UPPER).</summary>
        public string? Level { get; set; }
        /// <summary>Sector identifier.</summary>
        public string? Sector { get; set; }
        /// <summary>Whether the airspace is onshore.</summary>
        public string? Onshore { get; set; }
        /// <summary>Exclusion area indicator.</summary>
        public string? Exclusion { get; set; }
        /// <summary>Working hours code.</summary>
        public string? WkhrCode { get; set; }
        /// <summary>Working hours remarks.</summary>
        public string? WkhrRmk { get; set; }
        /// <summary>Daylight saving time indicator.</summary>
        public string? Dst { get; set; }
        /// <summary>GMT offset.</summary>
        public string? GmtOffset { get; set; }
        /// <summary>Controlling agency.</summary>
        public string? ContAgent { get; set; }
        /// <summary>City associated with the airspace.</summary>
        public string? City { get; set; }
        /// <summary>State associated with the airspace.</summary>
        public string? State { get; set; }
        /// <summary>Country code.</summary>
        public string? Country { get; set; }
        /// <summary>Associated aerodrome identifier.</summary>
        public string? AdhpId { get; set; }
        /// <summary>GeoJSON boundary geometry.</summary>
        public GeoJsonGeometry? Geometry { get; set; }
    }

    /// <summary>
    /// Special use airspace data (restricted, prohibited, warning, MOA, alert) from ArcGIS.
    /// </summary>
    public class SpecialUseAirspaceDto
    {
        /// <summary>ArcGIS global unique identifier.</summary>
        public string? GlobalId { get; set; }
        /// <summary>Airspace name.</summary>
        public string? Name { get; set; }
        /// <summary>Type code (e.g., R for restricted, P for prohibited).</summary>
        public string? TypeCode { get; set; }
        /// <summary>Airspace class.</summary>
        public string? Class { get; set; }
        /// <summary>Upper altitude limit description.</summary>
        public string? UpperDesc { get; set; }
        /// <summary>Upper altitude limit value.</summary>
        public string? UpperVal { get; set; }
        /// <summary>Upper altitude unit of measure.</summary>
        public string? UpperUom { get; set; }
        /// <summary>Upper altitude reference code.</summary>
        public string? UpperCode { get; set; }
        /// <summary>Lower altitude limit description.</summary>
        public string? LowerDesc { get; set; }
        /// <summary>Lower altitude limit value.</summary>
        public string? LowerVal { get; set; }
        /// <summary>Lower altitude unit of measure.</summary>
        public string? LowerUom { get; set; }
        /// <summary>Lower altitude reference code.</summary>
        public string? LowerCode { get; set; }
        /// <summary>Level code.</summary>
        public string? LevelCode { get; set; }
        /// <summary>City associated with the airspace.</summary>
        public string? City { get; set; }
        /// <summary>State associated with the airspace.</summary>
        public string? State { get; set; }
        /// <summary>Country code.</summary>
        public string? Country { get; set; }
        /// <summary>Controlling agency.</summary>
        public string? ContAgent { get; set; }
        /// <summary>Communications facility name.</summary>
        public string? CommName { get; set; }
        /// <summary>Sector identifier.</summary>
        public string? Sector { get; set; }
        /// <summary>Whether the airspace is onshore.</summary>
        public string? Onshore { get; set; }
        /// <summary>Exclusion area indicator.</summary>
        public string? Exclusion { get; set; }
        /// <summary>Times of use for the airspace.</summary>
        public string? TimesOfUse { get; set; }
        /// <summary>GMT offset.</summary>
        public string? GmtOffset { get; set; }
        /// <summary>Daylight saving time code.</summary>
        public string? DstCode { get; set; }
        /// <summary>Additional remarks.</summary>
        public string? Remarks { get; set; }
        /// <summary>GeoJSON boundary geometry.</summary>
        public GeoJsonGeometry? Geometry { get; set; }
    }
}
