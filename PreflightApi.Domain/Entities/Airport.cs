using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PreflightApi.Domain.Entities;

/// <summary>
/// Airport facility data from the FAA National Airspace System Resources (NASR) database.
/// Combines data from APT_BASE (base airport information), APT_ATT (attendance schedule),
/// and APT_CON (facility contacts) CSV files from the FAA NASR 28-day subscription.
/// </summary>
[Table("airports")]
public class Airport : INasrEntity<Airport>
{
    /// <summary>FAA NASR field: SITE_NO. Unique Site Number assigned by the FAA to identify the airport facility.</summary>
    [Key]
    [Column("site_no", TypeName = "varchar(9)")]
    public string SiteNo { get; set; } = string.Empty;

    /// <summary>FAA NASR field: EFF_DATE. Effective date of the airport information.</summary>
    [Column("eff_date")]
    public DateTime EffDate { get; set; }

    /// <summary>
    /// FAA NASR field: SITE_TYPE_CODE. Landing facility type code.
    /// <para>Possible values: A (Airport), H (Heliport), S (Seaplane Base), G (Gliderport), U (Ultralight).</para>
    /// </summary>
    [Column("site_type_code", TypeName = "varchar(1)")]
    public string? SiteTypeCode { get; set; } = string.Empty;

    /// <summary>FAA NASR field: STATE_CODE. Two-letter USPS state code where the airport is located.</summary>
    [Column("state_code", TypeName = "varchar(2)")]
    public string? StateCode { get; set; }

    /// <summary>FAA NASR field: ARPT_ID. FAA location identifier (e.g., DFW, LAX, ORD). Up to 4 characters.</summary>
    [Column("arpt_id", TypeName = "varchar(4)")]
    public string? ArptId { get; set; } = string.Empty;

    /// <summary>FAA NASR field: CITY. Associated city name for the airport.</summary>
    [Column("city", TypeName = "varchar(40)")]
    public string? City { get; set; } = string.Empty;

    /// <summary>FAA NASR field: COUNTRY_CODE. Two-letter country code.</summary>
    [Column("country_code", TypeName = "varchar(2)")]
    public string? CountryCode { get; set; } = string.Empty;

    /// <summary>FAA NASR field: REGION_CODE. FAA region code (e.g., ASW, AEA, AWP).</summary>
    [Column("region_code", TypeName = "varchar(3)")]
    public string? RegionCode { get; set; }

    /// <summary>FAA NASR field: ADO_CODE. FAA Airports District Office code.</summary>
    [Column("ado_code", TypeName = "varchar(3)")]
    public string? AdoCode { get; set; }

    /// <summary>FAA NASR field: STATE_NAME. Full state name where the airport is located.</summary>
    [Column("state_name", TypeName = "varchar(30)")]
    public string? StateName { get; set; }

    /// <summary>FAA NASR field: COUNTY_NAME. County name where the airport is located.</summary>
    [Column("county_name", TypeName = "varchar(21)")]
    public string? CountyName { get; set; } = string.Empty;

    /// <summary>FAA NASR field: COUNTY_ASSOC_STATE. Two-letter state code associated with the county.</summary>
    [Column("county_assoc_state", TypeName = "varchar(2)")]
    public string? CountyAssocState { get; set; } = string.Empty;

    /// <summary>FAA NASR field: ARPT_NAME. Official airport facility name.</summary>
    [Column("arpt_name", TypeName = "varchar(50)")]
    public string? ArptName { get; set; } = string.Empty;

    /// <summary>
    /// FAA NASR field: OWNERSHIP_TYPE_CODE. Airport ownership type.
    /// <para>Possible values: PU (Publicly Owned), PR (Privately Owned), MA (Air Force), MN (Navy), MR (Army).</para>
    /// </summary>
    [Column("ownership_type_code", TypeName = "varchar(2)")]
    public string? OwnershipTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// FAA NASR field: FACILITY_USE_CODE. Facility use designation.
    /// <para>Possible values: PU (Public Use), PR (Private Use).</para>
    /// </summary>
    [Column("facility_use_code", TypeName = "varchar(2)")]
    public string? FacilityUseCode { get; set; } = string.Empty;

    /// <summary>FAA NASR field: LAT_DECIMAL. Latitude of airport reference point in decimal degrees.</summary>
    [Column("lat_decimal", TypeName = "decimal(10,8)")]
    public decimal? LatDecimal { get; set; }

