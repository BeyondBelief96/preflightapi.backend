using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PreflightApi.Domain.ValueObjects.Fixes;

namespace PreflightApi.Domain.Entities;

[Table("fixes")]
public class Fix : INasrEntity<Fix>
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [Column("fix_id", TypeName = "varchar(30)")]
    [Required]
    public string FixId { get; set; } = string.Empty;

    [Column("icao_region_code", TypeName = "varchar(4)")]
    [Required]
    public string IcaoRegionCode { get; set; } = string.Empty;

    [Column("state_code", TypeName = "varchar(2)")]
    public string? StateCode { get; set; }

    [Column("country_code", TypeName = "varchar(2)")]
    public string? CountryCode { get; set; }

    [Column("effective_date")]
    public DateTime EffectiveDate { get; set; }

    [Column("lat_decimal", TypeName = "decimal(10,8)")]
    public decimal? LatDecimal { get; set; }

    [Column("long_decimal", TypeName = "decimal(11,8)")]
    public decimal? LongDecimal { get; set; }

    [Column("fix_id_old", TypeName = "varchar(30)")]
    public string? FixIdOld { get; set; }

    [Column("charting_remark", TypeName = "varchar(200)")]
    public string? ChartingRemark { get; set; }

    [Column("fix_use_code", TypeName = "varchar(20)")]
    public string? FixUseCode { get; set; }

    [Column("artcc_id_high", TypeName = "varchar(4)")]
    public string? ArtccIdHigh { get; set; }

    [Column("artcc_id_low", TypeName = "varchar(4)")]
    public string? ArtccIdLow { get; set; }

    [Column("pitch_flag", TypeName = "varchar(3)")]
    public string? PitchFlag { get; set; }

    [Column("catch_flag", TypeName = "varchar(3)")]
    public string? CatchFlag { get; set; }

    [Column("sua_atcaa_flag", TypeName = "varchar(3)")]
    public string? SuaAtcaaFlag { get; set; }

    [Column("min_recep_alt", TypeName = "varchar(10)")]
    public string? MinReceptionAlt { get; set; }

    [Column("compulsory", TypeName = "varchar(10)")]
    public string? Compulsory { get; set; }

    [Column("charts", TypeName = "text")]
    public string? Charts { get; set; }

    [Column("charting_types", TypeName = "jsonb")]
    public List<string>? ChartingTypes { get; set; }

    [Column("navaid_references", TypeName = "jsonb")]
    public List<FixNavaidReference>? NavaidReferences { get; set; }

    public string CreateUniqueKey()
    {
        return string.Join("|", FixId, IcaoRegionCode, StateCode, CountryCode);
    }

    public void UpdateFrom(Fix source, HashSet<string>? limitToProperties = null)
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

    public Fix CreateSelectiveEntity(HashSet<string> properties)
    {
        var selective = new Fix();

        if (properties.Contains(nameof(FixId))) selective.FixId = FixId;
        if (properties.Contains(nameof(IcaoRegionCode))) selective.IcaoRegionCode = IcaoRegionCode;
        if (properties.Contains(nameof(StateCode))) selective.StateCode = StateCode;
        if (properties.Contains(nameof(CountryCode))) selective.CountryCode = CountryCode;
        if (properties.Contains(nameof(EffectiveDate))) selective.EffectiveDate = EffectiveDate;
        if (properties.Contains(nameof(LatDecimal))) selective.LatDecimal = LatDecimal;
        if (properties.Contains(nameof(LongDecimal))) selective.LongDecimal = LongDecimal;
        if (properties.Contains(nameof(FixIdOld))) selective.FixIdOld = FixIdOld;
        if (properties.Contains(nameof(ChartingRemark))) selective.ChartingRemark = ChartingRemark;
        if (properties.Contains(nameof(FixUseCode))) selective.FixUseCode = FixUseCode;
        if (properties.Contains(nameof(ArtccIdHigh))) selective.ArtccIdHigh = ArtccIdHigh;
        if (properties.Contains(nameof(ArtccIdLow))) selective.ArtccIdLow = ArtccIdLow;
        if (properties.Contains(nameof(PitchFlag))) selective.PitchFlag = PitchFlag;
        if (properties.Contains(nameof(CatchFlag))) selective.CatchFlag = CatchFlag;
        if (properties.Contains(nameof(SuaAtcaaFlag))) selective.SuaAtcaaFlag = SuaAtcaaFlag;
        if (properties.Contains(nameof(MinReceptionAlt))) selective.MinReceptionAlt = MinReceptionAlt;
        if (properties.Contains(nameof(Compulsory))) selective.Compulsory = Compulsory;
        if (properties.Contains(nameof(Charts))) selective.Charts = Charts;
        if (properties.Contains(nameof(ChartingTypes))) selective.ChartingTypes = ChartingTypes;
        if (properties.Contains(nameof(NavaidReferences))) selective.NavaidReferences = NavaidReferences;

        return selective;
    }

    private void UpdateAllProperties(Fix source)
    {
        EffectiveDate = source.EffectiveDate;
        LatDecimal = source.LatDecimal;
        LongDecimal = source.LongDecimal;
        FixIdOld = source.FixIdOld;
        ChartingRemark = source.ChartingRemark;
        FixUseCode = source.FixUseCode;
        ArtccIdHigh = source.ArtccIdHigh;
        ArtccIdLow = source.ArtccIdLow;
        PitchFlag = source.PitchFlag;
        CatchFlag = source.CatchFlag;
        SuaAtcaaFlag = source.SuaAtcaaFlag;
        MinReceptionAlt = source.MinReceptionAlt;
        Compulsory = source.Compulsory;
        Charts = source.Charts;
        ChartingTypes = source.ChartingTypes;
        NavaidReferences = source.NavaidReferences;
    }

    private void UpdateSelectiveProperties(Fix source, HashSet<string> limitToProperties)
    {
        if (limitToProperties.Contains(nameof(EffectiveDate))) EffectiveDate = source.EffectiveDate;
        if (limitToProperties.Contains(nameof(LatDecimal)) && source.LatDecimal != null)
            LatDecimal = source.LatDecimal;
        if (limitToProperties.Contains(nameof(LongDecimal)) && source.LongDecimal != null)
            LongDecimal = source.LongDecimal;
        if (limitToProperties.Contains(nameof(FixIdOld)) && source.FixIdOld != null)
            FixIdOld = source.FixIdOld;
        if (limitToProperties.Contains(nameof(ChartingRemark)) && source.ChartingRemark != null)
            ChartingRemark = source.ChartingRemark;
        if (limitToProperties.Contains(nameof(FixUseCode)) && source.FixUseCode != null)
            FixUseCode = source.FixUseCode;
        if (limitToProperties.Contains(nameof(ArtccIdHigh)) && source.ArtccIdHigh != null)
            ArtccIdHigh = source.ArtccIdHigh;
        if (limitToProperties.Contains(nameof(ArtccIdLow)) && source.ArtccIdLow != null)
            ArtccIdLow = source.ArtccIdLow;
        if (limitToProperties.Contains(nameof(PitchFlag)) && source.PitchFlag != null)
            PitchFlag = source.PitchFlag;
        if (limitToProperties.Contains(nameof(CatchFlag)) && source.CatchFlag != null)
            CatchFlag = source.CatchFlag;
        if (limitToProperties.Contains(nameof(SuaAtcaaFlag)) && source.SuaAtcaaFlag != null)
            SuaAtcaaFlag = source.SuaAtcaaFlag;
        if (limitToProperties.Contains(nameof(MinReceptionAlt)) && source.MinReceptionAlt != null)
            MinReceptionAlt = source.MinReceptionAlt;
        if (limitToProperties.Contains(nameof(Compulsory)) && source.Compulsory != null)
            Compulsory = source.Compulsory;
        if (limitToProperties.Contains(nameof(Charts)) && source.Charts != null)
            Charts = source.Charts;
        if (limitToProperties.Contains(nameof(ChartingTypes)) && source.ChartingTypes != null)
            ChartingTypes = source.ChartingTypes;
        if (limitToProperties.Contains(nameof(NavaidReferences)) && source.NavaidReferences != null)
            NavaidReferences = source.NavaidReferences;
    }
}
