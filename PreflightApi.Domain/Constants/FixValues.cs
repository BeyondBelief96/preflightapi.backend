namespace PreflightApi.Domain.Constants;

/// <summary>
/// Known constrained values for Fix/Reporting Point (FIX) fields, sourced from the FAA NASR Data Layout.
/// </summary>
public static class FixValues
{
    /// <summary>
    /// Fix use/type codes (FIX_USE_CODE).
    /// </summary>
    public static class UseCode
    {
        /// <summary>Computer Navigation Fix.</summary>
        public const string ComputerNavigation = "CN";
        /// <summary>Military Reporting Point.</summary>
        public const string MilitaryReportingPoint = "MR";
        /// <summary>Military Waypoint.</summary>
        public const string MilitaryWaypoint = "MW";
        /// <summary>NRS Waypoint.</summary>
        public const string NrsWaypoint = "NRS";
        /// <summary>Radar fix.</summary>
        public const string Radar = "RADAR";
        /// <summary>Reporting Point.</summary>
        public const string ReportingPoint = "RP";
        /// <summary>VFR Waypoint.</summary>
        public const string VfrWaypoint = "VFR";
        /// <summary>Waypoint.</summary>
        public const string Waypoint = "WP";

        /// <summary>All known FIX_USE_CODE values.</summary>
        public static readonly string[] All = { ComputerNavigation, MilitaryReportingPoint, MilitaryWaypoint, NrsWaypoint, Radar, ReportingPoint, VfrWaypoint, Waypoint };
    }

    /// <summary>
    /// Compulsory fix type (COMPULSORY). Null indicates a non-compulsory fix.
    /// </summary>
    public static class Compulsory
    {
        /// <summary>Compulsory on high altitude structure.</summary>
        public const string High = "HIGH";
        /// <summary>Compulsory on low altitude structure.</summary>
        public const string Low = "LOW";
        /// <summary>Compulsory on both low and high altitude structures.</summary>
        public const string LowHigh = "LOW/HIGH";

        public static readonly string[] All = { High, Low, LowHigh };
    }
}
