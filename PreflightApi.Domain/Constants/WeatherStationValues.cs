namespace PreflightApi.Domain.Constants;

/// <summary>
/// Known constrained values for Weather Station (ASOS/AWOS) fields, sourced from the FAA NASR Data Layout.
/// </summary>
public static class WeatherStationValues
{
    /// <summary>
    /// Weather system sensor type (ASOS_AWOS_TYPE).
    /// </summary>
    public static class SensorType
    {
        /// <summary>Automated Surface Observing System.</summary>
        public const string Asos = "ASOS";
        /// <summary>Automated Weather Observing System — altimeter setting only.</summary>
        public const string Awos1 = "AWOS-1";
        /// <summary>AWOS-1 plus wind and temperature.</summary>
        public const string Awos2 = "AWOS-2";
        /// <summary>AWOS-2 plus sky condition and ceiling, visibility, and precipitation identification.</summary>
        public const string Awos3 = "AWOS-3";
        /// <summary>AWOS-3 with precipitation accumulation.</summary>
        public const string Awos3P = "AWOS-3P";
        /// <summary>AWOS-3 with precipitation accumulation and thunderstorm/lightning detection.</summary>
        public const string Awos3Pt = "AWOS-3PT";
        /// <summary>AWOS-3 with thunderstorm/lightning detection.</summary>
        public const string Awos3T = "AWOS-3T";
        /// <summary>AWOS-3 with additional sensors (freezing rain, runway surface condition).</summary>
        public const string Awos4 = "AWOS-4";
        /// <summary>AWOS providing altimeter setting only.</summary>
        public const string AwosA = "AWOS-A";
        /// <summary>AWOS providing altimeter setting and visibility.</summary>
        public const string AwosAv = "AWOS-AV";

        /// <summary>All known ASOS_AWOS_TYPE values.</summary>
        public static readonly string[] All = { Asos, Awos1, Awos2, Awos3, Awos3P, Awos3Pt, Awos3T, Awos4, AwosA, AwosAv };
    }

    /// <summary>
    /// Survey method codes (SURVEY_METHOD_CODE) — how the weather station location was determined.
    /// </summary>
    public static class SurveyMethod
    {
        /// <summary>Location was estimated.</summary>
        public const string Estimated = "E";
        /// <summary>Location was surveyed.</summary>
        public const string Surveyed = "S";

        public static readonly string[] All = { Estimated, Surveyed };
    }

    /// <summary>
    /// Landing facility type codes (SITE_TYPE_CODE) — type of facility where the weather station is located.
    /// </summary>
    public static class SiteType
    {
        /// <summary>Airport.</summary>
        public const string Airport = "A";
        /// <summary>Balloonport.</summary>
        public const string Balloonport = "B";
        /// <summary>Seaplane Base.</summary>
        public const string SeaplaneBase = "C";
        /// <summary>Gliderport.</summary>
        public const string Gliderport = "G";
        /// <summary>Heliport.</summary>
        public const string Heliport = "H";
        /// <summary>Ultralight.</summary>
        public const string Ultralight = "U";

        /// <summary>All known SITE_TYPE_CODE values.</summary>
        public static readonly string[] All = { Airport, Balloonport, SeaplaneBase, Gliderport, Heliport, Ultralight };
    }
}
