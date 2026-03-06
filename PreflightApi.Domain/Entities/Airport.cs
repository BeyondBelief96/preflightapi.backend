using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

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
    [Column("site_no")]
    public string SiteNo { get; set; } = string.Empty;

    /// <summary>FAA NASR field: EFF_DATE. Effective date of the airport information. ISO 8601 UTC format.</summary>
    [Column("eff_date")]
    public DateTime EffDate { get; set; }

    /// <summary>
    /// FAA NASR field: SITE_TYPE_CODE. Landing facility type code.
    /// <para>Possible values: A (Airport), H (Heliport), S (Seaplane Base), G (Gliderport), U (Ultralight).</para>
    /// </summary>
    [Column("site_type_code")]
    public string? SiteTypeCode { get; set; } = string.Empty;

    /// <summary>FAA NASR field: STATE_CODE. Two-letter USPS state code where the airport is located.</summary>
    [Column("state_code")]
    public string? StateCode { get; set; }

    /// <summary>FAA NASR field: ARPT_ID. FAA location identifier (e.g., DFW, LAX, ORD). Up to 4 characters.</summary>
    [Column("arpt_id")]
    public string? ArptId { get; set; } = string.Empty;

    /// <summary>FAA NASR field: CITY. Associated city name for the airport.</summary>
    [Column("city")]
    public string? City { get; set; } = string.Empty;

    /// <summary>FAA NASR field: COUNTRY_CODE. Two-letter country code.</summary>
    [Column("country_code")]
    public string? CountryCode { get; set; } = string.Empty;

    /// <summary>FAA NASR field: REGION_CODE. FAA region code (e.g., ASW, AEA, AWP).</summary>
    [Column("region_code")]
    public string? RegionCode { get; set; }

    /// <summary>FAA NASR field: ADO_CODE. FAA Airports District Office code.</summary>
    [Column("ado_code")]
    public string? AdoCode { get; set; }

    /// <summary>FAA NASR field: STATE_NAME. Full state name where the airport is located.</summary>
    [Column("state_name")]
    public string? StateName { get; set; }

    /// <summary>FAA NASR field: COUNTY_NAME. County name where the airport is located.</summary>
    [Column("county_name")]
    public string? CountyName { get; set; } = string.Empty;

    /// <summary>FAA NASR field: COUNTY_ASSOC_STATE. Two-letter state, territory, or country code associated with the county (e.g., US state codes, CN for Canada, GU for Guam, VI for Virgin Islands).</summary>
    [Column("county_assoc_state")]
    public string? CountyAssocState { get; set; } = string.Empty;

    /// <summary>FAA NASR field: ARPT_NAME. Official Facility Name.</summary>
    [Column("arpt_name")]
    public string? ArptName { get; set; } = string.Empty;

    /// <summary>
    /// FAA NASR field: OWNERSHIP_TYPE_CODE. Airport ownership type.
    /// <para>Possible values: PU (Publicly Owned), PR (Privately Owned), MA (Air Force Owned), MN (Navy Owned), MR (Army Owned), CG (Coast Guard Owned).</para>
    /// </summary>
    [Column("ownership_type_code")]
    public string? OwnershipTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// FAA NASR field: FACILITY_USE_CODE. Facility use designation.
    /// <para>Possible values: PU (Open to the Public), PR (Private).</para>
    /// </summary>
    [Column("facility_use_code")]
    public string? FacilityUseCode { get; set; } = string.Empty;

    /// <summary>FAA NASR field: LAT_DECIMAL. Latitude of airport reference point in decimal degrees (WGS 84).</summary>
    [Column("lat_decimal", TypeName = "decimal(10,8)")]
    public decimal? LatDecimal { get; set; }

    /// <summary>FAA NASR field: LONG_DECIMAL. Longitude of airport reference point in decimal degrees (WGS 84).</summary>
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
    [Column("lat_hemis")]
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
    [Column("long_hemis")]
    public string? LongHemis { get; set; }

    /// <summary>
    /// FAA NASR field: SURVEY_METHOD_CODE. Method used to determine the airport reference point position.
    /// <para>Possible values: E (Estimated), S (Surveyed).</para>
    /// </summary>
    [Column("survey_method_code")]
    public string? SurveyMethodCode { get; set; }

    /// <summary>FAA NASR field: ELEV. Airport elevation in feet MSL, to the nearest tenth of a foot. Measured at the highest point on the centerline of the usable landing surface.</summary>
    [Column("elev", TypeName = "decimal(6,1)")]
    public decimal? Elev { get; set; }

    /// <summary>
    /// FAA NASR field: ELEV_METHOD_CODE. Method used to determine the airport elevation.
    /// <para>Possible values: E (Estimated), S (Surveyed).</para>
    /// </summary>
    [Column("elev_method_code")]
    public string? ElevMethodCode { get; set; }

    /// <summary>FAA NASR field: MAG_VARN. Magnetic Variation in degrees. Use with <see cref="MagHemis"/> (E or W) to determine sign.</summary>
    [Column("mag_varn", TypeName = "decimal(2,0)")]
    public decimal? MagVarn { get; set; }

    /// <summary>
    /// FAA NASR field: MAG_HEMIS. Magnetic Variation Direction.
    /// <para>Possible values: E (East), W (West).</para>
    /// </summary>
    [Column("mag_hemis")]
    public string? MagHemis { get; set; }

    /// <summary>FAA NASR field: MAG_VARN_YEAR. Magnetic Variation Epoch Year.</summary>
    [Column("mag_varn_year")]
    public int? MagVarnYear { get; set; }

    /// <summary>FAA NASR field: TPA. Traffic Pattern Altitude (Whole Feet AGL).</summary>
    [Column("tpa")]
    public int? Tpa { get; set; }

    /// <summary>FAA NASR field: CHART_NAME. Sectional aeronautical chart name on which the airport appears.</summary>
    [Column("chart_name")]
    public string? ChartName { get; set; }

    /// <summary>FAA NASR field: DIST_CITY_TO_AIRPORT. Distance from Central Business District of the Associated City to the Airport, in nautical miles.</summary>
    [Column("dist_city_to_airport", TypeName = "decimal(2,0)")]
    public decimal? DistCityToAirport { get; set; }

    /// <summary>FAA NASR field: DIRECTION_CODE. Direction of Airport from Central Business District of Associated City (Nearest 1/8 Compass Point).</summary>
    [Column("direction_code")]
    public string? DirectionCode { get; set; }

    /// <summary>FAA NASR field: ACREAGE. Land Area Covered by Airport (Acres).</summary>
    [Column("acreage")]
    public int? Acreage { get; set; }

    /// <summary>FAA NASR field: COMPUTER_ID. Responsible ARTCC (FAA) Computer Identifier.</summary>
    [Column("computer_id")]
    public string? ComputerId { get; set; }

    /// <summary>FAA NASR field: RESP_ARTCC_ID. Responsible ARTCC Identifier. The Responsible ARTCC is the FAA Air Route Traffic Control Center that has control over the Airport.</summary>
    [Column("resp_artcc_id")]
    public string? RespArtccId { get; set; } = string.Empty;

    /// <summary>FAA NASR field: ARTCC_NAME. Name of the responsible ARTCC.</summary>
    [Column("artcc_name")]
    public string? ArtccName { get; set; }

    /// <summary>
    /// FAA NASR field: FSS_ON_ARPT_FLAG. Tie-In FSS Physically Located On Facility.
    /// <para>Possible values: Y (Tie-In FSS is on the airport), N (Tie-In FSS is not on airport).</para>
    /// </summary>
    [Column("fss_on_arpt_flag")]
    public string? FssOnArptFlag { get; set; }

    /// <summary>FAA NASR field: FSS_ID. Tie-In Flight Service Station (FSS) Identifier.</summary>
    [Column("fss_id")]
    public string? FssId { get; set; } = string.Empty;

    /// <summary>FAA NASR field: FSS_NAME. Tie-In FSS Name.</summary>
    [Column("fss_name")]
    public string? FssName { get; set; } = string.Empty;

    /// <summary>FAA NASR field: PHONE_NO. Local Phone Number from Airport to FSS for Administrative Services.</summary>
    [Column("fss_phone_number")]
    public string? FssPhoneNumber { get; set; }

    /// <summary>FAA NASR field: TOLL_FREE_NO. Toll Free Phone Number from Airport to FSS for Pilot Briefing Services.</summary>
    [Column("toll_free_number")]
    public string? TollFreeNumber { get; set; }

    /// <summary>FAA NASR field: ALT_FSS_ID. Alternate FSS Identifier. Identifies a full-time Flight Service Station that assumes responsibility for the Airport during the off hours of a part-time primary FSS.</summary>
    [Column("alt_fss_id")]
    public string? AltFssId { get; set; }

    /// <summary>FAA NASR field: ALT_FSS_NAME. Alternate Flight Service Station name.</summary>
    [Column("alt_fss_name")]
    public string? AltFssName { get; set; }

    /// <summary>FAA NASR field: ALT_TOLL_FREE_NO. Toll Free Phone Number from Airport to Alternate FSS for Pilot Briefing Services.</summary>
    [Column("alt_toll_free_number")]
    public string? AltTollFreeNumber { get; set; }

    /// <summary>FAA NASR field: NOTAM_ID. Identifier of the Facility responsible for issuing Notices to Airmen (NOTAMs) and Weather information for the Airport.</summary>
    [Column("notam_id")]
    public string? NotamId { get; set; }

    /// <summary>
    /// FAA NASR field: NOTAM_FLAG. Availability of NOTAM 'D' Service at Airport.
    /// <para>Possible values: Y (Yes), N (No).</para>
    /// </summary>
    [Column("notam_flag")]
    public string? NotamFlag { get; set; }

    /// <summary>FAA NASR field: ACTIVATION_DATE. Airport Activation Date (YYYY/MM). Year and month the facility was added to the NFDC airport database. Only available for facilities opened since 1981.</summary>
    [Column("activation_date")]
    public string? ActivationDate { get; set; }

    /// <summary>
    /// FAA NASR field: ARPT_STATUS. Airport operational status.
    /// <para>Possible values: O (Operational), CI (Closed Indefinitely), CP (Closed Permanently).</para>
    /// </summary>
    [Column("arpt_status")]
    public string? ArptStatus { get; set; } = string.Empty;

    /// <summary>
    /// FAA NASR field: NASP_CODE. NPIAS/Federal Agreements Code. A combination of 1 to 7 codes indicating the type
    /// of Federal agreements existing at the Airport.
    /// <para>Possible values: N (NPIAS), B (Navigational Facilities on Private Airports), G (Grant Agreements under FAAP/ADAP/AIP),
    /// H (Handicapped Accessibility Compliance), P (Surplus Property - Public Law 289), R (Surplus Property - Regulation 16-WAA),
    /// S (Conveyance under Section 16/23), V (Advance Planning Agreement under FAAP), X (Obligations Assumed by Transfer),
    /// Y (Title VI Civil Rights Act), Z (Conveyance under Section 303(C)), 1 (Expired Grant - Still Public Use),
    /// 2 (Expired 303(C) - Still Public Use), 3 (Expired AP-4 Agreement), NONE/blank (No Grant Agreement).</para>
    /// </summary>
    [Column("nasp_code")]
    public string? NaspCode { get; set; }

    /// <summary>
    /// FAA NASR field: CUST_FLAG. Facility has been designated by the U.S. Department of Homeland Security as an International Airport of Entry for Customs.
    /// <para>Possible values: Y (Yes), N (No).</para>
    /// </summary>
    [Column("customs_flag")]
    public string? CustomsFlag { get; set; }

    /// <summary>
    /// FAA NASR field: LNDG_RIGHTS_FLAG. Facility has been designated by the U.S. Department of Homeland Security as a Customs Landing Rights Airport.
    /// <para>Possible values: Y (Yes), N (No).</para>
    /// </summary>
    [Column("lndg_rights_flag")]
    public string? LndgRightsFlag { get; set; }

    /// <summary>
    /// FAA NASR field: JOINT_USE_FLAG. Facility has Military/Civil Joint Use Agreement that allows Civil Operations at a Military Airport.
    /// <para>Possible values: Y (Yes), N (No).</para>
    /// </summary>
    [Column("joint_use_flag")]
    public string? JointUseFlag { get; set; }

    /// <summary>
    /// FAA NASR field: MIL_LNDG_FLAG. Airport has entered into an Agreement that Grants Landing Rights to the Military.
    /// <para>Possible values: Y (Yes), N (No).</para>
    /// </summary>
    [Column("mil_lndg_flag")]
    public string? MilLndgFlag { get; set; }

    /// <summary>
    /// FAA NASR field: INSPECT_METHOD_CODE. Airport inspection method.
    /// <para>Possible values: F (Federal), S (State), C (Contractor), 1 (5010-1 Public Use Mailout), 2 (5010-2 Private Use Mailout).</para>
    /// </summary>
    [Column("inspect_method_code")]
    public string? InspectMethodCode { get; set; }

    /// <summary>
    /// FAA NASR field: INSPECTOR_CODE. Agency/Group Performing Physical Inspection.
    /// <para>Possible values: F (FAA Airports Field Personnel), S (State Aeronautical Personnel), C (Private Contract Personnel), N (Owner).</para>
    /// </summary>
    [Column("inspector_code")]
    public string? InspectorCode { get; set; } = string.Empty;

    /// <summary>FAA NASR field: LAST_INSPECTION. Date of the last physical inspection. ISO 8601 UTC format.</summary>
    [Column("last_inspection")]
    public DateTime? LastInspection { get; set; }

    /// <summary>FAA NASR field: LAST_INFO_RESPONSE. Date of the last information request response. ISO 8601 UTC format.</summary>
    [Column("last_info_response")]
    public DateTime? LastInfoResponse { get; set; }

    /// <summary>FAA NASR field: FUEL_TYPES. Fuel Types available for public use at the Airport (e.g., 100LL, A, A+, MOGAS, UL94).</summary>
    [Column("fuel_types")]
    public string? FuelTypes { get; set; }

    /// <summary>
    /// FAA NASR field: AIRFRAME_REPAIR_SER_CODE. Airframe repair service availability.
    /// <para>Possible values: MAJOR, MINOR, NONE.</para>
    /// </summary>
    [Column("airframe_repair_ser_code")]
    public string? AirframeRepairSerCode { get; set; }

    /// <summary>
    /// FAA NASR field: PWR_PLANT_REPAIR_SER. Power plant (engine) repair service availability.
    /// <para>Possible values: MAJOR, MINOR, NONE.</para>
    /// </summary>
    [Column("pwr_plant_repair_ser")]
    public string? PwrPlantRepairSer { get; set; }

    /// <summary>
    /// FAA NASR field: BOTTLED_OXY_TYPE. Type of bottled oxygen available.
    /// <para>Possible values: HIGH, LOW, HIGH/LOW, NONE.</para>
    /// </summary>
    [Column("bottled_oxy_type")]
    public string? BottledOxyType { get; set; }

    /// <summary>
    /// FAA NASR field: BULK_OXY_TYPE. Type of bulk oxygen available.
    /// <para>Possible values: HIGH, LOW, HIGH/LOW, NONE.</para>
    /// </summary>
    [Column("bulk_oxy_type")]
    public string? BulkOxyType { get; set; }

    /// <summary>FAA NASR field: LGT_SKED. Airport Lighting Schedule. Beginning-ending times (local time) that Standard Airport Lights are operated. Value can be "SS-SR" (sunset-sunrise), blank, or "SEE RMK".</summary>
    [Column("lgt_sked")]
    public string? LgtSked { get; set; }

    /// <summary>FAA NASR field: BCN_LGT_SKED. Beacon Lighting Schedule. Beginning-ending times (local time) that the Rotating Airport Beacon Light is operated. Value can be "SS-SR" (sunset-sunrise), blank, or "SEE RMK".</summary>
    [Column("bcn_lgt_sked")]
    public string? BcnLgtSked { get; set; }

    /// <summary>
    /// FAA NASR field: TWR_TYPE_CODE. Air Traffic Control Tower Facility Type. NON-ATCT is equivalent to no ATC tower; all others indicate ATC tower present.
    /// <para>Possible values: ATCT (Air Traffic Control Tower), NON-ATCT (No Tower), ATCT-A/C (Tower plus Approach Control),
    /// ATCT-RAPCON (Tower plus Radar Approach Control - AF operates ATCT/FAA operates AC),
    /// ATCT-RATCF (Tower plus Radar Approach Control - Navy operates ATCT/FAA operates AC),
    /// ATCT-TRACON (Tower plus Terminal Radar Approach Control), TRACON.</para>
    /// </summary>
    [Column("twr_type_code")]
    public string? TwrTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// FAA NASR field: SEG_CIRCLE_MKR_FLAG. Segmented Circle Airport Marker System on the Airport.
    /// <para>Possible values: Y (Yes), Y-L (Yes, Lighted), N (No), NONE.</para>
    /// </summary>
    [Column("seg_circle_mkr_flag")]
    public string? SegCircleMkrFlag { get; set; }

    /// <summary>
    /// FAA NASR field: BCN_LENS_COLOR. Lens Color of Operable Beacon located on the Airport.
    /// <para>Possible values: WG (White-Green, lighted land airport), WY (White-Yellow, lighted seaplane base),
    /// WGY (White-Green-Yellow, heliport), SWG (Split-White-Green, lighted military airport),
    /// W (White, unlighted land airport), Y (Yellow, unlighted seaplane base), G (Green, lighted land airport), N (None).</para>
    /// </summary>
    [Column("bcn_lens_color")]
    public string? BcnLensColor { get; set; }

    /// <summary>
    /// FAA NASR field: LNDG_FEE_FLAG. Landing Fee charged to Non-Commercial Users of Airport.
    /// <para>Possible values: Y (Yes), N (No).</para>
    /// </summary>
    [Column("lndg_fee_flag")]
    public string? LndgFeeFlag { get; set; }

    /// <summary>
    /// FAA NASR field: MEDICAL_USE_FLAG. Indicates that the Landing Facility is used for Medical Purposes.
    /// <para>Possible values: Y (Yes), N (No).</para>
    /// </summary>
    [Column("medical_use_flag")]
    public string? MedicalUseFlag { get; set; }

    /// <summary>FAA NASR field: ARPT_PSN_SOURCE. Source of the airport position information.</summary>
    [Column("arpt_psn_source")]
    public string? ArptPsnSource { get; set; }

    /// <summary>FAA NASR field: POSITION_SRC_DATE. Date the airport position information was determined. ISO 8601 UTC format.</summary>
    [Column("position_src_date")]
    public DateTime? PositionSrcDate { get; set; }

    /// <summary>FAA NASR field: ARPT_ELEV_SOURCE. Source of the airport elevation information.</summary>
    [Column("arpt_elev_source")]
    public string? ArptElevSource { get; set; }

    /// <summary>FAA NASR field: ELEVATION_SRC_DATE. Date the airport elevation information was determined. ISO 8601 UTC format.</summary>
    [Column("elevation_src_date")]
    public DateTime? ElevationSrcDate { get; set; }

    /// <summary>
    /// FAA NASR field: CONTR_FUEL_AVBL. Contract fuel availability.
    /// <para>Possible values: Y (Yes), N (No).</para>
    /// </summary>
    [Column("contr_fuel_avbl")]
    public string? ContrFuelAvbl { get; set; }

    /// <summary>
    /// FAA NASR field: TRNS_STRG_BUOY_FLAG. Transient storage availability - buoys.
    /// <para>Possible values: Y (Yes), N (No).</para>
    /// </summary>
    [Column("trns_strg_buoy_flag")]
    public string? TrnsStrgBuoyFlag { get; set; }

    /// <summary>
    /// FAA NASR field: TRNS_STRG_HGR_FLAG. Transient storage availability - hangars.
    /// <para>Possible values: Y (Yes), N (No).</para>
    /// </summary>
    [Column("trns_strg_hgr_flag")]
    public string? TrnsStrgHgrFlag { get; set; }

    /// <summary>
    /// FAA NASR field: TRNS_STRG_TIE_FLAG. Transient storage availability - tie-downs.
    /// <para>Possible values: Y (Yes), N (No).</para>
    /// </summary>
    [Column("trns_strg_tie_flag")]
    public string? TrnsStrgTieFlag { get; set; }

    /// <summary>
    /// FAA NASR field: OTHER_SERVICES. Other airport services available. A comma-separated list.
    /// <para>Possible values: AFRT (Air Freight Services), AGRI (Crop Dusting Services), AMB (Air Ambulance Services),
    /// AVNCS (Avionics), BCHGR (Beaching Gear), CARGO (Cargo Handling Services), CHTR (Charter Service),
    /// GLD (Glider Service), INSTR (Pilot Instruction), PAJA (Parachute Jump Activity), RNTL (Aircraft Rental),
    /// SALES (Aircraft Sales), SURV (Annual Surveying), TOW (Glider Towing Services).</para>
    /// </summary>
    [Column("other_services")]
    public string? OtherServices { get; set; }

    /// <summary>
    /// FAA NASR field: WIND_INDCR_FLAG. Wind Indicator at the Airport.
    /// <para>Possible values: N (No Wind Indicator), Y (Unlighted Wind Indicator Exists), Y-L (Lighted Wind Indicator Exists).</para>
    /// </summary>
    [Column("wind_indcr_flag")]
    public string? WindIndcrFlag { get; set; }

    /// <summary>FAA NASR field: ICAO_ID. ICAO (International Civil Aviation Organization) identifier (e.g., KDFW, KLAX).</summary>
    [Column("icao_id")]
    public string? IcaoId { get; set; }

    /// <summary>FAA NASR field: MIN_OP_NETWORK. Minimum Operational Network (MON) designation.</summary>
    [Column("min_op_network")]
    public string? MinOpNetwork { get; set; } = string.Empty;

    /// <summary>FAA NASR field: USER_FEE_FLAG. If set, User Fee Airports will be designated with text "US CUSTOMS USER FEE ARPT."</summary>
    [Column("user_fee_flag")]
    public string? UserFeeFlag { get; set; }

    /// <summary>FAA NASR field: CTA. Cold Temperature Airport. Altitude correction required at or below the temperature given in Celsius.</summary>
    [Column("cta")]
    public string? Cta { get; set; }

    /// <summary>FAA NASR field: FAR_139_TYPE_CODE. Airport ARFF Certification Type Code. Format is class code (I/II/III/IV) followed by A/B/C/D/E (full certificate under CFR Part 139, identifies ARFF index) or L (limited certification). Blank if not certificated.</summary>
    [Column("far_139_type_code")]
    public string? Far139TypeCode { get; set; }

    /// <summary>
    /// FAA NASR field: FAR_139_CARRIER_SER_CODE. Airport ARFF Certification Carrier Service Code.
    /// <para>Possible values: S (Scheduled Air Carrier Service), U (Not receiving scheduled service).</para>
    /// </summary>
    [Column("far_139_carrier_ser_code")]
    public string? Far139CarrierSerCode { get; set; }

    /// <summary>FAA NASR field: ARFF_CERT_TYPE_DATE. Airport ARFF Certification Date. ISO 8601 UTC format.</summary>
    [Column("arff_cert_type_date")]
    public DateTime? ArffCertTypeDate { get; set; }

    /// <summary>
    /// FAA NASR field: ASP_ANLYS_DTRM_CODE. Airport Airspace Analysis Determination.
    /// <para>Possible values: CONDL (Conditional), NOT ANALYZED, NO OBJECTION, OBJECTIONABLE.</para>
    /// </summary>
    [Column("asp_analysis_dtrm_code")]
    public string? AspAnalysisDtrmCode { get; set; }

    /// <summary>PostGIS geography point computed from LatDecimal/LongDecimal via database trigger.</summary>
    [Column("location", TypeName = "geography(Point, 4326)")]
    public Point? Location { get; set; }

    // Supplementary data from APT_ATT.csv

    /// <summary>FAA NASR field: SKED_SEQ_NO (APT_ATT). Attendance Schedule Sequence Number. Together with Site Number, uniquely identifies the attendance schedule component.</summary>
    [Column("sked_seq_no")]
    public int? SkedSeqNo { get; set; }

    /// <summary>FAA NASR field: MONTH (APT_ATT). Describes the months that the facility is attended. May contain 'UNATNDD' for unattended facilities.</summary>
    [Column("attendance_month")]
    public string? AttendanceMonth { get; set; }

    /// <summary>FAA NASR field: DAY (APT_ATT). Describes the days of the week that the facility is open.</summary>
    [Column("attendance_day")]
    public string? AttendanceDay { get; set; }

    /// <summary>FAA NASR field: HOUR (APT_ATT). Describes the hours within the day that the facility is attended.</summary>
    [Column("attendance_hours")]
    public string? AttendanceHours { get; set; }

    // Supplementary data from APT_CON.csv

    /// <summary>FAA NASR field: TITLE (APT_CON). Title of the facility contact (e.g., MANAGER, OWNER, ASST-MGR).</summary>
    [Column("contact_title")]
    public string? ContactTitle { get; set; }

    /// <summary>FAA NASR field: NAME (APT_CON). Facility contact name for the title.</summary>
    [Column("contact_name")]
    public string? ContactName { get; set; }

    /// <summary>FAA NASR field: ADDRESS1 (APT_CON). Contact address line 1.</summary>
    [Column("contact_address1")]
    public string? ContactAddress1 { get; set; }

    /// <summary>FAA NASR field: ADDRESS2 (APT_CON). Contact address line 2.</summary>
    [Column("contact_address2")]
    public string? ContactAddress2 { get; set; }

    /// <summary>FAA NASR field: TITLE_CITY (APT_CON). Contact city.</summary>
    [Column("contact_city")]
    public string? ContactCity { get; set; }

    /// <summary>FAA NASR field: STATE (APT_CON). Contact state.</summary>
    [Column("contact_state")]
    public string? ContactState { get; set; }

    /// <summary>FAA NASR field: ZIP_CODE (APT_CON). Contact ZIP code.</summary>
    [Column("contact_zip_code")]
    public string? ContactZipCode { get; set; }

    /// <summary>FAA NASR field: ZIP_PLUS_FOUR (APT_CON). Contact ZIP+4 code.</summary>
    [Column("contact_zip_plus_four")]
    public string? ContactZipPlusFour { get; set; }

    /// <summary>FAA NASR field: PHONE_NO (APT_CON). Contact phone number.</summary>
    [Column("contact_phone_number")]
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
        ComputerId = source.ComputerId;
        RespArtccId = source.RespArtccId;
        ArtccName = source.ArtccName;
        FssOnArptFlag = source.FssOnArptFlag;
        FssId = source.FssId;
        FssName = source.FssName;
        FssPhoneNumber = source.FssPhoneNumber;
        TollFreeNumber = source.TollFreeNumber;
        AltFssId = source.AltFssId;
        AltFssName = source.AltFssName;
        AltTollFreeNumber = source.AltTollFreeNumber;
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
        Far139TypeCode = source.Far139TypeCode;
        Far139CarrierSerCode = source.Far139CarrierSerCode;
        ArffCertTypeDate = source.ArffCertTypeDate;
        AspAnalysisDtrmCode = source.AspAnalysisDtrmCode;
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