    /// <summary>FAA NASR field: LONG_DECIMAL. Longitude of airport reference point in decimal degrees.</summary>
    [Column("long_decimal", TypeName = "decimal(11,8)")]
    public decimal? LongDecimal { get; set; }

    /// <summary>FAA NASR field: LAT_DEG. Latitude degrees of airport reference point.</summary>
    [Column("lat_deg", TypeName = "int")]
    public int? LatDeg { get; set; }

    /// <summary>FAA NASR field: LAT_MIN. Latitude minutes of airport reference point.</summary>
    [Column("lat_min", TypeName = "int")]
    public int? LatMin { get; set; }

    /// <summary>FAA NASR field: LAT_SEC. Latitude seconds of airport reference point.</summary>
    [Column("lat_sec", TypeName = "decimal(6,2)")]
    public decimal? LatSec { get; set; }

    /// <summary>
    /// FAA NASR field: LAT_HEMIS. Latitude hemisphere of airport reference point.
    /// <para>Possible values: N (North), S (South).</para>
    /// </summary>
    [Column("lat_hemis", TypeName = "varchar(1)")]
    public string? LatHemis { get; set; }

    /// <summary>FAA NASR field: LONG_DEG. Longitude degrees of airport reference point.</summary>
    [Column("long_deg", TypeName = "int")]
    public int? LongDeg { get; set; }

    /// <summary>FAA NASR field: LONG_MIN. Longitude minutes of airport reference point.</summary>
    [Column("long_min", TypeName = "int")]
    public int? LongMin { get; set; }

    /// <summary>FAA NASR field: LONG_SEC. Longitude seconds of airport reference point.</summary>
    [Column("long_sec", TypeName = "decimal(6,2)")]
    public decimal? LongSec { get; set; }

    /// <summary>
    /// FAA NASR field: LONG_HEMIS. Longitude hemisphere of airport reference point.
    /// <para>Possible values: E (East), W (West).</para>
    /// </summary>
    [Column("long_hemis", TypeName = "varchar(1)")]
    public string? LongHemis { get; set; }

    /// <summary>
    /// FAA NASR field: SURVEY_METHOD_CODE. Method used to determine the airport reference point position.
    /// <para>Possible values: E (Estimated), S (Surveyed).</para>
    /// </summary>
    [Column("survey_method_code", TypeName = "varchar(1)")]
    public string? SurveyMethodCode { get; set; }

    /// <summary>FAA NASR field: ELEV. Airport elevation in feet above Mean Sea Level (MSL), to the nearest tenth of a foot.</summary>
    [Column("elev", TypeName = "decimal(6,1)")]
    public decimal? Elev { get; set; }

    /// <summary>
    /// FAA NASR field: ELEV_METHOD_CODE. Method used to determine the airport elevation.
    /// <para>Possible values: E (Estimated), S (Surveyed).</para>
    /// </summary>
    [Column("elev_method_code", TypeName = "varchar(1)")]
    public string? ElevMethodCode { get; set; }

    /// <summary>FAA NASR field: MAG_VARN. Magnetic variation in degrees. Combine with MagHemis to determine east/west deviation.</summary>
    [Column("mag_varn", TypeName = "decimal(2,0)")]
    public decimal? MagVarn { get; set; }

    /// <summary>
    /// FAA NASR field: MAG_HEMIS. Magnetic variation hemisphere.
    /// <para>Possible values: E (East - add to true heading for magnetic), W (West - subtract from true heading for magnetic).</para>
    /// </summary>
    [Column("mag_hemis", TypeName = "varchar(1)")]
    public string? MagHemis { get; set; }

    /// <summary>FAA NASR field: MAG_VARN_YEAR. Year the magnetic variation was determined.</summary>
    [Column("mag_varn_year")]
    public int? MagVarnYear { get; set; }

    /// <summary>FAA NASR field: TPA. Traffic Pattern Altitude in feet above Mean Sea Level (MSL).</summary>
    [Column("tpa")]
    public int? Tpa { get; set; }

    /// <summary>FAA NASR field: CHART_NAME. Sectional aeronautical chart name on which the airport appears.</summary>
    [Column("chart_name", TypeName = "varchar(30)")]
    public string? ChartName { get; set; }

