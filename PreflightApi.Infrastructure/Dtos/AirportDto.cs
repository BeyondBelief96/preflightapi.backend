namespace PreflightApi.Infrastructure.Dtos
{
    /// <summary>
    /// Airport data from the FAA National Airspace System Resources (NASR) database.
    /// Combines data from APT_BASE, APT_ATT, and APT_CON CSV files.
    /// Use the airport's IcaoId or ArptId to query related endpoints such as runways
    /// (<c>GET /api/v1/airports/{icaoCodeOrIdent}/runways</c>), communication frequencies
    /// (<c>GET /api/v1/communication-frequencies/{servicedFacility}</c>), METARs
    /// (<c>GET /api/v1/metars/{icaoCodeOrIdent}</c>), TAFs (<c>GET /api/v1/tafs/{icaoCodeOrIdent}</c>),
    /// airport diagrams (<c>GET /api/v1/airport-diagrams/{icaoCodeOrIdent}</c>), and
    /// chart supplements (<c>GET /api/v1/chart-supplements/{icaoCodeOrIdent}</c>).
    /// </summary>
    public record AirportDto
    {
        /// <summary>FAA NASR field: SITE_NO. Unique Site Number assigned by the FAA to identify the airport facility.</summary>
        public string SiteNo { get; init; } = string.Empty;

        /// <summary>FAA NASR field: ICAO_ID. ICAO (International Civil Aviation Organization) identifier (e.g., KDFW, KLAX).</summary>
        public string? IcaoId { get; init; }

        /// <summary>FAA NASR field: ARPT_ID. FAA location identifier (e.g., DFW, LAX, ORD). Up to 4 characters.</summary>
        public string? ArptId { get; init; }

        /// <summary>FAA NASR field: ARPT_NAME. Official airport facility name.</summary>
        public string? ArptName { get; init; }

        /// <summary>
        /// FAA NASR field: SITE_TYPE_CODE. Landing facility type code.
        /// <para>Possible values: A (Airport), H (Heliport), S (Seaplane Base), G (Gliderport), U (Ultralight).</para>
        /// </summary>
        public string? SiteTypeCode { get; init; }

        /// <summary>FAA NASR field: CITY. Associated city name for the airport.</summary>
        public string? City { get; init; }

        /// <summary>FAA NASR field: STATE_CODE. Two-letter USPS state code where the airport is located.</summary>
        public string? StateCode { get; init; }

        /// <summary>FAA NASR field: COUNTRY_CODE. Two-letter country code.</summary>
        public string? CountryCode { get; init; }

        /// <summary>FAA NASR field: STATE_NAME. Full state name where the airport is located.</summary>
        public string? StateName { get; init; }

        /// <summary>FAA NASR field: LAT_DECIMAL. Latitude of airport reference point in decimal degrees.</summary>
        public decimal? LatDecimal { get; init; }

        /// <summary>FAA NASR field: LONG_DECIMAL. Longitude of airport reference point in decimal degrees.</summary>
        public decimal? LongDecimal { get; init; }

        /// <summary>FAA NASR field: ELEV. Airport elevation in feet above Mean Sea Level (MSL), to the nearest tenth of a foot.</summary>
        public decimal? Elev { get; init; }

        /// <summary>FAA NASR field: MAG_VARN. Magnetic variation in degrees. Combine with MagHemis to determine east/west deviation.</summary>
        public decimal? MagVarn { get; init; }

        /// <summary>
        /// FAA NASR field: MAG_HEMIS. Magnetic variation hemisphere.
        /// <para>Possible values: E (East - add to true heading for magnetic), W (West - subtract from true heading for magnetic).</para>
        /// </summary>
        public string? MagHemis { get; init; }

        /// <summary>FAA NASR field: CHART_NAME. Sectional aeronautical chart name on which the airport appears.</summary>
        public string? ChartName { get; init; }

        /// <summary>
        /// FAA NASR field: ARPT_STATUS. Airport operational status.
        /// <para>Possible values: O (Operational), CI (Closed Indefinitely), CP (Closed Permanently).</para>
        /// </summary>
        public string? ArptStatus { get; init; }

        /// <summary>FAA NASR field: FUEL_TYPES. Available fuel types at the airport. Comma-separated list (e.g., 100LL, JET-A, MOGAS).</summary>
        public string? FuelTypes { get; init; }

        /// <summary>FAA NASR field: LAST_INSPECTION. Date of the last physical inspection (YYYY/MM/DD).</summary>
        public DateTime? LastInspection { get; init; }

        /// <summary>FAA NASR field: LAST_INFO_RESPONSE. Date of the last information request response (YYYY/MM/DD).</summary>
        public DateTime? LastInfoResponse { get; init; }

        /// <summary>
        /// FAA NASR field: CUST_FLAG. Customs airport of entry flag.
        /// <para>Possible values: Y (Yes, airport of entry), N (No).</para>
        /// </summary>
        public string? CustomsFlag { get; init; }

        /// <summary>
        /// FAA NASR field: LNDG_RIGHTS_FLAG. Customs landing rights flag.
        /// <para>Possible values: Y (Yes, airport has landing rights), N (No).</para>
        /// </summary>
        public string? LndgRightsFlag { get; init; }

        /// <summary>
        /// FAA NASR field: JOINT_USE_FLAG. Joint use agreement between military and civil.
        /// <para>Possible values: Y (Yes, joint civil/military use), N (No).</para>
        /// </summary>
        public string? JointUseFlag { get; init; }

        /// <summary>
        /// FAA NASR field: MIL_LNDG_FLAG. Military landing rights agreement.
        /// <para>Possible values: Y (Yes, military landing rights), N (No).</para>
        /// </summary>
        public string? MilLndgFlag { get; init; }

        /// <summary>FAA NASR field: TITLE (APT_CON). Title of the facility contact (e.g., MANAGER, OWNER, ASST-MGR).</summary>
        public string? ContactTitle { get; init; }

        /// <summary>FAA NASR field: NAME (APT_CON). Facility contact name for the title.</summary>
        public string? ContactName { get; init; }

        /// <summary>FAA NASR field: ADDRESS1 (APT_CON). Contact address line 1.</summary>
        public string? ContactAddress1 { get; init; }

        /// <summary>FAA NASR field: ADDRESS2 (APT_CON). Contact address line 2.</summary>
        public string? ContactAddress2 { get; init; }

        /// <summary>FAA NASR field: TITLE_CITY (APT_CON). Contact city.</summary>
        public string? ContactCity { get; init; }

        /// <summary>FAA NASR field: STATE (APT_CON). Contact state.</summary>
        public string? ContactState { get; init; }

        /// <summary>FAA NASR field: ZIP_CODE (APT_CON). Contact ZIP code.</summary>
        public string? ContactZipCode { get; init; }

        /// <summary>FAA NASR field: ZIP_PLUS_FOUR (APT_CON). Contact ZIP+4 code.</summary>
        public string? ContactZipPlusFour { get; init; }

        /// <summary>FAA NASR field: PHONE_NO (APT_CON). Contact phone number.</summary>
        public string? ContactPhoneNumber { get; init; }
    }
}
