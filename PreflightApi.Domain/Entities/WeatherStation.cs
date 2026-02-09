using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PreflightApi.Domain.Entities;

[Table("weather_stations")]
public class WeatherStation : INasrEntity<WeatherStation>
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [Column("asos_awos_id", TypeName = "varchar(30)")]
    [Required]
    public string AsosAwosId { get; set; } = string.Empty;

    [Column("asos_awos_type", TypeName = "varchar(20)")]
    [Required]
    public string AsosAwosType { get; set; } = string.Empty;

    [Column("effective_date")]
    public DateTime EffectiveDate { get; set; }

    [Column("state_code", TypeName = "varchar(2)")]
    public string? StateCode { get; set; }

    [Column("city", TypeName = "varchar(40)")]
    public string? City { get; set; }

    [Column("country_code", TypeName = "varchar(2)")]
    public string? CountryCode { get; set; }

    [Column("commissioned_date")]
    public DateTime? CommissionedDate { get; set; }

    [Column("navaid_flag", TypeName = "varchar(1)")]
    public string? NavaidFlag { get; set; }

    [Column("lat_decimal", TypeName = "decimal(10,8)")]
    public decimal? LatDecimal { get; set; }

    [Column("long_decimal", TypeName = "decimal(11,8)")]
    public decimal? LongDecimal { get; set; }

    [Column("elevation", TypeName = "decimal(7,1)")]
    public decimal? Elevation { get; set; }

    [Column("survey_method_code", TypeName = "varchar(1)")]
    public string? SurveyMethodCode { get; set; }

    [Column("phone_no", TypeName = "varchar(20)")]
    public string? PhoneNo { get; set; }

    [Column("second_phone_no", TypeName = "varchar(20)")]
    public string? SecondPhoneNo { get; set; }

    [Column("site_no", TypeName = "varchar(30)")]
    public string? SiteNo { get; set; }

    [Column("site_type_code", TypeName = "varchar(10)")]
    public string? SiteTypeCode { get; set; }

    [Column("remarks", TypeName = "text")]
    public string? Remarks { get; set; }

    public string CreateUniqueKey()
    {
        return string.Join("|", AsosAwosId, AsosAwosType);
    }

    public void UpdateFrom(WeatherStation source, HashSet<string>? limitToProperties = null)
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

    public WeatherStation CreateSelectiveEntity(HashSet<string> properties)
    {
        var selective = new WeatherStation();

        if (properties.Contains(nameof(AsosAwosId))) selective.AsosAwosId = AsosAwosId;
        if (properties.Contains(nameof(AsosAwosType))) selective.AsosAwosType = AsosAwosType;
        if (properties.Contains(nameof(EffectiveDate))) selective.EffectiveDate = EffectiveDate;
        if (properties.Contains(nameof(StateCode))) selective.StateCode = StateCode;
        if (properties.Contains(nameof(City))) selective.City = City;
        if (properties.Contains(nameof(CountryCode))) selective.CountryCode = CountryCode;
        if (properties.Contains(nameof(CommissionedDate))) selective.CommissionedDate = CommissionedDate;
        if (properties.Contains(nameof(NavaidFlag))) selective.NavaidFlag = NavaidFlag;
        if (properties.Contains(nameof(LatDecimal))) selective.LatDecimal = LatDecimal;
        if (properties.Contains(nameof(LongDecimal))) selective.LongDecimal = LongDecimal;
        if (properties.Contains(nameof(Elevation))) selective.Elevation = Elevation;
        if (properties.Contains(nameof(SurveyMethodCode))) selective.SurveyMethodCode = SurveyMethodCode;
        if (properties.Contains(nameof(PhoneNo))) selective.PhoneNo = PhoneNo;
        if (properties.Contains(nameof(SecondPhoneNo))) selective.SecondPhoneNo = SecondPhoneNo;
        if (properties.Contains(nameof(SiteNo))) selective.SiteNo = SiteNo;
        if (properties.Contains(nameof(SiteTypeCode))) selective.SiteTypeCode = SiteTypeCode;
        if (properties.Contains(nameof(Remarks))) selective.Remarks = Remarks;

        return selective;
    }

    private void UpdateAllProperties(WeatherStation source)
    {
        EffectiveDate = source.EffectiveDate;
        StateCode = source.StateCode;
        City = source.City;
        CountryCode = source.CountryCode;
        CommissionedDate = source.CommissionedDate;
        NavaidFlag = source.NavaidFlag;
        LatDecimal = source.LatDecimal;
        LongDecimal = source.LongDecimal;
        Elevation = source.Elevation;
        SurveyMethodCode = source.SurveyMethodCode;
        PhoneNo = source.PhoneNo;
        SecondPhoneNo = source.SecondPhoneNo;
        SiteNo = source.SiteNo;
        SiteTypeCode = source.SiteTypeCode;
        Remarks = source.Remarks;
    }

    private void UpdateSelectiveProperties(WeatherStation source, HashSet<string> limitToProperties)
    {
        if (limitToProperties.Contains(nameof(EffectiveDate))) EffectiveDate = source.EffectiveDate;
        if (limitToProperties.Contains(nameof(StateCode)) && source.StateCode != null)
            StateCode = source.StateCode;
        if (limitToProperties.Contains(nameof(City)) && source.City != null)
            City = source.City;
        if (limitToProperties.Contains(nameof(CountryCode)) && source.CountryCode != null)
            CountryCode = source.CountryCode;
        if (limitToProperties.Contains(nameof(CommissionedDate)) && source.CommissionedDate != null)
            CommissionedDate = source.CommissionedDate;
        if (limitToProperties.Contains(nameof(NavaidFlag)) && source.NavaidFlag != null)
            NavaidFlag = source.NavaidFlag;
        if (limitToProperties.Contains(nameof(LatDecimal)) && source.LatDecimal != null)
            LatDecimal = source.LatDecimal;
        if (limitToProperties.Contains(nameof(LongDecimal)) && source.LongDecimal != null)
            LongDecimal = source.LongDecimal;
        if (limitToProperties.Contains(nameof(Elevation)) && source.Elevation != null)
            Elevation = source.Elevation;
        if (limitToProperties.Contains(nameof(SurveyMethodCode)) && source.SurveyMethodCode != null)
            SurveyMethodCode = source.SurveyMethodCode;
        if (limitToProperties.Contains(nameof(PhoneNo)) && source.PhoneNo != null)
            PhoneNo = source.PhoneNo;
        if (limitToProperties.Contains(nameof(SecondPhoneNo)) && source.SecondPhoneNo != null)
            SecondPhoneNo = source.SecondPhoneNo;
        if (limitToProperties.Contains(nameof(SiteNo)) && source.SiteNo != null)
            SiteNo = source.SiteNo;
        if (limitToProperties.Contains(nameof(SiteTypeCode)) && source.SiteTypeCode != null)
            SiteTypeCode = source.SiteTypeCode;
        if (limitToProperties.Contains(nameof(Remarks)) && source.Remarks != null)
            Remarks = source.Remarks;
    }
}