    /// <summary>FAA NASR field: DIST_CITY_TO_AIRPORT. Distance from the associated city to the airport, in nautical miles.</summary>
    [Column("dist_city_to_airport", TypeName = "decimal(2,0)")]
    public decimal? DistCityToAirport { get; set; }

    /// <summary>FAA NASR field: DIRECTION_CODE. Compass direction from the associated city to the airport (e.g., N, NE, SW).</summary>
    [Column("direction_code", TypeName = "varchar(3)")]
    public string? DirectionCode { get; set; }

    /// <summary>FAA NASR field: ACREAGE. Airport acreage.</summary>
    [Column("acreage")]
    public int? Acreage { get; set; }

    /// <summary>FAA NASR field: RESP_ARTCC_ID. Identifier of the responsible Air Route Traffic Control Center (ARTCC).</summary>
    [Column("resp_artcc_id", TypeName = "varchar(4)")]
    public string? RespArtccId { get; set; } = string.Empty;

    /// <summary>
    /// FAA NASR field: FSS_ON_ARPT_FLAG. Whether a Flight Service Station (FSS) is located on the airport.
    /// <para>Possible values: Y (Yes), N (No).</para>
    /// </summary>
    [Column("fss_on_arpt_flag", TypeName = "varchar(1)")]
    public string? FssOnArptFlag { get; set; }

    /// <summary>FAA NASR field: FSS_ID. Identifier of the Flight Service Station (FSS) serving the airport.</summary>
    [Column("fss_id", TypeName = "varchar(4)")]
    public string? FssId { get; set; } = string.Empty;

    /// <summary>FAA NASR field: FSS_NAME. Name of the Flight Service Station (FSS) serving the airport.</summary>
    [Column("fss_name", TypeName = "varchar(30)")]
    public string? FssName { get; set; } = string.Empty;

    /// <summary>FAA NASR field: NOTAM_ID. NOTAM facility identifier. The identifier used in the NOTAM system to identify the airport.</summary>
    [Column("notam_id", TypeName = "varchar(4)")]
    public string? NotamId { get; set; }

    /// <summary>
    /// FAA NASR field: NOTAM_FLAG. NOTAM service availability flag.
    /// <para>Possible values: Y (Yes, NOTAM service available), N (No).</para>
    /// </summary>
    [Column("notam_flag", TypeName = "varchar(1)")]
    public string? NotamFlag { get; set; }

    /// <summary>FAA NASR field: ACTIVATION_DATE. Airport activation date (MM/YYYY format).</summary>
    [Column("activation_date", TypeName = "varchar(7)")]
    public string? ActivationDate { get; set; }

    /// <summary>
    /// FAA NASR field: ARPT_STATUS. Airport operational status.
    /// <para>Possible values: O (Operational), CI (Closed Indefinitely), CP (Closed Permanently).</para>
    /// </summary>
    [Column("arpt_status", TypeName = "varchar(2)")]
    public string? ArptStatus { get; set; } = string.Empty;

    /// <summary>FAA NASR field: NASP_CODE. National Plan of Integrated Airport Systems (NPIAS) or Federal/Military Airport code.</summary>
    [Column("nasp_code", TypeName = "varchar(7)")]
    public string? NaspCode { get; set; }

    /// <summary>
    /// FAA NASR field: CUST_FLAG. Customs airport of entry flag.
    /// <para>Possible values: Y (Yes, airport of entry), N (No).</para>
    /// </summary>
    [Column("customs_flag", TypeName = "varchar(1)")]
    public string? CustomsFlag { get; set; }

    /// <summary>
    /// FAA NASR field: LNDG_RIGHTS_FLAG. Customs landing rights flag.
    /// <para>Possible values: Y (Yes, airport has landing rights), N (No).</para>
    /// </summary>
    [Column("lndg_rights_flag", TypeName = "varchar(1)")]
    public string? LndgRightsFlag { get; set; }

    /// <summary>
    /// FAA NASR field: JOINT_USE_FLAG. Joint use agreement between military and civil.
    /// <para>Possible values: Y (Yes, joint civil/military use), N (No).</para>
    /// </summary>
    [Column("joint_use_flag", TypeName = "varchar(1)")]
    public string? JointUseFlag { get; set; }

    /// <summary>
    /// FAA NASR field: MIL_LNDG_FLAG. Military landing rights agreement.
    /// <para>Possible values: Y (Yes, military landing rights), N (No).</para>
    /// </summary>
    [Column("mil_lndg_flag", TypeName = "varchar(1)")]
    public string? MilLndgFlag { get; set; }

