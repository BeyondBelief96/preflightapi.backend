namespace PreflightApi.Domain.Constants;

/// <summary>
/// Known constrained values for Navigational Aid (NAV) fields, sourced from the FAA NASR Data Layout.
/// </summary>
public static class NavaidValues
{
    /// <summary>
    /// NAVAID Facility Type (NAV_TYPE) — describes the type of navigational aid.
    /// </summary>
    public static class NavType
    {
        /// <summary>A Low Frequency, Long-Distance NAVAID used principally for transoceanic navigation.</summary>
        public const string Consolan = "CONSOLAN";
        /// <summary>Distance Measuring Equipment only.</summary>
        public const string Dme = "DME";
        /// <summary>EN ROUTE Marker Beacon providing positive identification of positions along airways.</summary>
        public const string FanMarker = "FAN MARKER";
        /// <summary>NON Directional Beacon used primarily for Marine (surface) navigation.</summary>
        public const string MarineNdb = "MARINE NDB";
        /// <summary>NON Directional Beacon with associated DME; used primarily for Marine navigation.</summary>
        public const string MarineNdbDme = "MARINE NDB/DME";
        /// <summary>NON Directional Beacon.</summary>
        public const string Ndb = "NDB";
        /// <summary>NON Directional Beacon with associated Distance Measuring Equipment.</summary>
        public const string NdbDme = "NDB/DME";
        /// <summary>Tactical Air Navigation System providing Azimuth and Slant Range Distance.</summary>
        public const string Tacan = "TACAN";
        /// <summary>Ultra High Frequency NON Directional Beacon.</summary>
        public const string UhfNdb = "UHF/NDB";
        /// <summary>VHF OMNI-Directional Range providing Azimuth only.</summary>
        public const string Vor = "VOR";
        /// <summary>VOR and TACAN combined facility providing VOR Azimuth, TACAN Azimuth and DME.</summary>
        public const string Vortac = "VORTAC";
        /// <summary>VHF OMNI-Directional Range with associated Distance Measuring Equipment.</summary>
        public const string VorDme = "VOR/DME";
        /// <summary>FAA VOR Test Facility.</summary>
        public const string Vot = "VOT";

        /// <summary>All known NAV_TYPE values.</summary>
        public static readonly string[] All = { Consolan, Dme, FanMarker, MarineNdb, MarineNdbDme, Ndb, NdbDme, Tacan, UhfNdb, Vor, Vortac, VorDme, Vot };
    }

    /// <summary>
    /// NAVAID operational status (NAV_STATUS).
    /// </summary>
    public static class NavStatus
    {
        public const string OperationalIfr = "OPERATIONAL IFR";
        public const string OperationalRestricted = "OPERATIONAL RESTRICTED";
        public const string OperationalVfrOnly = "OPERATIONAL VFR ONLY";
        public const string Shutdown = "SHUTDOWN";

        public static readonly string[] All = { OperationalIfr, OperationalRestricted, OperationalVfrOnly, Shutdown };
    }

    /// <summary>
    /// FAA Region codes (REGION_CODE).
    /// </summary>
    public static class RegionCode
    {
        /// <summary>Alaska</summary>
        public const string Alaska = "AAL";
        /// <summary>Central</summary>
        public const string Central = "ACE";
        /// <summary>Eastern</summary>
        public const string Eastern = "AEA";
        /// <summary>Great Lakes</summary>
        public const string GreatLakes = "AGL";
        /// <summary>New England</summary>
        public const string NewEngland = "ANE";
        /// <summary>Northwest Mountain</summary>
        public const string NorthwestMountain = "ANM";
        /// <summary>Southern</summary>
        public const string Southern = "ASO";
        /// <summary>Southwest</summary>
        public const string Southwest = "ASW";
        /// <summary>Western-Pacific</summary>
        public const string WesternPacific = "AWP";

        public static readonly string[] All = { Alaska, Central, Eastern, GreatLakes, NewEngland, NorthwestMountain, Southern, Southwest, WesternPacific };
    }

    /// <summary>
    /// Latitude/Longitude Survey Accuracy codes (SURVEY_ACCURACY_CODE).
    /// </summary>
    public static class SurveyAccuracy
    {
        /// <summary>Unknown accuracy.</summary>
        public const string Unknown = "0";
        /// <summary>Degree accuracy.</summary>
        public const string Degree = "1";
        /// <summary>10 Minutes accuracy.</summary>
        public const string TenMinutes = "2";
        /// <summary>1 Minute accuracy.</summary>
        public const string OneMinute = "3";
        /// <summary>10 Seconds accuracy.</summary>
        public const string TenSeconds = "4";
        /// <summary>1 Second or better accuracy.</summary>
        public const string OneSecondOrBetter = "5";
        /// <summary>NOS accuracy.</summary>
        public const string Nos = "6";
        /// <summary>3rd Order Triangulation.</summary>
        public const string ThirdOrderTriangulation = "7";

        public static readonly string[] All = { Unknown, Degree, TenMinutes, OneMinute, TenSeconds, OneSecondOrBetter, Nos, ThirdOrderTriangulation };
    }

    /// <summary>
    /// Monitoring Category codes (MNT_CAT_CODE). Defines how the NAVAID is monitored.
    /// </summary>
    public static class MonitoringCategory
    {
        /// <summary>Internal monitoring plus status indicator at control point.</summary>
        public const string Category1 = "1";
        /// <summary>Internal monitoring with status indicator inoperative but facility operating normally.</summary>
        public const string Category2 = "2";
        /// <summary>Internal monitoring only, no status indicator at control point.</summary>
        public const string Category3 = "3";
        /// <summary>Internal monitor not installed, remote status indicator at control point (NDB only).</summary>
        public const string Category4 = "4";

        public static readonly string[] All = { Category1, Category2, Category3, Category4 };
    }

    /// <summary>
    /// VOR Standard Service Volume codes (ALT_CODE).
    /// </summary>
    public static class AltitudeServiceVolume
    {
        /// <summary>High Altitude: 1,000'-14,499' = 40NM; 14,500'-17,999' = 100NM; 18,000'-FL450 = 130NM.</summary>
        public const string High = "H";
        /// <summary>Low Altitude: 1,000'-18,000' = 40NM.</summary>
        public const string Low = "L";
        /// <summary>Terminal: 1,000'-12,000' = 25NM.</summary>
        public const string Terminal = "T";
        /// <summary>VOR High: expanded service volume.</summary>
        public const string VorHigh = "VH";
        /// <summary>VOR Low: 1,000'-4,999' = 40NM; 5,000'-17,999' = 70NM.</summary>
        public const string VorLow = "VL";

        public static readonly string[] All = { High, Low, Terminal, VorHigh, VorLow };
    }

    /// <summary>
    /// DME Standard Service Volume codes (DME_SSV).
    /// </summary>
    public static class DmeServiceVolume
    {
        /// <summary>High Altitude.</summary>
        public const string High = "H";
        /// <summary>Low Altitude.</summary>
        public const string Low = "L";
        /// <summary>Terminal.</summary>
        public const string Terminal = "T";
        /// <summary>DME High: 12,900'-FL450 = 130NM.</summary>
        public const string DmeHigh = "DH";
        /// <summary>DME Low: 12,900'-18,000' = 130NM.</summary>
        public const string DmeLow = "DL";

        public static readonly string[] All = { High, Low, Terminal, DmeHigh, DmeLow };
    }
}
