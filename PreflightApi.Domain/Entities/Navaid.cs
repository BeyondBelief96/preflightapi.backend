using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace PreflightApi.Domain.Entities
{
    /// <summary>
    /// Navigation Aid (NAVAID) data from the FAA NASR database. Sourced from NAV_BASE, NAV_CKPT, and NAV_RMK CSV files
    /// in the FAA NASR 28-day subscription. Contains VOR, VORTAC, NDB, DME, TACAN, and other navigation aid data.
    /// </summary>
    [Table("navaids")]
    public class Navaid : INasrEntity<Navaid>
    {
        /// <summary>System-generated unique identifier.</summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        // === Common columns (present in all NAV files) ===

        /// <summary>FAA NASR field: EFF_DATE. The 28 Day NASR Subscription Effective Date. ISO 8601 UTC format.</summary>
        [Column("effective_date")]
        public DateTime EffectiveDate { get; set; }

        /// <summary>FAA NASR field: NAV_ID. NAVAID Facility Identifier.</summary>
        [Column("nav_id", TypeName = "varchar(4)")]
        [Required]
        public string NavId { get; set; } = string.Empty;

        /// <summary>FAA NASR field: NAV_TYPE. NAVAID Facility Type.</summary>
        [Column("nav_type", TypeName = "varchar(25)")]
        [Required]
        public string NavType { get; set; } = string.Empty;

        /// <summary>FAA NASR field: STATE_CODE. Associated State Post Office Code.</summary>
        [Column("state_code", TypeName = "varchar(2)")]
        public string? StateCode { get; set; }

        /// <summary>FAA NASR field: CITY. Associated City Name.</summary>
        [Column("city", TypeName = "varchar(40)")]
        [Required]
        public string City { get; set; } = string.Empty;

        /// <summary>FAA NASR field: COUNTRY_CODE. Country Post Office Code NAVAID Located.</summary>
        [Column("country_code", TypeName = "varchar(2)")]
        [Required]
        public string CountryCode { get; set; } = string.Empty;

        // === NAV_BASE specific columns ===

        /// <summary>FAA NASR field: NAV_STATUS. Navigation Aid Status.</summary>
        [Column("nav_status", TypeName = "varchar(30)")]
        [Required]
        public string NavStatus { get; set; } = string.Empty;

        /// <summary>FAA NASR field: NAME. Name of NAVAID.</summary>
        [Column("name", TypeName = "varchar(30)")]
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>FAA NASR field: STATE_NAME. Associated State Name.</summary>
        [Column("state_name", TypeName = "varchar(30)")]
        public string? StateName { get; set; }

        /// <summary>FAA NASR field: REGION_CODE. FAA Region responsible for NAVAID (code).</summary>
        [Column("region_code", TypeName = "varchar(3)")]
        public string? RegionCode { get; set; }

        /// <summary>FAA NASR field: COUNTRY_NAME. Country Name NAVAID Located.</summary>
        [Column("country_name", TypeName = "varchar(30)")]
        [Required]
        public string CountryName { get; set; } = string.Empty;

        /// <summary>FAA NASR field: FAN_MARKER. Name of FAN MARKER.</summary>
        [Column("fan_marker", TypeName = "varchar(30)")]
        public string? FanMarker { get; set; }

        /// <summary>FAA NASR field: OWNER. A Concatenation of the NAVAID OWNER CODE - NAVAID OWNER NAME.</summary>
        [Column("owner", TypeName = "varchar(50)")]
        public string? Owner { get; set; }

        /// <summary>FAA NASR field: OPERATOR. A Concatenation of the NAVAID OPERATOR CODE - NAVAID OPERATOR NAME.</summary>
        [Column("operator", TypeName = "varchar(50)")]
        public string? Operator { get; set; }

        /// <summary>FAA NASR field: NAS_USE_FLAG. Common System Usage (Y or N). Defines how the NAVAID is used.</summary>
        [Column("nas_use_flag", TypeName = "varchar(1)")]
        [Required]
        public string NasUseFlag { get; set; } = string.Empty;

        /// <summary>FAA NASR field: PUBLIC_USE_FLAG. NAVAID PUBLIC USE (Y or N). Defines by whom the NAVAID is used.</summary>
        [Column("public_use_flag", TypeName = "varchar(1)")]
        [Required]
        public string PublicUseFlag { get; set; } = string.Empty;

        /// <summary>FAA NASR field: NDB_CLASS_CODE. Class of NDB.</summary>
        [Column("ndb_class_code", TypeName = "varchar(11)")]
        public string? NdbClassCode { get; set; }

        /// <summary>FAA NASR field: OPER_HOURS. Hours of Operation of NAVAID.</summary>
        [Column("oper_hours", TypeName = "varchar(11)")]
        public string? OperHours { get; set; }

        /// <summary>FAA NASR field: HIGH_ALT_ARTCC_ID. Identifier of ARTCC with High Altitude Boundary That the NAVAID Falls Within.</summary>
        [Column("high_alt_artcc_id", TypeName = "varchar(4)")]
        public string? HighAltArtccId { get; set; }

        /// <summary>FAA NASR field: HIGH_ARTCC_NAME. Name of ARTCC with High Altitude Boundary That the NAVAID Falls Within.</summary>
        [Column("high_artcc_name", TypeName = "varchar(30)")]
        public string? HighArtccName { get; set; }

        /// <summary>FAA NASR field: LOW_ALT_ARTCC_ID. Identifier of ARTCC with Low Altitude Boundary That the NAVAID Falls Within.</summary>
        [Column("low_alt_artcc_id", TypeName = "varchar(4)")]
        public string? LowAltArtccId { get; set; }

        /// <summary>FAA NASR field: LOW_ARTCC_NAME. Name of ARTCC with Low Altitude Boundary That the NAVAID Falls Within.</summary>
        [Column("low_artcc_name", TypeName = "varchar(30)")]
        public string? LowArtccName { get; set; }

        // === Coordinates ===

        /// <summary>FAA NASR field: LAT_DEG. NAVAID Latitude Degrees.</summary>
        [Column("lat_deg")]
        public int? LatDeg { get; set; }

        /// <summary>FAA NASR field: LAT_MIN. NAVAID Latitude Minutes.</summary>
        [Column("lat_min")]
        public int? LatMin { get; set; }

        /// <summary>FAA NASR field: LAT_SEC. NAVAID Latitude Seconds.</summary>
        [Column("lat_sec", TypeName = "decimal(6,4)")]
        public decimal? LatSec { get; set; }

        /// <summary>FAA NASR field: LAT_HEMIS. NAVAID Latitude Hemisphere.</summary>
        [Column("lat_hemis", TypeName = "varchar(1)")]
        public string? LatHemis { get; set; }

        /// <summary>FAA NASR field: LAT_DECIMAL. NAVAID Latitude in decimal degrees (WGS 84).</summary>
        [Column("lat_decimal", TypeName = "decimal(10,8)")]
        public decimal? LatDecimal { get; set; }

        /// <summary>FAA NASR field: LONG_DEG. NAVAID Longitude Degrees.</summary>
        [Column("long_deg")]
        public int? LongDeg { get; set; }

        /// <summary>FAA NASR field: LONG_MIN. NAVAID Longitude Minutes.</summary>
        [Column("long_min")]
        public int? LongMin { get; set; }

        /// <summary>FAA NASR field: LONG_SEC. NAVAID Longitude Seconds.</summary>
        [Column("long_sec", TypeName = "decimal(6,4)")]
        public decimal? LongSec { get; set; }

        /// <summary>FAA NASR field: LONG_HEMIS. NAVAID Longitude Hemisphere.</summary>
        [Column("long_hemis", TypeName = "varchar(1)")]
        public string? LongHemis { get; set; }

        /// <summary>FAA NASR field: LONG_DECIMAL. NAVAID Longitude in decimal degrees (WGS 84).</summary>
        [Column("long_decimal", TypeName = "decimal(11,8)")]
        public decimal? LongDecimal { get; set; }

        /// <summary>FAA NASR field: SURVEY_ACCURACY_CODE. Latitude/Longitude Survey Accuracy (Code).</summary>
        [Column("survey_accuracy_code", TypeName = "varchar(1)")]
        public string? SurveyAccuracyCode { get; set; }

        // === TACAN/DME Location ===

        /// <summary>FAA NASR field: TACAN_DME_STATUS. Status of TACAN or DME Equipment.</summary>
        [Column("tacan_dme_status", TypeName = "varchar(30)")]
        public string? TacanDmeStatus { get; set; }

        /// <summary>FAA NASR field: TACAN_DME_LAT_DEG. Latitude Degrees of TACAN Portion of VORTAC when TACAN is not sited with VOR.</summary>
        [Column("tacan_dme_lat_deg")]
        public int? TacanDmeLatDeg { get; set; }

        /// <summary>FAA NASR field: TACAN_DME_LAT_MIN. Latitude Minutes of TACAN Portion of VORTAC when TACAN is not sited with VOR.</summary>
        [Column("tacan_dme_lat_min")]
        public int? TacanDmeLatMin { get; set; }

        /// <summary>FAA NASR field: TACAN_DME_LAT_SEC. Latitude Seconds of TACAN Portion of VORTAC when TACAN is not sited with VOR.</summary>
        [Column("tacan_dme_lat_sec", TypeName = "decimal(6,4)")]
        public decimal? TacanDmeLatSec { get; set; }

        /// <summary>FAA NASR field: TACAN_DME_LAT_HEMIS. Latitude Hemisphere of TACAN Portion of VORTAC when TACAN is not sited with VOR.</summary>
        [Column("tacan_dme_lat_hemis", TypeName = "varchar(1)")]
        public string? TacanDmeLatHemis { get; set; }

        /// <summary>FAA NASR field: TACAN_DME_LAT_DECIMAL. Latitude in decimal degrees (WGS 84) of TACAN Portion of VORTAC when TACAN is not sited with VOR.</summary>
        [Column("tacan_dme_lat_decimal", TypeName = "decimal(10,8)")]
        public decimal? TacanDmeLatDecimal { get; set; }

        /// <summary>FAA NASR field: TACAN_DME_LONG_DEG. Longitude Degrees of TACAN Portion of VORTAC when TACAN is not sited with VOR.</summary>
        [Column("tacan_dme_long_deg")]
        public int? TacanDmeLongDeg { get; set; }

        /// <summary>FAA NASR field: TACAN_DME_LONG_MIN. Longitude Minutes of TACAN Portion of VORTAC when TACAN is not sited with VOR.</summary>
        [Column("tacan_dme_long_min")]
        public int? TacanDmeLongMin { get; set; }

        /// <summary>FAA NASR field: TACAN_DME_LONG_SEC. Longitude Seconds of TACAN Portion of VORTAC when TACAN is not sited with VOR.</summary>
        [Column("tacan_dme_long_sec", TypeName = "decimal(6,4)")]
        public decimal? TacanDmeLongSec { get; set; }

        /// <summary>FAA NASR field: TACAN_DME_LONG_HEMIS. Longitude Hemisphere of TACAN Portion of VORTAC when TACAN is not sited with VOR.</summary>
        [Column("tacan_dme_long_hemis", TypeName = "varchar(1)")]
        public string? TacanDmeLongHemis { get; set; }

        /// <summary>FAA NASR field: TACAN_DME_LONG_DECIMAL. Longitude in decimal degrees (WGS 84) of TACAN Portion of VORTAC when TACAN is not sited with VOR.</summary>
        [Column("tacan_dme_long_decimal", TypeName = "decimal(11,8)")]
        public decimal? TacanDmeLongDecimal { get; set; }

        // === Other data ===

        /// <summary>FAA NASR field: ELEV. Elevation in tenths of a foot MSL.</summary>
        [Column("elev", TypeName = "decimal(6,1)")]
        public decimal? Elev { get; set; }

        /// <summary>FAA NASR field: MAG_VARN. Magnetic Variation in degrees. Direction (E/W) is in MagVarnHemis.</summary>
        [Column("mag_varn")]
        public int? MagVarn { get; set; }

        /// <summary>FAA NASR field: MAG_VARN_HEMIS. Magnetic Variation Direction.</summary>
        [Column("mag_varn_hemis", TypeName = "varchar(1)")]
        public string? MagVarnHemis { get; set; }

        /// <summary>FAA NASR field: MAG_VARN_YEAR. Magnetic Variation Epoch Year.</summary>
        [Column("mag_varn_year")]
        public int? MagVarnYear { get; set; }

        /// <summary>FAA NASR field: SIMUL_VOICE_FLAG. Simultaneous Voice Feature.</summary>
        [Column("simul_voice_flag", TypeName = "varchar(1)")]
        public string? SimulVoiceFlag { get; set; }

        /// <summary>FAA NASR field: PWR_OUTPUT. Power Output (In Watts).</summary>
        [Column("pwr_output")]
        public int? PwrOutput { get; set; }

        /// <summary>FAA NASR field: AUTO_VOICE_ID_FLAG. Automatic Voice Identification Feature.</summary>
        [Column("auto_voice_id_flag", TypeName = "varchar(1)")]
        public string? AutoVoiceIdFlag { get; set; }

        /// <summary>FAA NASR field: MNT_CAT_CODE. Monitoring Category.</summary>
        [Column("mnt_cat_code", TypeName = "varchar(1)")]
        public string? MntCatCode { get; set; }

        /// <summary>FAA NASR field: VOICE_CALL. Radio Voice Call (Name) or Trans Signal.</summary>
        [Column("voice_call", TypeName = "varchar(60)")]
        public string? VoiceCall { get; set; }

        /// <summary>FAA NASR field: CHAN. Channel (TACAN) NAVAID Transmits On.</summary>
        [Column("chan", TypeName = "varchar(4)")]
        public string? Chan { get; set; }

        /// <summary>FAA NASR field: FREQ. Frequency the NAVAID Transmits On (Except TACAN). MHz for VOR/ILS, kHz for NDB.</summary>
        [Column("freq", TypeName = "decimal(5,2)")]
        public decimal? Freq { get; set; }

        /// <summary>FAA NASR field: MKR_IDENT. Transmitted Fan Marker/Marine Radio Beacon Identifier.</summary>
        [Column("mkr_ident", TypeName = "varchar(30)")]
        public string? MkrIdent { get; set; }

        /// <summary>FAA NASR field: MKR_SHAPE. Fan Marker Type (E - ELLIPTICAL).</summary>
        [Column("mkr_shape", TypeName = "varchar(1)")]
        public string? MkrShape { get; set; }

        /// <summary>FAA NASR field: MKR_BRG. Marker Bearing in degrees true.</summary>
        [Column("mkr_brg")]
        public int? MkrBrg { get; set; }

        /// <summary>FAA NASR field: ALT_CODE. VOR Standard Service Volume.</summary>
        [Column("alt_code", TypeName = "varchar(2)")]
        public string? AltCode { get; set; }

        /// <summary>FAA NASR field: DME_SSV. DME Standard Service Volume.</summary>
        [Column("dme_ssv", TypeName = "varchar(2)")]
        public string? DmeSsv { get; set; }

        /// <summary>FAA NASR field: LOW_NAV_ON_HIGH_CHART_FLAG. Low Altitude Facility Used in High Structure.</summary>
        [Column("low_nav_on_high_chart_flag", TypeName = "varchar(1)")]
        public string? LowNavOnHighChartFlag { get; set; }

        /// <summary>FAA NASR field: Z_MKR_FLAG. NAVAID Z Marker Available.</summary>
        [Column("z_mkr_flag", TypeName = "varchar(1)")]
        public string? ZMkrFlag { get; set; }

        /// <summary>FAA NASR field: FSS_ID. Associated/Controlling FSS (IDENT).</summary>
        [Column("fss_id", TypeName = "varchar(4)")]
        public string? FssId { get; set; }

        /// <summary>FAA NASR field: FSS_NAME. Associated/Controlling FSS (Name).</summary>
        [Column("fss_name", TypeName = "varchar(30)")]
        public string? FssName { get; set; }

        /// <summary>FAA NASR field: FSS_HOURS. Hours of Operation of Controlling FSS.</summary>
        [Column("fss_hours", TypeName = "varchar(65)")]
        public string? FssHours { get; set; }

        /// <summary>FAA NASR field: NOTAM_ID. NOTAM Accountability Code (IDENT).</summary>
        [Column("notam_id", TypeName = "varchar(4)")]
        public string? NotamId { get; set; }

        /// <summary>FAA NASR field: QUAD_IDENT. Quadrant Identification and Range Leg Bearing (LFR Only).</summary>
        [Column("quad_ident", TypeName = "varchar(20)")]
        public string? QuadIdent { get; set; }

        /// <summary>FAA NASR field: PITCH_FLAG. Pitch Flag.</summary>
        [Column("pitch_flag", TypeName = "varchar(1)")]
        public string? PitchFlag { get; set; }

        /// <summary>FAA NASR field: CATCH_FLAG. Catch Flag.</summary>
        [Column("catch_flag", TypeName = "varchar(1)")]
        public string? CatchFlag { get; set; }

        /// <summary>FAA NASR field: SUA_ATCAA_FLAG. SUA/ATCAA Flag.</summary>
        [Column("sua_atcaa_flag", TypeName = "varchar(1)")]
        public string? SuaAtcaaFlag { get; set; }

        /// <summary>FAA NASR field: RESTRICTION_FLAG. NAVAID Restriction Flag.</summary>
        [Column("restriction_flag", TypeName = "varchar(1)")]
        public string? RestrictionFlag { get; set; }

        /// <summary>FAA NASR field: HIWAS_FLAG. HIWAS Flag.</summary>
        [Column("hiwas_flag", TypeName = "varchar(1)")]
        public string? HiwasFlag { get; set; }

        // === PostGIS spatial columns ===

        /// <summary>PostGIS geography point derived from LAT_DECIMAL/LONG_DECIMAL.</summary>
        [Column("location", TypeName = "geography(Point, 4326)")]
        public Point? Location { get; set; }

        /// <summary>PostGIS geography point derived from TACAN_DME_LAT_DECIMAL/TACAN_DME_LONG_DECIMAL.</summary>
        [Column("tacan_dme_location", TypeName = "geography(Point, 4326)")]
        public Point? TacanDmeLocation { get; set; }

        // === JSONB columns for checkpoint and remark data ===

        /// <summary>JSON-serialized list of NavaidCheckpoint objects from NAV_CKPT file.</summary>
        [Column("checkpoints_json", TypeName = "jsonb")]
        public string? CheckpointsJson { get; set; }

        /// <summary>JSON-serialized list of NavaidRemark objects from NAV_RMK file.</summary>
        [Column("remarks_json", TypeName = "jsonb")]
        public string? RemarksJson { get; set; }

        // === INasrEntity<Navaid> implementation ===

        public string CreateUniqueKey()
        {
            return string.Join("|", NavId, NavType, CountryCode, City);
        }

        public void UpdateFrom(Navaid source, HashSet<string>? limitToProperties = null)
        {
            if (limitToProperties == null || !limitToProperties.Any())
            {
                UpdateAllProperties(source);
            }
            else
            {
                UpdateSelectiveProperties(source, limitToProperties);
            }
        }

        public Navaid CreateSelectiveEntity(HashSet<string> properties)
        {
            var selective = new Navaid();

            // Key properties
            if (properties.Contains(nameof(NavId)))
                selective.NavId = NavId;
            if (properties.Contains(nameof(NavType)))
                selective.NavType = NavType;
            if (properties.Contains(nameof(CountryCode)))
                selective.CountryCode = CountryCode;
            if (properties.Contains(nameof(City)))
                selective.City = City;

            // Common columns
            if (properties.Contains(nameof(EffectiveDate)))
                selective.EffectiveDate = EffectiveDate;
            if (properties.Contains(nameof(StateCode)))
                selective.StateCode = StateCode;

            // NAV_BASE specific
            if (properties.Contains(nameof(NavStatus)))
                selective.NavStatus = NavStatus;
            if (properties.Contains(nameof(Name)))
                selective.Name = Name;
            if (properties.Contains(nameof(StateName)))
                selective.StateName = StateName;
            if (properties.Contains(nameof(RegionCode)))
                selective.RegionCode = RegionCode;
            if (properties.Contains(nameof(CountryName)))
                selective.CountryName = CountryName;
            if (properties.Contains(nameof(FanMarker)))
                selective.FanMarker = FanMarker;
            if (properties.Contains(nameof(Owner)))
                selective.Owner = Owner;
            if (properties.Contains(nameof(Operator)))
                selective.Operator = Operator;
            if (properties.Contains(nameof(NasUseFlag)))
                selective.NasUseFlag = NasUseFlag;
            if (properties.Contains(nameof(PublicUseFlag)))
                selective.PublicUseFlag = PublicUseFlag;
            if (properties.Contains(nameof(NdbClassCode)))
                selective.NdbClassCode = NdbClassCode;
            if (properties.Contains(nameof(OperHours)))
                selective.OperHours = OperHours;
            if (properties.Contains(nameof(HighAltArtccId)))
                selective.HighAltArtccId = HighAltArtccId;
            if (properties.Contains(nameof(HighArtccName)))
                selective.HighArtccName = HighArtccName;
            if (properties.Contains(nameof(LowAltArtccId)))
                selective.LowAltArtccId = LowAltArtccId;
            if (properties.Contains(nameof(LowArtccName)))
                selective.LowArtccName = LowArtccName;
            if (properties.Contains(nameof(LatDeg)))
                selective.LatDeg = LatDeg;
            if (properties.Contains(nameof(LatMin)))
                selective.LatMin = LatMin;
            if (properties.Contains(nameof(LatSec)))
                selective.LatSec = LatSec;
            if (properties.Contains(nameof(LatHemis)))
                selective.LatHemis = LatHemis;
            if (properties.Contains(nameof(LatDecimal)))
                selective.LatDecimal = LatDecimal;
            if (properties.Contains(nameof(LongDeg)))
                selective.LongDeg = LongDeg;
            if (properties.Contains(nameof(LongMin)))
                selective.LongMin = LongMin;
            if (properties.Contains(nameof(LongSec)))
                selective.LongSec = LongSec;
            if (properties.Contains(nameof(LongHemis)))
                selective.LongHemis = LongHemis;
            if (properties.Contains(nameof(LongDecimal)))
                selective.LongDecimal = LongDecimal;
            if (properties.Contains(nameof(SurveyAccuracyCode)))
                selective.SurveyAccuracyCode = SurveyAccuracyCode;
            if (properties.Contains(nameof(TacanDmeStatus)))
                selective.TacanDmeStatus = TacanDmeStatus;
            if (properties.Contains(nameof(TacanDmeLatDeg)))
                selective.TacanDmeLatDeg = TacanDmeLatDeg;
            if (properties.Contains(nameof(TacanDmeLatMin)))
                selective.TacanDmeLatMin = TacanDmeLatMin;
            if (properties.Contains(nameof(TacanDmeLatSec)))
                selective.TacanDmeLatSec = TacanDmeLatSec;
            if (properties.Contains(nameof(TacanDmeLatHemis)))
                selective.TacanDmeLatHemis = TacanDmeLatHemis;
            if (properties.Contains(nameof(TacanDmeLatDecimal)))
                selective.TacanDmeLatDecimal = TacanDmeLatDecimal;
            if (properties.Contains(nameof(TacanDmeLongDeg)))
                selective.TacanDmeLongDeg = TacanDmeLongDeg;
            if (properties.Contains(nameof(TacanDmeLongMin)))
                selective.TacanDmeLongMin = TacanDmeLongMin;
            if (properties.Contains(nameof(TacanDmeLongSec)))
                selective.TacanDmeLongSec = TacanDmeLongSec;
            if (properties.Contains(nameof(TacanDmeLongHemis)))
                selective.TacanDmeLongHemis = TacanDmeLongHemis;
            if (properties.Contains(nameof(TacanDmeLongDecimal)))
                selective.TacanDmeLongDecimal = TacanDmeLongDecimal;
            if (properties.Contains(nameof(Elev)))
                selective.Elev = Elev;
            if (properties.Contains(nameof(MagVarn)))
                selective.MagVarn = MagVarn;
            if (properties.Contains(nameof(MagVarnHemis)))
                selective.MagVarnHemis = MagVarnHemis;
            if (properties.Contains(nameof(MagVarnYear)))
                selective.MagVarnYear = MagVarnYear;
            if (properties.Contains(nameof(SimulVoiceFlag)))
                selective.SimulVoiceFlag = SimulVoiceFlag;
            if (properties.Contains(nameof(PwrOutput)))
                selective.PwrOutput = PwrOutput;
            if (properties.Contains(nameof(AutoVoiceIdFlag)))
                selective.AutoVoiceIdFlag = AutoVoiceIdFlag;
            if (properties.Contains(nameof(MntCatCode)))
                selective.MntCatCode = MntCatCode;
            if (properties.Contains(nameof(VoiceCall)))
                selective.VoiceCall = VoiceCall;
            if (properties.Contains(nameof(Chan)))
                selective.Chan = Chan;
            if (properties.Contains(nameof(Freq)))
                selective.Freq = Freq;
            if (properties.Contains(nameof(MkrIdent)))
                selective.MkrIdent = MkrIdent;
            if (properties.Contains(nameof(MkrShape)))
                selective.MkrShape = MkrShape;
            if (properties.Contains(nameof(MkrBrg)))
                selective.MkrBrg = MkrBrg;
            if (properties.Contains(nameof(AltCode)))
                selective.AltCode = AltCode;
            if (properties.Contains(nameof(DmeSsv)))
                selective.DmeSsv = DmeSsv;
            if (properties.Contains(nameof(LowNavOnHighChartFlag)))
                selective.LowNavOnHighChartFlag = LowNavOnHighChartFlag;
            if (properties.Contains(nameof(ZMkrFlag)))
                selective.ZMkrFlag = ZMkrFlag;
            if (properties.Contains(nameof(FssId)))
                selective.FssId = FssId;
            if (properties.Contains(nameof(FssName)))
                selective.FssName = FssName;
            if (properties.Contains(nameof(FssHours)))
                selective.FssHours = FssHours;
            if (properties.Contains(nameof(NotamId)))
                selective.NotamId = NotamId;
            if (properties.Contains(nameof(QuadIdent)))
                selective.QuadIdent = QuadIdent;
            if (properties.Contains(nameof(PitchFlag)))
                selective.PitchFlag = PitchFlag;
            if (properties.Contains(nameof(CatchFlag)))
                selective.CatchFlag = CatchFlag;
            if (properties.Contains(nameof(SuaAtcaaFlag)))
                selective.SuaAtcaaFlag = SuaAtcaaFlag;
            if (properties.Contains(nameof(RestrictionFlag)))
                selective.RestrictionFlag = RestrictionFlag;
            if (properties.Contains(nameof(HiwasFlag)))
                selective.HiwasFlag = HiwasFlag;

            // Spatial
            if (properties.Contains(nameof(Location)))
                selective.Location = Location;
            if (properties.Contains(nameof(TacanDmeLocation)))
                selective.TacanDmeLocation = TacanDmeLocation;

            // JSONB
            if (properties.Contains(nameof(CheckpointsJson)))
                selective.CheckpointsJson = CheckpointsJson;
            if (properties.Contains(nameof(RemarksJson)))
                selective.RemarksJson = RemarksJson;

            return selective;
        }

        private void UpdateAllProperties(Navaid source)
        {
            EffectiveDate = source.EffectiveDate;
            StateCode = source.StateCode;
            NavStatus = source.NavStatus;
            Name = source.Name;
            StateName = source.StateName;
            RegionCode = source.RegionCode;
            CountryName = source.CountryName;
            FanMarker = source.FanMarker;
            Owner = source.Owner;
            Operator = source.Operator;
            NasUseFlag = source.NasUseFlag;
            PublicUseFlag = source.PublicUseFlag;
            NdbClassCode = source.NdbClassCode;
            OperHours = source.OperHours;
            HighAltArtccId = source.HighAltArtccId;
            HighArtccName = source.HighArtccName;
            LowAltArtccId = source.LowAltArtccId;
            LowArtccName = source.LowArtccName;
            LatDeg = source.LatDeg;
            LatMin = source.LatMin;
            LatSec = source.LatSec;
            LatHemis = source.LatHemis;
            LatDecimal = source.LatDecimal;
            LongDeg = source.LongDeg;
            LongMin = source.LongMin;
            LongSec = source.LongSec;
            LongHemis = source.LongHemis;
            LongDecimal = source.LongDecimal;
            SurveyAccuracyCode = source.SurveyAccuracyCode;
            TacanDmeStatus = source.TacanDmeStatus;
            TacanDmeLatDeg = source.TacanDmeLatDeg;
            TacanDmeLatMin = source.TacanDmeLatMin;
            TacanDmeLatSec = source.TacanDmeLatSec;
            TacanDmeLatHemis = source.TacanDmeLatHemis;
            TacanDmeLatDecimal = source.TacanDmeLatDecimal;
            TacanDmeLongDeg = source.TacanDmeLongDeg;
            TacanDmeLongMin = source.TacanDmeLongMin;
            TacanDmeLongSec = source.TacanDmeLongSec;
            TacanDmeLongHemis = source.TacanDmeLongHemis;
            TacanDmeLongDecimal = source.TacanDmeLongDecimal;
            Elev = source.Elev;
            MagVarn = source.MagVarn;
            MagVarnHemis = source.MagVarnHemis;
            MagVarnYear = source.MagVarnYear;
            SimulVoiceFlag = source.SimulVoiceFlag;
            PwrOutput = source.PwrOutput;
            AutoVoiceIdFlag = source.AutoVoiceIdFlag;
            MntCatCode = source.MntCatCode;
            VoiceCall = source.VoiceCall;
            Chan = source.Chan;
            Freq = source.Freq;
            MkrIdent = source.MkrIdent;
            MkrShape = source.MkrShape;
            MkrBrg = source.MkrBrg;
            AltCode = source.AltCode;
            DmeSsv = source.DmeSsv;
            LowNavOnHighChartFlag = source.LowNavOnHighChartFlag;
            ZMkrFlag = source.ZMkrFlag;
            FssId = source.FssId;
            FssName = source.FssName;
            FssHours = source.FssHours;
            NotamId = source.NotamId;
            QuadIdent = source.QuadIdent;
            PitchFlag = source.PitchFlag;
            CatchFlag = source.CatchFlag;
            SuaAtcaaFlag = source.SuaAtcaaFlag;
            RestrictionFlag = source.RestrictionFlag;
            HiwasFlag = source.HiwasFlag;
            Location = source.Location;
            TacanDmeLocation = source.TacanDmeLocation;
            CheckpointsJson = source.CheckpointsJson;
            RemarksJson = source.RemarksJson;
        }

        private void UpdateSelectiveProperties(Navaid source, HashSet<string> limitToProperties)
        {
            if (limitToProperties.Contains(nameof(EffectiveDate)))
                EffectiveDate = source.EffectiveDate;
            if (limitToProperties.Contains(nameof(StateCode)) && source.StateCode != null)
                StateCode = source.StateCode;
            if (limitToProperties.Contains(nameof(NavStatus)) && source.NavStatus != null)
                NavStatus = source.NavStatus;
            if (limitToProperties.Contains(nameof(Name)) && source.Name != null)
                Name = source.Name;
            if (limitToProperties.Contains(nameof(StateName)) && source.StateName != null)
                StateName = source.StateName;
            if (limitToProperties.Contains(nameof(RegionCode)) && source.RegionCode != null)
                RegionCode = source.RegionCode;
            if (limitToProperties.Contains(nameof(CountryName)) && source.CountryName != null)
                CountryName = source.CountryName;
            if (limitToProperties.Contains(nameof(FanMarker)) && source.FanMarker != null)
                FanMarker = source.FanMarker;
            if (limitToProperties.Contains(nameof(Owner)) && source.Owner != null)
                Owner = source.Owner;
            if (limitToProperties.Contains(nameof(Operator)) && source.Operator != null)
                Operator = source.Operator;
            if (limitToProperties.Contains(nameof(NasUseFlag)) && source.NasUseFlag != null)
                NasUseFlag = source.NasUseFlag;
            if (limitToProperties.Contains(nameof(PublicUseFlag)) && source.PublicUseFlag != null)
                PublicUseFlag = source.PublicUseFlag;
            if (limitToProperties.Contains(nameof(NdbClassCode)) && source.NdbClassCode != null)
                NdbClassCode = source.NdbClassCode;
            if (limitToProperties.Contains(nameof(OperHours)) && source.OperHours != null)
                OperHours = source.OperHours;
            if (limitToProperties.Contains(nameof(HighAltArtccId)) && source.HighAltArtccId != null)
                HighAltArtccId = source.HighAltArtccId;
            if (limitToProperties.Contains(nameof(HighArtccName)) && source.HighArtccName != null)
                HighArtccName = source.HighArtccName;
            if (limitToProperties.Contains(nameof(LowAltArtccId)) && source.LowAltArtccId != null)
                LowAltArtccId = source.LowAltArtccId;
            if (limitToProperties.Contains(nameof(LowArtccName)) && source.LowArtccName != null)
                LowArtccName = source.LowArtccName;
            if (limitToProperties.Contains(nameof(LatDeg)) && source.LatDeg != null)
                LatDeg = source.LatDeg;
            if (limitToProperties.Contains(nameof(LatMin)) && source.LatMin != null)
                LatMin = source.LatMin;
            if (limitToProperties.Contains(nameof(LatSec)) && source.LatSec != null)
                LatSec = source.LatSec;
            if (limitToProperties.Contains(nameof(LatHemis)) && source.LatHemis != null)
                LatHemis = source.LatHemis;
            if (limitToProperties.Contains(nameof(LatDecimal)) && source.LatDecimal != null)
                LatDecimal = source.LatDecimal;
            if (limitToProperties.Contains(nameof(LongDeg)) && source.LongDeg != null)
                LongDeg = source.LongDeg;
            if (limitToProperties.Contains(nameof(LongMin)) && source.LongMin != null)
                LongMin = source.LongMin;
            if (limitToProperties.Contains(nameof(LongSec)) && source.LongSec != null)
                LongSec = source.LongSec;
            if (limitToProperties.Contains(nameof(LongHemis)) && source.LongHemis != null)
                LongHemis = source.LongHemis;
            if (limitToProperties.Contains(nameof(LongDecimal)) && source.LongDecimal != null)
                LongDecimal = source.LongDecimal;
            if (limitToProperties.Contains(nameof(SurveyAccuracyCode)) && source.SurveyAccuracyCode != null)
                SurveyAccuracyCode = source.SurveyAccuracyCode;
            if (limitToProperties.Contains(nameof(TacanDmeStatus)) && source.TacanDmeStatus != null)
                TacanDmeStatus = source.TacanDmeStatus;
            if (limitToProperties.Contains(nameof(TacanDmeLatDeg)) && source.TacanDmeLatDeg != null)
                TacanDmeLatDeg = source.TacanDmeLatDeg;
            if (limitToProperties.Contains(nameof(TacanDmeLatMin)) && source.TacanDmeLatMin != null)
                TacanDmeLatMin = source.TacanDmeLatMin;
            if (limitToProperties.Contains(nameof(TacanDmeLatSec)) && source.TacanDmeLatSec != null)
                TacanDmeLatSec = source.TacanDmeLatSec;
            if (limitToProperties.Contains(nameof(TacanDmeLatHemis)) && source.TacanDmeLatHemis != null)
                TacanDmeLatHemis = source.TacanDmeLatHemis;
            if (limitToProperties.Contains(nameof(TacanDmeLatDecimal)) && source.TacanDmeLatDecimal != null)
                TacanDmeLatDecimal = source.TacanDmeLatDecimal;
            if (limitToProperties.Contains(nameof(TacanDmeLongDeg)) && source.TacanDmeLongDeg != null)
                TacanDmeLongDeg = source.TacanDmeLongDeg;
            if (limitToProperties.Contains(nameof(TacanDmeLongMin)) && source.TacanDmeLongMin != null)
                TacanDmeLongMin = source.TacanDmeLongMin;
            if (limitToProperties.Contains(nameof(TacanDmeLongSec)) && source.TacanDmeLongSec != null)
                TacanDmeLongSec = source.TacanDmeLongSec;
            if (limitToProperties.Contains(nameof(TacanDmeLongHemis)) && source.TacanDmeLongHemis != null)
                TacanDmeLongHemis = source.TacanDmeLongHemis;
            if (limitToProperties.Contains(nameof(TacanDmeLongDecimal)) && source.TacanDmeLongDecimal != null)
                TacanDmeLongDecimal = source.TacanDmeLongDecimal;
            if (limitToProperties.Contains(nameof(Elev)) && source.Elev != null)
                Elev = source.Elev;
            if (limitToProperties.Contains(nameof(MagVarn)) && source.MagVarn != null)
                MagVarn = source.MagVarn;
            if (limitToProperties.Contains(nameof(MagVarnHemis)) && source.MagVarnHemis != null)
                MagVarnHemis = source.MagVarnHemis;
            if (limitToProperties.Contains(nameof(MagVarnYear)) && source.MagVarnYear != null)
                MagVarnYear = source.MagVarnYear;
            if (limitToProperties.Contains(nameof(SimulVoiceFlag)) && source.SimulVoiceFlag != null)
                SimulVoiceFlag = source.SimulVoiceFlag;
            if (limitToProperties.Contains(nameof(PwrOutput)) && source.PwrOutput != null)
                PwrOutput = source.PwrOutput;
            if (limitToProperties.Contains(nameof(AutoVoiceIdFlag)) && source.AutoVoiceIdFlag != null)
                AutoVoiceIdFlag = source.AutoVoiceIdFlag;
            if (limitToProperties.Contains(nameof(MntCatCode)) && source.MntCatCode != null)
                MntCatCode = source.MntCatCode;
            if (limitToProperties.Contains(nameof(VoiceCall)) && source.VoiceCall != null)
                VoiceCall = source.VoiceCall;
            if (limitToProperties.Contains(nameof(Chan)) && source.Chan != null)
                Chan = source.Chan;
            if (limitToProperties.Contains(nameof(Freq)) && source.Freq != null)
                Freq = source.Freq;
            if (limitToProperties.Contains(nameof(MkrIdent)) && source.MkrIdent != null)
                MkrIdent = source.MkrIdent;
            if (limitToProperties.Contains(nameof(MkrShape)) && source.MkrShape != null)
                MkrShape = source.MkrShape;
            if (limitToProperties.Contains(nameof(MkrBrg)) && source.MkrBrg != null)
                MkrBrg = source.MkrBrg;
            if (limitToProperties.Contains(nameof(AltCode)) && source.AltCode != null)
                AltCode = source.AltCode;
            if (limitToProperties.Contains(nameof(DmeSsv)) && source.DmeSsv != null)
                DmeSsv = source.DmeSsv;
            if (limitToProperties.Contains(nameof(LowNavOnHighChartFlag)) && source.LowNavOnHighChartFlag != null)
                LowNavOnHighChartFlag = source.LowNavOnHighChartFlag;
            if (limitToProperties.Contains(nameof(ZMkrFlag)) && source.ZMkrFlag != null)
                ZMkrFlag = source.ZMkrFlag;
            if (limitToProperties.Contains(nameof(FssId)) && source.FssId != null)
                FssId = source.FssId;
            if (limitToProperties.Contains(nameof(FssName)) && source.FssName != null)
                FssName = source.FssName;
            if (limitToProperties.Contains(nameof(FssHours)) && source.FssHours != null)
                FssHours = source.FssHours;
            if (limitToProperties.Contains(nameof(NotamId)) && source.NotamId != null)
                NotamId = source.NotamId;
            if (limitToProperties.Contains(nameof(QuadIdent)) && source.QuadIdent != null)
                QuadIdent = source.QuadIdent;
            if (limitToProperties.Contains(nameof(PitchFlag)) && source.PitchFlag != null)
                PitchFlag = source.PitchFlag;
            if (limitToProperties.Contains(nameof(CatchFlag)) && source.CatchFlag != null)
                CatchFlag = source.CatchFlag;
            if (limitToProperties.Contains(nameof(SuaAtcaaFlag)) && source.SuaAtcaaFlag != null)
                SuaAtcaaFlag = source.SuaAtcaaFlag;
            if (limitToProperties.Contains(nameof(RestrictionFlag)) && source.RestrictionFlag != null)
                RestrictionFlag = source.RestrictionFlag;
            if (limitToProperties.Contains(nameof(HiwasFlag)) && source.HiwasFlag != null)
                HiwasFlag = source.HiwasFlag;
            if (limitToProperties.Contains(nameof(Location)) && source.Location != null)
                Location = source.Location;
            if (limitToProperties.Contains(nameof(TacanDmeLocation)) && source.TacanDmeLocation != null)
                TacanDmeLocation = source.TacanDmeLocation;
            if (limitToProperties.Contains(nameof(CheckpointsJson)) && source.CheckpointsJson != null)
                CheckpointsJson = source.CheckpointsJson;
            if (limitToProperties.Contains(nameof(RemarksJson)) && source.RemarksJson != null)
                RemarksJson = source.RemarksJson;
        }
    }
}