    /// <summary>
    /// FAA NASR field: INSPECT_METHOD_CODE. Airport inspection method.
    /// <para>Possible values: F (Federal), S (State), C (Contractor), 1 (5010-1 Public Use Mailout), 2 (5010-2 Private Use Mailout).</para>
    /// </summary>
    [Column("inspect_method_code", TypeName = "varchar(1)")]
    public string? InspectMethodCode { get; set; }

    /// <summary>
    /// FAA NASR field: INSPECTOR_CODE. Agency performing the airport inspection.
    /// <para>Possible values: F (FAA), S (State), C (Contractor).</para>
    /// </summary>
    [Column("inspector_code", TypeName = "varchar(1)")]
    public string? InspectorCode { get; set; } = string.Empty;

    /// <summary>FAA NASR field: LAST_INSPECTION. Date of the last physical inspection (YYYY/MM/DD).</summary>
    [Column("last_inspection")]
    public DateTime? LastInspection { get; set; }

    /// <summary>FAA NASR field: LAST_INFO_RESPONSE. Date of the last information request response (YYYY/MM/DD).</summary>
    [Column("last_info_response")]
    public DateTime? LastInfoResponse { get; set; }

    /// <summary>FAA NASR field: FUEL_TYPES. Available fuel types at the airport. Comma-separated list (e.g., 100LL, JET-A, MOGAS).</summary>
    [Column("fuel_types", TypeName = "varchar(40)")]
    public string? FuelTypes { get; set; }

    /// <summary>
    /// FAA NASR field: AIRFRAME_REPAIR_SER_CODE. Airframe repair service availability.
    /// <para>Possible values: MAJOR, MINOR, NONE.</para>
    /// </summary>
    [Column("airframe_repair_ser_code", TypeName = "varchar(5)")]
    public string? AirframeRepairSerCode { get; set; }

    /// <summary>
    /// FAA NASR field: PWR_PLANT_REPAIR_SER. Power plant (engine) repair service availability.
    /// <para>Possible values: MAJOR, MINOR, NONE.</para>
    /// </summary>
    [Column("pwr_plant_repair_ser", TypeName = "varchar(5)")]
    public string? PwrPlantRepairSer { get; set; }

    /// <summary>
    /// FAA NASR field: BOTTLED_OXY_TYPE. Type of bottled oxygen available.
    /// <para>Possible values: HIGH, LOW, HIGH/LOW, NONE.</para>
    /// </summary>
    [Column("bottled_oxy_type", TypeName = "varchar(8)")]
    public string? BottledOxyType { get; set; }

    /// <summary>
    /// FAA NASR field: BULK_OXY_TYPE. Type of bulk oxygen available.
    /// <para>Possible values: HIGH, LOW, HIGH/LOW, NONE.</para>
    /// </summary>
    [Column("bulk_oxy_type", TypeName = "varchar(8)")]
    public string? BulkOxyType { get; set; }

    /// <summary>FAA NASR field: LGT_SKED. Airport lighting schedule (e.g., SS-SR for sunset to sunrise).</summary>
    [Column("lgt_sked", TypeName = "varchar(7)")]
    public string? LgtSked { get; set; }

    /// <summary>FAA NASR field: BCN_LGT_SKED. Beacon lighting schedule (e.g., SS-SR for sunset to sunrise).</summary>
    [Column("bcn_lgt_sked", TypeName = "varchar(7)")]
    public string? BcnLgtSked { get; set; }

    /// <summary>
    /// FAA NASR field: TWR_TYPE_CODE. Air Traffic Control Tower type.
    /// <para>Possible values include: NON-ATCT, ATCT, ATCT-A/C (with approach control), etc.</para>
    /// </summary>
    [Column("twr_type_code", TypeName = "varchar(12)")]
    public string? TwrTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// FAA NASR field: SEG_CIRCLE_MKR_FLAG. Segmented circle airport marker system.
    /// <para>Possible values: Y (Yes), Y-L (Yes, Lighted), N (No).</para>
    /// </summary>
    [Column("seg_circle_mkr_flag", TypeName = "varchar(3)")]
    public string? SegCircleMkrFlag { get; set; }

