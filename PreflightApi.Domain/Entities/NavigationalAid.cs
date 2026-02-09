using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PreflightApi.Domain.ValueObjects.NavigationalAids;

namespace PreflightApi.Domain.Entities;

[Table("navigational_aids")]
public class NavigationalAid : INasrEntity<NavigationalAid>
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [Column("nav_id", TypeName = "varchar(30)")]
    [Required]
    public string NavId { get; set; } = string.Empty;

    [Column("nav_type", TypeName = "varchar(20)")]
    [Required]
    public string NavType { get; set; } = string.Empty;

    [Column("effective_date")]
    public DateTime EffectiveDate { get; set; }

    [Column("state_code", TypeName = "varchar(2)")]
    public string? StateCode { get; set; }

    [Column("city", TypeName = "varchar(40)")]
    public string? City { get; set; }

    [Column("country_code", TypeName = "varchar(2)")]
    public string? CountryCode { get; set; }

    [Column("nav_status", TypeName = "varchar(30)")]
    public string? NavStatus { get; set; }

    [Column("name", TypeName = "varchar(50)")]
    public string? Name { get; set; }

    [Column("state_name", TypeName = "varchar(30)")]
    public string? StateName { get; set; }

    [Column("region_code", TypeName = "varchar(3)")]
    public string? RegionCode { get; set; }

    [Column("country_name", TypeName = "varchar(30)")]
    public string? CountryName { get; set; }

    [Column("fan_marker", TypeName = "varchar(10)")]
    public string? FanMarker { get; set; }

    [Column("owner", TypeName = "varchar(50)")]
    public string? Owner { get; set; }

    [Column("operator", TypeName = "varchar(50)")]
    public string? Operator { get; set; }

    [Column("nas_use_flag", TypeName = "varchar(1)")]
    public string? NasUseFlag { get; set; }

    [Column("public_use_flag", TypeName = "varchar(1)")]
    public string? PublicUseFlag { get; set; }

    [Column("ndb_class_code", TypeName = "varchar(10)")]
    public string? NdbClassCode { get; set; }

    [Column("oper_hours", TypeName = "varchar(30)")]
    public string? OperHours { get; set; }

    [Column("high_alt_artcc_id", TypeName = "varchar(4)")]
    public string? HighAltArtccId { get; set; }

    [Column("high_artcc_name", TypeName = "varchar(30)")]
    public string? HighArtccName { get; set; }

    [Column("low_alt_artcc_id", TypeName = "varchar(4)")]
    public string? LowAltArtccId { get; set; }

    [Column("low_artcc_name", TypeName = "varchar(30)")]
    public string? LowArtccName { get; set; }

    [Column("lat_decimal", TypeName = "decimal(10,8)")]
    public decimal? LatDecimal { get; set; }

    [Column("long_decimal", TypeName = "decimal(11,8)")]
    public decimal? LongDecimal { get; set; }

    [Column("survey_accuracy_code", TypeName = "varchar(1)")]
    public string? SurveyAccuracyCode { get; set; }

    [Column("tacan_dme_status", TypeName = "varchar(30)")]
    public string? TacanDmeStatus { get; set; }

    [Column("tacan_dme_lat_decimal", TypeName = "decimal(10,8)")]
    public decimal? TacanDmeLatDecimal { get; set; }

    [Column("tacan_dme_long_decimal", TypeName = "decimal(11,8)")]
    public decimal? TacanDmeLongDecimal { get; set; }

    [Column("elevation", TypeName = "decimal(7,1)")]
    public decimal? Elevation { get; set; }

    [Column("mag_varn", TypeName = "varchar(10)")]
    public string? MagVarn { get; set; }

    [Column("mag_varn_hemis", TypeName = "varchar(1)")]
    public string? MagVarnHemis { get; set; }

    [Column("mag_varn_year", TypeName = "varchar(4)")]
    public string? MagVarnYear { get; set; }

    [Column("simul_voice_flag", TypeName = "varchar(1)")]
    public string? SimulVoiceFlag { get; set; }

    [Column("power_output", TypeName = "varchar(10)")]
    public string? PowerOutput { get; set; }

    [Column("auto_voice_id_flag", TypeName = "varchar(1)")]
    public string? AutoVoiceIdFlag { get; set; }

    [Column("monitoring_category_code", TypeName = "varchar(3)")]
    public string? MonitoringCategoryCode { get; set; }

    [Column("voice_call", TypeName = "varchar(30)")]
    public string? VoiceCall { get; set; }

    [Column("channel", TypeName = "varchar(10)")]
    public string? Channel { get; set; }

    [Column("frequency", TypeName = "varchar(20)")]
    public string? Frequency { get; set; }

    [Column("marker_ident", TypeName = "varchar(10)")]
    public string? MarkerIdent { get; set; }

    [Column("marker_shape", TypeName = "varchar(10)")]
    public string? MarkerShape { get; set; }

    [Column("marker_bearing", TypeName = "varchar(10)")]
    public string? MarkerBearing { get; set; }

    [Column("altitude_code", TypeName = "varchar(3)")]
    public string? AltitudeCode { get; set; }

    [Column("dme_ssv", TypeName = "varchar(10)")]
    public string? DmeSsv { get; set; }

    [Column("low_nav_on_high_chart_flag", TypeName = "varchar(1)")]
    public string? LowNavOnHighChartFlag { get; set; }

    [Column("z_marker_flag", TypeName = "varchar(1)")]
    public string? ZMarkerFlag { get; set; }

    [Column("fss_id", TypeName = "varchar(4)")]
    public string? FssId { get; set; }

    [Column("fss_name", TypeName = "varchar(30)")]
    public string? FssName { get; set; }

    [Column("fss_hours", TypeName = "varchar(100)")]
    public string? FssHours { get; set; }

    [Column("notam_id", TypeName = "varchar(4)")]
    public string? NotamId { get; set; }

    [Column("quad_ident", TypeName = "varchar(10)")]
    public string? QuadIdent { get; set; }

    [Column("pitch_flag", TypeName = "varchar(3)")]
    public string? PitchFlag { get; set; }

    [Column("catch_flag", TypeName = "varchar(3)")]
    public string? CatchFlag { get; set; }

    [Column("sua_atcaa_flag", TypeName = "varchar(3)")]
    public string? SuaAtcaaFlag { get; set; }

    [Column("restriction_flag", TypeName = "varchar(3)")]
    public string? RestrictionFlag { get; set; }

    [Column("hiwas_flag", TypeName = "varchar(3)")]
    public string? HiwasFlag { get; set; }

    [Column("remarks", TypeName = "text")]
    public string? Remarks { get; set; }

    [Column("checkpoints", TypeName = "jsonb")]
    public List<NavaidCheckpoint>? Checkpoints { get; set; }

    public string CreateUniqueKey()
    {
        return string.Join("|", NavId, NavType);
    }

    public void UpdateFrom(NavigationalAid source, HashSet<string>? limitToProperties = null)
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

    public NavigationalAid CreateSelectiveEntity(HashSet<string> properties)
    {
        var selective = new NavigationalAid();

        if (properties.Contains(nameof(NavId))) selective.NavId = NavId;
        if (properties.Contains(nameof(NavType))) selective.NavType = NavType;
        if (properties.Contains(nameof(EffectiveDate))) selective.EffectiveDate = EffectiveDate;
        if (properties.Contains(nameof(StateCode))) selective.StateCode = StateCode;
        if (properties.Contains(nameof(City))) selective.City = City;
        if (properties.Contains(nameof(CountryCode))) selective.CountryCode = CountryCode;
        if (properties.Contains(nameof(NavStatus))) selective.NavStatus = NavStatus;
        if (properties.Contains(nameof(Name))) selective.Name = Name;
        if (properties.Contains(nameof(StateName))) selective.StateName = StateName;
        if (properties.Contains(nameof(RegionCode))) selective.RegionCode = RegionCode;
        if (properties.Contains(nameof(CountryName))) selective.CountryName = CountryName;
        if (properties.Contains(nameof(FanMarker))) selective.FanMarker = FanMarker;
        if (properties.Contains(nameof(Owner))) selective.Owner = Owner;
        if (properties.Contains(nameof(Operator))) selective.Operator = Operator;
        if (properties.Contains(nameof(NasUseFlag))) selective.NasUseFlag = NasUseFlag;
        if (properties.Contains(nameof(PublicUseFlag))) selective.PublicUseFlag = PublicUseFlag;
        if (properties.Contains(nameof(NdbClassCode))) selective.NdbClassCode = NdbClassCode;
        if (properties.Contains(nameof(OperHours))) selective.OperHours = OperHours;
        if (properties.Contains(nameof(HighAltArtccId))) selective.HighAltArtccId = HighAltArtccId;
        if (properties.Contains(nameof(HighArtccName))) selective.HighArtccName = HighArtccName;
        if (properties.Contains(nameof(LowAltArtccId))) selective.LowAltArtccId = LowAltArtccId;
        if (properties.Contains(nameof(LowArtccName))) selective.LowArtccName = LowArtccName;
        if (properties.Contains(nameof(LatDecimal))) selective.LatDecimal = LatDecimal;
        if (properties.Contains(nameof(LongDecimal))) selective.LongDecimal = LongDecimal;
        if (properties.Contains(nameof(SurveyAccuracyCode))) selective.SurveyAccuracyCode = SurveyAccuracyCode;
        if (properties.Contains(nameof(TacanDmeStatus))) selective.TacanDmeStatus = TacanDmeStatus;
        if (properties.Contains(nameof(TacanDmeLatDecimal))) selective.TacanDmeLatDecimal = TacanDmeLatDecimal;
        if (properties.Contains(nameof(TacanDmeLongDecimal))) selective.TacanDmeLongDecimal = TacanDmeLongDecimal;
        if (properties.Contains(nameof(Elevation))) selective.Elevation = Elevation;
        if (properties.Contains(nameof(MagVarn))) selective.MagVarn = MagVarn;
        if (properties.Contains(nameof(MagVarnHemis))) selective.MagVarnHemis = MagVarnHemis;
        if (properties.Contains(nameof(MagVarnYear))) selective.MagVarnYear = MagVarnYear;
        if (properties.Contains(nameof(SimulVoiceFlag))) selective.SimulVoiceFlag = SimulVoiceFlag;
        if (properties.Contains(nameof(PowerOutput))) selective.PowerOutput = PowerOutput;
        if (properties.Contains(nameof(AutoVoiceIdFlag))) selective.AutoVoiceIdFlag = AutoVoiceIdFlag;
        if (properties.Contains(nameof(MonitoringCategoryCode))) selective.MonitoringCategoryCode = MonitoringCategoryCode;
        if (properties.Contains(nameof(VoiceCall))) selective.VoiceCall = VoiceCall;
        if (properties.Contains(nameof(Channel))) selective.Channel = Channel;
        if (properties.Contains(nameof(Frequency))) selective.Frequency = Frequency;
        if (properties.Contains(nameof(MarkerIdent))) selective.MarkerIdent = MarkerIdent;
        if (properties.Contains(nameof(MarkerShape))) selective.MarkerShape = MarkerShape;
        if (properties.Contains(nameof(MarkerBearing))) selective.MarkerBearing = MarkerBearing;
        if (properties.Contains(nameof(AltitudeCode))) selective.AltitudeCode = AltitudeCode;
        if (properties.Contains(nameof(DmeSsv))) selective.DmeSsv = DmeSsv;
        if (properties.Contains(nameof(LowNavOnHighChartFlag))) selective.LowNavOnHighChartFlag = LowNavOnHighChartFlag;
        if (properties.Contains(nameof(ZMarkerFlag))) selective.ZMarkerFlag = ZMarkerFlag;
        if (properties.Contains(nameof(FssId))) selective.FssId = FssId;
        if (properties.Contains(nameof(FssName))) selective.FssName = FssName;
        if (properties.Contains(nameof(FssHours))) selective.FssHours = FssHours;
        if (properties.Contains(nameof(NotamId))) selective.NotamId = NotamId;
        if (properties.Contains(nameof(QuadIdent))) selective.QuadIdent = QuadIdent;
        if (properties.Contains(nameof(PitchFlag))) selective.PitchFlag = PitchFlag;
        if (properties.Contains(nameof(CatchFlag))) selective.CatchFlag = CatchFlag;
        if (properties.Contains(nameof(SuaAtcaaFlag))) selective.SuaAtcaaFlag = SuaAtcaaFlag;
        if (properties.Contains(nameof(RestrictionFlag))) selective.RestrictionFlag = RestrictionFlag;
        if (properties.Contains(nameof(HiwasFlag))) selective.HiwasFlag = HiwasFlag;
        if (properties.Contains(nameof(Remarks))) selective.Remarks = Remarks;
        if (properties.Contains(nameof(Checkpoints))) selective.Checkpoints = Checkpoints;

        return selective;
    }

    private void UpdateAllProperties(NavigationalAid source)
    {
        EffectiveDate = source.EffectiveDate;
        StateCode = source.StateCode;
        City = source.City;
        CountryCode = source.CountryCode;
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
        LatDecimal = source.LatDecimal;
        LongDecimal = source.LongDecimal;
        SurveyAccuracyCode = source.SurveyAccuracyCode;
        TacanDmeStatus = source.TacanDmeStatus;
        TacanDmeLatDecimal = source.TacanDmeLatDecimal;
        TacanDmeLongDecimal = source.TacanDmeLongDecimal;
        Elevation = source.Elevation;
        MagVarn = source.MagVarn;
        MagVarnHemis = source.MagVarnHemis;
        MagVarnYear = source.MagVarnYear;
        SimulVoiceFlag = source.SimulVoiceFlag;
        PowerOutput = source.PowerOutput;
        AutoVoiceIdFlag = source.AutoVoiceIdFlag;
        MonitoringCategoryCode = source.MonitoringCategoryCode;
        VoiceCall = source.VoiceCall;
        Channel = source.Channel;
        Frequency = source.Frequency;
        MarkerIdent = source.MarkerIdent;
        MarkerShape = source.MarkerShape;
        MarkerBearing = source.MarkerBearing;
        AltitudeCode = source.AltitudeCode;
        DmeSsv = source.DmeSsv;
        LowNavOnHighChartFlag = source.LowNavOnHighChartFlag;
        ZMarkerFlag = source.ZMarkerFlag;
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
        Remarks = source.Remarks;
        Checkpoints = source.Checkpoints;
    }

    private void UpdateSelectiveProperties(NavigationalAid source, HashSet<string> limitToProperties)
    {
        if (limitToProperties.Contains(nameof(EffectiveDate))) EffectiveDate = source.EffectiveDate;
        if (limitToProperties.Contains(nameof(StateCode)) && source.StateCode != null) StateCode = source.StateCode;
        if (limitToProperties.Contains(nameof(City)) && source.City != null) City = source.City;
        if (limitToProperties.Contains(nameof(CountryCode)) && source.CountryCode != null) CountryCode = source.CountryCode;
        if (limitToProperties.Contains(nameof(NavStatus)) && source.NavStatus != null) NavStatus = source.NavStatus;
        if (limitToProperties.Contains(nameof(Name)) && source.Name != null) Name = source.Name;
        if (limitToProperties.Contains(nameof(StateName)) && source.StateName != null) StateName = source.StateName;
        if (limitToProperties.Contains(nameof(RegionCode)) && source.RegionCode != null) RegionCode = source.RegionCode;
        if (limitToProperties.Contains(nameof(CountryName)) && source.CountryName != null) CountryName = source.CountryName;
        if (limitToProperties.Contains(nameof(Remarks)) && source.Remarks != null) Remarks = source.Remarks;
        if (limitToProperties.Contains(nameof(Checkpoints)) && source.Checkpoints != null) Checkpoints = source.Checkpoints;
    }
}
