using PreflightApi.Domain.Enums;

namespace PreflightApi.Infrastructure.Dtos;

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
    // ── Identification ──────────────────────────────────────────────────

    /// <summary>FAA NASR field: SITE_NO. Unique Site Number assigned by the FAA to identify the airport facility.</summary>
    public string SiteNo { get; init; } = string.Empty;

    /// <summary>FAA NASR field: ICAO_ID. ICAO (International Civil Aviation Organization) identifier (e.g., KDFW, KLAX).</summary>
    public string? IcaoId { get; init; }

    /// <summary>FAA NASR field: ARPT_ID. FAA location identifier (e.g., DFW, LAX, ORD). Up to 4 characters.</summary>
    public string? ArptId { get; init; }

    /// <summary>FAA NASR field: ARPT_NAME. Official airport facility name.</summary>
    public string? ArptName { get; init; }

    /// <summary>FAA NASR field: EFF_DATE. Effective date of the airport information.</summary>
    public DateTime EffDate { get; init; }

    // ── Classification ──────────────────────────────────────────────────

    /// <summary>FAA NASR field: SITE_TYPE_CODE. Landing facility type.</summary>
    public AirportSiteType SiteType { get; init; }

    /// <summary>FAA NASR field: OWNERSHIP_TYPE_CODE. Airport ownership type.</summary>
    public AirportOwnershipType OwnershipType { get; init; }

    /// <summary>FAA NASR field: FACILITY_USE_CODE. Facility use designation.</summary>
    public AirportFacilityUse FacilityUse { get; init; }

    /// <summary>FAA NASR field: ARPT_STATUS. Airport operational status.</summary>
    public AirportStatus ArptStatus { get; init; }

    /// <summary>FAA NASR field: NASP_CODE. National Plan of Integrated Airport Systems (NPIAS) or Federal/Military Airport code.</summary>
    public string? NaspCode { get; init; }

    // ── Location ────────────────────────────────────────────────────────

    /// <summary>FAA NASR field: CITY. Associated city name for the airport.</summary>
    public string? City { get; init; }

    /// <summary>FAA NASR field: STATE_CODE. Two-letter USPS state code where the airport is located.</summary>
    public string? StateCode { get; init; }

    /// <summary>FAA NASR field: COUNTRY_CODE. Two-letter country code.</summary>
    public string? CountryCode { get; init; }

    /// <summary>FAA NASR field: STATE_NAME. Full state name where the airport is located.</summary>
    public string? StateName { get; init; }

    /// <summary>FAA NASR field: REGION_CODE. FAA region code (e.g., ASW, AEA, AWP).</summary>
    public string? RegionCode { get; init; }

    /// <summary>FAA NASR field: ADO_CODE. FAA Airports District Office code.</summary>
    public string? AdoCode { get; init; }

    /// <summary>FAA NASR field: COUNTY_NAME. County name where the airport is located.</summary>
    public string? CountyName { get; init; }

    /// <summary>FAA NASR field: COUNTY_ASSOC_STATE. Two-letter state code associated with the county.</summary>
    public string? CountyAssocState { get; init; }

    /// <summary>FAA NASR field: DIST_CITY_TO_AIRPORT. Distance from the associated city to the airport, in nautical miles.</summary>
    public decimal? DistCityToAirport { get; init; }

    /// <summary>FAA NASR field: DIRECTION_CODE. Compass direction from the associated city to the airport (e.g., N, NE, SW).</summary>
    public string? DirectionCode { get; init; }

    /// <summary>FAA NASR field: ACREAGE. Airport acreage.</summary>
    public int? Acreage { get; init; }

    // ── Coordinates ─────────────────────────────────────────────────────

    /// <summary>FAA NASR field: LAT_DECIMAL. Latitude of airport reference point in decimal degrees.</summary>
    public decimal? LatDecimal { get; init; }

    /// <summary>FAA NASR field: LONG_DECIMAL. Longitude of airport reference point in decimal degrees.</summary>
    public decimal? LongDecimal { get; init; }

    /// <summary>FAA NASR field: LAT_DEG. Latitude degrees of airport reference point.</summary>
    public int? LatDeg { get; init; }

    /// <summary>FAA NASR field: LAT_MIN. Latitude minutes of airport reference point.</summary>
    public int? LatMin { get; init; }

    /// <summary>FAA NASR field: LAT_SEC. Latitude seconds of airport reference point.</summary>
    public decimal? LatSec { get; init; }

    /// <summary>FAA NASR field: LAT_HEMIS. Latitude hemisphere of airport reference point (N or S).</summary>
    public string? LatHemis { get; init; }

    /// <summary>FAA NASR field: LONG_DEG. Longitude degrees of airport reference point.</summary>
    public int? LongDeg { get; init; }

    /// <summary>FAA NASR field: LONG_MIN. Longitude minutes of airport reference point.</summary>
    public int? LongMin { get; init; }

    /// <summary>FAA NASR field: LONG_SEC. Longitude seconds of airport reference point.</summary>
    public decimal? LongSec { get; init; }

    /// <summary>FAA NASR field: LONG_HEMIS. Longitude hemisphere of airport reference point (E or W).</summary>
    public string? LongHemis { get; init; }

    /// <summary>FAA NASR field: SURVEY_METHOD_CODE. Method used to determine the airport reference point position.</summary>
    public SurveyMethod PositionSurveyMethod { get; init; }

    /// <summary>FAA NASR field: ARPT_PSN_SOURCE. Source of the airport position information.</summary>
    public string? ArptPsnSource { get; init; }

    /// <summary>FAA NASR field: POSITION_SRC_DATE. Date the airport position information was determined.</summary>
    public DateTime? PositionSrcDate { get; init; }

    // ── Elevation & Magnetic Variation ──────────────────────────────────

    /// <summary>FAA NASR field: ELEV. Airport elevation in feet above Mean Sea Level (MSL), to the nearest tenth of a foot.</summary>
    public decimal? Elev { get; init; }

    /// <summary>FAA NASR field: ELEV_METHOD_CODE. Method used to determine the airport elevation.</summary>
    public SurveyMethod ElevationSurveyMethod { get; init; }

    /// <summary>FAA NASR field: ARPT_ELEV_SOURCE. Source of the airport elevation information.</summary>
    public string? ArptElevSource { get; init; }

    /// <summary>FAA NASR field: ELEVATION_SRC_DATE. Date the airport elevation information was determined.</summary>
    public DateTime? ElevationSrcDate { get; init; }

    /// <summary>FAA NASR field: MAG_VARN. Magnetic variation in degrees.</summary>
    public decimal? MagVarn { get; init; }

    /// <summary>FAA NASR field: MAG_HEMIS. Magnetic variation hemisphere (E or W).</summary>
    public string? MagHemis { get; init; }

    /// <summary>FAA NASR field: MAG_VARN_YEAR. Year the magnetic variation was determined.</summary>
    public int? MagVarnYear { get; init; }

    /// <summary>FAA NASR field: TPA. Traffic Pattern Altitude in feet above Mean Sea Level (MSL).</summary>
    public int? Tpa { get; init; }

    // ── Charting ────────────────────────────────────────────────────────

    /// <summary>FAA NASR field: CHART_NAME. Sectional aeronautical chart name on which the airport appears.</summary>
    public string? ChartName { get; init; }

    // ── ATC & Communication ─────────────────────────────────────────────

    /// <summary>FAA NASR field: RESP_ARTCC_ID. Identifier of the responsible Air Route Traffic Control Center (ARTCC).</summary>
    public string? RespArtccId { get; init; }

    /// <summary>FAA NASR field: TWR_TYPE_CODE. Air Traffic Control Tower type (e.g., NON-ATCT, ATCT).</summary>
    public string? TwrTypeCode { get; init; }

    /// <summary>FAA NASR field: FSS_ON_ARPT_FLAG. Whether a Flight Service Station (FSS) is located on the airport.</summary>
    public bool FssOnAirport { get; init; }

    /// <summary>FAA NASR field: FSS_ID. Identifier of the Flight Service Station (FSS) serving the airport.</summary>
    public string? FssId { get; init; }

    /// <summary>FAA NASR field: FSS_NAME. Name of the Flight Service Station (FSS) serving the airport.</summary>
    public string? FssName { get; init; }

    /// <summary>FAA NASR field: NOTAM_ID. NOTAM facility identifier.</summary>
    public string? NotamId { get; init; }

    /// <summary>FAA NASR field: NOTAM_FLAG. Whether NOTAM service is available.</summary>
    public bool NotamAvailable { get; init; }

    // ── Customs & Military ──────────────────────────────────────────────

    /// <summary>FAA NASR field: CUST_FLAG. Whether the airport is a customs port of entry.</summary>
    public bool CustomsPortOfEntry { get; init; }

    /// <summary>FAA NASR field: LNDG_RIGHTS_FLAG. Whether the airport has customs landing rights.</summary>
    public bool CustomsLandingRights { get; init; }

    /// <summary>FAA NASR field: JOINT_USE_FLAG. Whether a joint civil/military use agreement exists.</summary>
    public bool JointUse { get; init; }

    /// <summary>FAA NASR field: MIL_LNDG_FLAG. Whether military landing rights exist.</summary>
    public bool MilitaryLandingRights { get; init; }

    // ── Inspection ──────────────────────────────────────────────────────

    /// <summary>FAA NASR field: INSPECT_METHOD_CODE. Airport inspection method.</summary>
    public AirportInspectionMethod InspectionMethod { get; init; }

    /// <summary>FAA NASR field: INSPECTOR_CODE. Agency performing the airport inspection.</summary>
    public AirportInspectorAgency InspectorAgency { get; init; }

    /// <summary>FAA NASR field: LAST_INSPECTION. Date of the last physical inspection.</summary>
    public DateTime? LastInspection { get; init; }

    /// <summary>FAA NASR field: LAST_INFO_RESPONSE. Date of the last information request response.</summary>
    public DateTime? LastInfoResponse { get; init; }

    // ── Services & Fuel ─────────────────────────────────────────────────

    /// <summary>FAA NASR field: FUEL_TYPES. Available fuel types (e.g., 100LL, JET-A, MOGAS).</summary>
    public string? FuelTypes { get; init; }

    /// <summary>FAA NASR field: CONTR_FUEL_AVBL. Whether contract fuel is available.</summary>
    public bool ContractFuelAvailable { get; init; }

    /// <summary>FAA NASR field: AIRFRAME_REPAIR_SER_CODE. Airframe repair service availability.</summary>
    public RepairServiceAvailability AirframeRepairService { get; init; }

    /// <summary>FAA NASR field: PWR_PLANT_REPAIR_SER. Power plant (engine) repair service availability.</summary>
    public RepairServiceAvailability PowerPlantRepairService { get; init; }

    /// <summary>FAA NASR field: BOTTLED_OXY_TYPE. Type of bottled oxygen available.</summary>
    public OxygenPressureType BottledOxygenType { get; init; }

    /// <summary>FAA NASR field: BULK_OXY_TYPE. Type of bulk oxygen available.</summary>
    public OxygenPressureType BulkOxygenType { get; init; }

    /// <summary>FAA NASR field: OTHER_SERVICES. Other airport services available (comma-separated codes).</summary>
    public string? OtherServices { get; init; }

    // ── Transient Storage ───────────────────────────────────────────────

    /// <summary>FAA NASR field: TRNS_STRG_BUOY_FLAG. Whether transient storage buoys are available.</summary>
    public bool TransientStorageBuoys { get; init; }

    /// <summary>FAA NASR field: TRNS_STRG_HGR_FLAG. Whether transient storage hangars are available.</summary>
    public bool TransientStorageHangars { get; init; }

    /// <summary>FAA NASR field: TRNS_STRG_TIE_FLAG. Whether transient storage tie-downs are available.</summary>
    public bool TransientStorageTiedowns { get; init; }

    // ── Lighting & Visual Aids ──────────────────────────────────────────

    /// <summary>FAA NASR field: LGT_SKED. Airport lighting schedule (e.g., SS-SR for sunset to sunrise).</summary>
    public string? LgtSked { get; init; }

    /// <summary>FAA NASR field: BCN_LGT_SKED. Beacon lighting schedule (e.g., SS-SR).</summary>
    public string? BcnLgtSked { get; init; }

    /// <summary>FAA NASR field: BCN_LENS_COLOR. Airport beacon lens color.</summary>
    public BeaconLensColor BeaconLensColor { get; init; }

    /// <summary>FAA NASR field: SEG_CIRCLE_MKR_FLAG. Segmented circle airport marker system.</summary>
    public SegmentedCircleMarkerType SegmentedCircleMarker { get; init; }

    /// <summary>FAA NASR field: WIND_INDCR_FLAG. Wind indicator type.</summary>
    public WindIndicatorType WindIndicator { get; init; }

    // ── Fees & Misc ─────────────────────────────────────────────────────

    /// <summary>FAA NASR field: LNDG_FEE_FLAG. Whether landing fees are charged.</summary>
    public bool LandingFee { get; init; }

    /// <summary>FAA NASR field: MEDICAL_USE_FLAG. Whether the airport is used for medical purposes (air ambulance).</summary>
    public bool MedicalUse { get; init; }

    /// <summary>FAA NASR field: ACTIVATION_DATE. Airport activation date (MM/YYYY format).</summary>
    public string? ActivationDate { get; init; }

    /// <summary>FAA NASR field: MIN_OP_NETWORK. Minimum Operational Network (MON) designation.</summary>
    public string? MinOpNetwork { get; init; }

    /// <summary>FAA NASR field: USER_FEE_FLAG. US Customs User Fee Airport designation.</summary>
    public string? UserFeeFlag { get; init; }

    /// <summary>FAA NASR field: CTA. Cold Temperature Airport. Altitude correction required at or below the temperature given in Celsius.</summary>
    public string? Cta { get; init; }

    // ── Attendance (from APT_ATT) ───────────────────────────────────────

    /// <summary>FAA NASR field: MONTH (APT_ATT). Months the facility is attended. May contain 'UNATNDD' for unattended facilities.</summary>
    public string? AttendanceMonth { get; init; }

    /// <summary>FAA NASR field: DAY (APT_ATT). Days of the week the facility is open.</summary>
    public string? AttendanceDay { get; init; }

    /// <summary>FAA NASR field: HOUR (APT_ATT). Hours within the day the facility is attended.</summary>
    public string? AttendanceHours { get; init; }

    // ── Contact (from APT_CON) ──────────────────────────────────────────

    /// <summary>FAA NASR field: TITLE (APT_CON). Title of the facility contact (e.g., MANAGER, OWNER).</summary>
    public string? ContactTitle { get; init; }

    /// <summary>FAA NASR field: NAME (APT_CON). Facility contact name.</summary>
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