    /// <summary>
    /// FAA NASR field: BCN_LENS_COLOR. Airport beacon lens color.
    /// <para>Possible values: CG (Clear-Green, land airport), CY (Clear-Yellow, water airport), CGY (Clear-Green-Yellow, heliport), SCG (Split Clear-Green, military), C (Clear, unlighted).</para>
    /// </summary>
    [Column("bcn_lens_color", TypeName = "varchar(3)")]
    public string? BcnLensColor { get; set; }

    /// <summary>
    /// FAA NASR field: LNDG_FEE_FLAG. Whether landing fees are charged at the airport.
    /// <para>Possible values: Y (Yes), N (No).</para>
    /// </summary>
    [Column("lndg_fee_flag", TypeName = "varchar(1)")]
    public string? LndgFeeFlag { get; set; }

    /// <summary>
    /// FAA NASR field: MEDICAL_USE_FLAG. Whether the airport is used for medical purposes (air ambulance).
    /// <para>Possible values: Y (Yes), N (No).</para>
    /// </summary>
    [Column("medical_use_flag", TypeName = "varchar(1)")]
    public string? MedicalUseFlag { get; set; }

    /// <summary>FAA NASR field: ARPT_PSN_SOURCE. Source of the airport position information.</summary>
    [Column("arpt_psn_source", TypeName = "varchar(16)")]
    public string? ArptPsnSource { get; set; }

    /// <summary>FAA NASR field: POSITION_SRC_DATE. Date the airport position information was determined (YYYY/MM/DD).</summary>
    [Column("position_src_date")]
    public DateTime? PositionSrcDate { get; set; }

    /// <summary>FAA NASR field: ARPT_ELEV_SOURCE. Source of the airport elevation information.</summary>
    [Column("arpt_elev_source", TypeName = "varchar(16)")]
    public string? ArptElevSource { get; set; }

    /// <summary>FAA NASR field: ELEVATION_SRC_DATE. Date the airport elevation information was determined (YYYY/MM/DD).</summary>
    [Column("elevation_src_date")]
    public DateTime? ElevationSrcDate { get; set; }

    /// <summary>
    /// FAA NASR field: CONTR_FUEL_AVBL. Contract fuel availability.
    /// <para>Possible values: Y (Yes), N (No).</para>
    /// </summary>
    [Column("contr_fuel_avbl", TypeName = "varchar(1)")]
    public string? ContrFuelAvbl { get; set; }

    /// <summary>
    /// FAA NASR field: TRNS_STRG_BUOY_FLAG. Transient storage availability - buoys.
    /// <para>Possible values: Y (Yes), N (No).</para>
    /// </summary>
    [Column("trns_strg_buoy_flag", TypeName = "varchar(1)")]
    public string? TrnsStrgBuoyFlag { get; set; }

    /// <summary>
    /// FAA NASR field: TRNS_STRG_HGR_FLAG. Transient storage availability - hangars.
    /// <para>Possible values: Y (Yes), N (No).</para>
    /// </summary>
    [Column("trns_strg_hgr_flag", TypeName = "varchar(1)")]
    public string? TrnsStrgHgrFlag { get; set; }

    /// <summary>
    /// FAA NASR field: TRNS_STRG_TIE_FLAG. Transient storage availability - tie-downs.
    /// <para>Possible values: Y (Yes), N (No).</para>
    /// </summary>
    [Column("trns_strg_tie_flag", TypeName = "varchar(1)")]
    public string? TrnsStrgTieFlag { get; set; }

    /// <summary>
    /// FAA NASR field: OTHER_SERVICES. Other airport services available. A comma-separated list.
    /// <para>Possible values: AFRT (Air Freight Services), AGRI (Crop Dusting Services), AMB (Air Ambulance Services),
    /// AVNCS (Avionics), BCHGR (Beaching Gear), CARGO (Cargo Handling Services), CHTR (Charter Service),
    /// GLD (Glider Service), INSTR (Pilot Instruction), PAJA (Parachute Jump Activity), RNTL (Aircraft Rental),
    /// SALES (Aircraft Sales), SURV (Annual Surveying), TOW (Glider Towing Services).</para>
    /// </summary>
    [Column("other_services", TypeName = "varchar(110)")]
    public string? OtherServices { get; set; }

    /// <summary>
    /// FAA NASR field: WIND_INDCR_FLAG. Whether a wind indicator exists at the airport.
    /// <para>Possible values: N (No Wind Indicator), Y (Unlighted Wind Indicator Exists), Y-L (Lighted Wind Indicator Exists).</para>
    /// </summary>
    [Column("wind_indcr_flag", TypeName = "varchar(3)")]
    public string? WindIndcrFlag { get; set; }

    /// <summary>FAA NASR field: ICAO_ID. ICAO (International Civil Aviation Organization) identifier (e.g., KDFW, KLAX).</summary>
    [Column("icao_id", TypeName = "varchar(7)")]
    public string? IcaoId { get; set; }

    /// <summary>FAA NASR field: MIN_OP_NETWORK. Minimum Operational Network (MON) designation.</summary>
    [Column("min_op_network", TypeName = "varchar(1)")]
    public string? MinOpNetwork { get; set; } = string.Empty;

    /// <summary>FAA NASR field: USER_FEE_FLAG. If set, the airport is designated as "US CUSTOMS USER FEE ARPT."</summary>
    [Column("user_fee_flag", TypeName = "varchar(26)")]
    public string? UserFeeFlag { get; set; }

    /// <summary>FAA NASR field: CTA. Cold Temperature Airport. Altitude correction required at or below the temperature given in Celsius.</summary>
    [Column("cta", TypeName = "varchar(4)")]
    public string? Cta { get; set; }

    // Supplementary data from APT_ATT.csv

    /// <summary>FAA NASR field: SKED_SEQ_NO (APT_ATT). Attendance Schedule Sequence Number. Together with Site Number, uniquely identifies the attendance schedule component.</summary>
    [Column("sked_seq_no")]
    public int? SkedSeqNo { get; set; }

    /// <summary>FAA NASR field: MONTH (APT_ATT). Describes the months that the facility is attended. May contain 'UNATNDD' for unattended facilities.</summary>
    [Column("attendance_month", TypeName = "varchar(50)")]
    public string? AttendanceMonth { get; set; }

    /// <summary>FAA NASR field: DAY (APT_ATT). Describes the days of the week that the facility is open.</summary>
    [Column("attendance_day", TypeName = "varchar(16)")]
    public string? AttendanceDay { get; set; }

    /// <summary>FAA NASR field: HOUR (APT_ATT). Describes the hours within the day that the facility is attended.</summary>
    [Column("attendance_hours", TypeName = "varchar(40)")]
    public string? AttendanceHours { get; set; }

    // Supplementary data from APT_CON.csv

    /// <summary>FAA NASR field: TITLE (APT_CON). Title of the facility contact (e.g., MANAGER, OWNER, ASST-MGR).</summary>
    [Column("contact_title", TypeName = "varchar(10)")]
    public string? ContactTitle { get; set; }

    /// <summary>FAA NASR field: NAME (APT_CON). Facility contact name for the title.</summary>
    [Column("contact_name", TypeName = "varchar(35)")]
    public string? ContactName { get; set; }

    /// <summary>FAA NASR field: ADDRESS1 (APT_CON). Contact address line 1.</summary>
    [Column("contact_address1", TypeName = "varchar(35)")]
    public string? ContactAddress1 { get; set; }

    /// <summary>FAA NASR field: ADDRESS2 (APT_CON). Contact address line 2.</summary>
    [Column("contact_address2", TypeName = "varchar(35)")]
    public string? ContactAddress2 { get; set; }

    /// <summary>FAA NASR field: TITLE_CITY (APT_CON). Contact city.</summary>
    [Column("contact_city", TypeName = "varchar(30)")]
    public string? ContactCity { get; set; }

    /// <summary>FAA NASR field: STATE (APT_CON). Contact state.</summary>
    [Column("contact_state", TypeName = "varchar(2)")]
    public string? ContactState { get; set; }

    /// <summary>FAA NASR field: ZIP_CODE (APT_CON). Contact ZIP code.</summary>
    [Column("contact_zip_code", TypeName = "varchar(5)")]
    public string? ContactZipCode { get; set; }

    /// <summary>FAA NASR field: ZIP_PLUS_FOUR (APT_CON). Contact ZIP+4 code.</summary>
    [Column("contact_zip_plus_four", TypeName = "varchar(4)")]
    public string? ContactZipPlusFour { get; set; }

    /// <summary>FAA NASR field: PHONE_NO (APT_CON). Contact phone number.</summary>
    [Column("contact_phone_number", TypeName = "varchar(16)")]
    public string? ContactPhoneNumber { get; set; }

    // INasrEntity<Airport> implementation
    public string CreateUniqueKey()
    {
        return SiteNo ?? string.Empty;
    }

    public void UpdateFrom(Airport source, HashSet<string>? limitToProperties = null)
    {
        if (limitToProperties == null)
        {
            // Update all properties (for base data)
            UpdateAllProperties(source);
        }
        else
        {
            // Update only specified properties (for supplementary data)
            UpdateSelectiveProperties(source, limitToProperties);
        }
    }

    public Airport CreateSelectiveEntity(HashSet<string> properties)
    {
        var selective = new Airport();

        // Always include the key
        if (properties.Contains(nameof(SiteNo)))
            selective.SiteNo = SiteNo;

        // Base data properties
        if (properties.Contains(nameof(EffDate)))
            selective.EffDate = EffDate;
        if (properties.Contains(nameof(SiteTypeCode)))
            selective.SiteTypeCode = SiteTypeCode;
        if (properties.Contains(nameof(StateCode)))
            selective.StateCode = StateCode;
        if (properties.Contains(nameof(ArptId)))
            selective.ArptId = ArptId;

        // Supplementary data properties (APT_ATT)
        if (properties.Contains(nameof(SkedSeqNo)))
            selective.SkedSeqNo = SkedSeqNo;
        if (properties.Contains(nameof(AttendanceMonth)))
            selective.AttendanceMonth = AttendanceMonth;
        if (properties.Contains(nameof(AttendanceDay)))
            selective.AttendanceDay = AttendanceDay;
        if (properties.Contains(nameof(AttendanceHours)))
            selective.AttendanceHours = AttendanceHours;

        // Supplementary data properties (APT_CON)
        if (properties.Contains(nameof(ContactTitle)))
            selective.ContactTitle = ContactTitle;
        if (properties.Contains(nameof(ContactName)))
            selective.ContactName = ContactName;
        if (properties.Contains(nameof(ContactAddress1)))
            selective.ContactAddress1 = ContactAddress1;
        if (properties.Contains(nameof(ContactAddress2)))
            selective.ContactAddress2 = ContactAddress2;
        if (properties.Contains(nameof(ContactCity)))
            selective.ContactCity = ContactCity;
        if (properties.Contains(nameof(ContactState)))
            selective.ContactState = ContactState;
        if (properties.Contains(nameof(ContactZipCode)))
            selective.ContactZipCode = ContactZipCode;
        if (properties.Contains(nameof(ContactZipPlusFour)))
            selective.ContactZipPlusFour = ContactZipPlusFour;
        if (properties.Contains(nameof(ContactPhoneNumber)))
            selective.ContactPhoneNumber = ContactPhoneNumber;

        return selective;
    }

    private void UpdateAllProperties(Airport source)
    {
        EffDate = source.EffDate;
        SiteTypeCode = source.SiteTypeCode;
        StateCode = source.StateCode;
        ArptId = source.ArptId;
        City = source.City;
        CountryCode = source.CountryCode;
        RegionCode = source.RegionCode;
        AdoCode = source.AdoCode;
        StateName = source.StateName;
        CountyName = source.CountyName;
        CountyAssocState = source.CountyAssocState;
        ArptName = source.ArptName;
        OwnershipTypeCode = source.OwnershipTypeCode;
        FacilityUseCode = source.FacilityUseCode;
        LatDecimal = source.LatDecimal;
        LongDecimal = source.LongDecimal;
        LatDeg = source.LatDeg;
        LatMin = source.LatMin;
        LatSec = source.LatSec;
        LatHemis = source.LatHemis;
        LongDeg = source.LongDeg;
        LongMin = source.LongMin;
        LongSec = source.LongSec;
        LongHemis = source.LongHemis;
        SurveyMethodCode = source.SurveyMethodCode;
        Elev = source.Elev;
        ElevMethodCode = source.ElevMethodCode;
        MagVarn = source.MagVarn;
        MagHemis = source.MagHemis;
        MagVarnYear = source.MagVarnYear;
        Tpa = source.Tpa;
        ChartName = source.ChartName;
        DistCityToAirport = source.DistCityToAirport;
        DirectionCode = source.DirectionCode;
        Acreage = source.Acreage;
        RespArtccId = source.RespArtccId;
        FssOnArptFlag = source.FssOnArptFlag;
        FssId = source.FssId;
        FssName = source.FssName;
        NotamId = source.NotamId;
        NotamFlag = source.NotamFlag;
        ActivationDate = source.ActivationDate;
        ArptStatus = source.ArptStatus;
        NaspCode = source.NaspCode;
        CustomsFlag = source.CustomsFlag;
        LndgRightsFlag = source.LndgRightsFlag;
        JointUseFlag = source.JointUseFlag;
        MilLndgFlag = source.MilLndgFlag;
        InspectMethodCode = source.InspectMethodCode;
        InspectorCode = source.InspectorCode;
        LastInspection = source.LastInspection;
        LastInfoResponse = source.LastInfoResponse;
        FuelTypes = source.FuelTypes;
        AirframeRepairSerCode = source.AirframeRepairSerCode;
        PwrPlantRepairSer = source.PwrPlantRepairSer;
        BottledOxyType = source.BottledOxyType;
        BulkOxyType = source.BulkOxyType;
        LgtSked = source.LgtSked;
        BcnLgtSked = source.BcnLgtSked;
        TwrTypeCode = source.TwrTypeCode;
        SegCircleMkrFlag = source.SegCircleMkrFlag;
        BcnLensColor = source.BcnLensColor;
        LndgFeeFlag = source.LndgFeeFlag;
        MedicalUseFlag = source.MedicalUseFlag;
        ArptPsnSource = source.ArptPsnSource;
        PositionSrcDate = source.PositionSrcDate;
        ArptElevSource = source.ArptElevSource;
        ElevationSrcDate = source.ElevationSrcDate;
        ContrFuelAvbl = source.ContrFuelAvbl;
        TrnsStrgBuoyFlag = source.TrnsStrgBuoyFlag;
        TrnsStrgHgrFlag = source.TrnsStrgHgrFlag;
        TrnsStrgTieFlag = source.TrnsStrgTieFlag;
        OtherServices = source.OtherServices;
        WindIndcrFlag = source.WindIndcrFlag;
        IcaoId = source.IcaoId;
        MinOpNetwork = source.MinOpNetwork;
        UserFeeFlag = source.UserFeeFlag;
        Cta = source.Cta;
    }

    private void UpdateSelectiveProperties(Airport source, HashSet<string> limitToProperties)
    {
        // Only update non-null values for properties in the limit set
        if (limitToProperties.Contains(nameof(SkedSeqNo)) && source.SkedSeqNo != null)
            SkedSeqNo = source.SkedSeqNo;
        if (limitToProperties.Contains(nameof(AttendanceMonth)) && source.AttendanceMonth != null)
            AttendanceMonth = source.AttendanceMonth;
        if (limitToProperties.Contains(nameof(AttendanceDay)) && source.AttendanceDay != null)
            AttendanceDay = source.AttendanceDay;
        if (limitToProperties.Contains(nameof(AttendanceHours)) && source.AttendanceHours != null)
            AttendanceHours = source.AttendanceHours;

        if (limitToProperties.Contains(nameof(ContactTitle)) && source.ContactTitle != null)
            ContactTitle = source.ContactTitle;
        if (limitToProperties.Contains(nameof(ContactName)) && source.ContactName != null)
            ContactName = source.ContactName;
        if (limitToProperties.Contains(nameof(ContactAddress1)) && source.ContactAddress1 != null)
            ContactAddress1 = source.ContactAddress1;
        if (limitToProperties.Contains(nameof(ContactAddress2)) && source.ContactAddress2 != null)
            ContactAddress2 = source.ContactAddress2;
        if (limitToProperties.Contains(nameof(ContactCity)) && source.ContactCity != null)
            ContactCity = source.ContactCity;
        if (limitToProperties.Contains(nameof(ContactState)) && source.ContactState != null)
            ContactState = source.ContactState;
        if (limitToProperties.Contains(nameof(ContactZipCode)) && source.ContactZipCode != null)
            ContactZipCode = source.ContactZipCode;
        if (limitToProperties.Contains(nameof(ContactZipPlusFour)) && source.ContactZipPlusFour != null)
            ContactZipPlusFour = source.ContactZipPlusFour;
        if (limitToProperties.Contains(nameof(ContactPhoneNumber)) && source.ContactPhoneNumber != null)
            ContactPhoneNumber = source.ContactPhoneNumber;
    }
}
