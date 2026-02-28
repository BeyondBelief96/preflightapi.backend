using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace PreflightApi.Domain.Entities;

/// <summary>
/// Runway data from the FAA NASR database. Sourced from APT_RWY CSV file in the FAA NASR 28-day subscription.
/// Ordered by SITE_NO, SITE_TYPE_CODE, RWY_ID.
/// </summary>
[Table("runways")]
public class Runway : INasrEntity<Runway>
{
    /// <summary>System-generated unique identifier.</summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    /// <summary>FAA NASR field: SITE_NO. Unique Site Number of the parent airport facility.</summary>
    [Column("site_no")]
    [Required]
    public string SiteNo { get; set; } = string.Empty;

    /// <summary>FAA NASR field: RWY_ID. Runway identification (e.g., "01/19", "09L/27R", "H1" for helipad).</summary>
    [Column("runway_id")]
    [Required]
    public string RunwayId { get; set; } = string.Empty;

    /// <summary>FAA NASR field: RWY_LEN. Physical runway length to the nearest foot.</summary>
    [Column("length")]
    public int? Length { get; set; }

    /// <summary>FAA NASR field: RWY_WIDTH. Physical runway width to the nearest foot.</summary>
    [Column("width")]
    public int? Width { get; set; }

    /// <summary>
    /// FAA NASR field: SURFACE_TYPE_CODE. Runway surface type. May be a single type or a combination
    /// of two types when the runway is composed of distinct sections.
    /// <para>Common values: CONC (Portland Cement Concrete), ASPH (Asphalt or Bituminous Concrete),
    /// SNOW, ICE, MATS (PSP/Landing Mats/Membranes), TREATED (Oiled/Soil Cement/Lime Stabilized),
    /// GRAVEL (Gravel/Cinders/Crushed Rock/Coral/Shells/Slag), TURF (Grass/Sod), DIRT (Natural Soil),
    /// PEM (Partially Concrete/Asphalt/Bitumen-Bound Macadam), ROOF-TOP, WATER.</para>
    /// <para>Less common: ALUMINUM, BRICK, CALICHE, CORAL, DECK, GRASS, METAL, NSTD, OIL&amp;CHIP,
    /// PSP, SAND, SOD, STEEL, TRTD, WOOD.</para>
    /// </summary>
    [Column("surface_type_code")]
    public string? SurfaceTypeCode { get; set; }

    /// <summary>
    /// FAA NASR field: TREATMENT_CODE. Runway surface treatment.
    /// <para>Possible values: GRVD (Saw-Cut or Plastic Grooved), PFC (Porous Friction Course),
    /// AFSC (Aggregate Friction Seal Coat), RFSC (Rubberized Friction Seal Coat),
    /// WC (Wire Comb or Wire Tine), NONE (No Special Surface Treatment).</para>
    /// </summary>
    [Column("surface_treatment_code")]
    public string? SurfaceTreatmentCode { get; set; }

    /// <summary>FAA NASR field: PCN. Pavement Classification Number. See FAA Advisory Circular 150/5335-5 for code definitions and PCN determination formula.</summary>
    [Column("pavement_classification")]
    public string? PavementClassification { get; set; }

    /// <summary>
    /// FAA NASR field: RWY_LGT_CODE. Runway lights edge intensity.
    /// <para>Possible values: HIGH, MED (Medium), LOW, FLD (Flood), NSTD (Non-Standard Lighting System),
    /// PERI (Perimeter), STRB (Strobe), NONE (No Edge Lighting System).</para>
    /// </summary>
    [Column("edge_light_intensity")]
    public string? EdgeLightIntensity { get; set; }

    /// <summary>FAA NASR field: GROSS_WT_SW. Runway weight-bearing capacity for single wheel type landing gear, in pounds.</summary>
    [Column("weight_bearing_single_wheel")]
    public int? WeightBearingSingleWheel { get; set; }

    /// <summary>FAA NASR field: GROSS_WT_DW. Runway weight-bearing capacity for dual wheel type landing gear, in pounds.</summary>
    [Column("weight_bearing_dual_wheel")]
    public int? WeightBearingDualWheel { get; set; }

    /// <summary>FAA NASR field: GROSS_WT_DTW. Runway weight-bearing capacity for two dual wheels in tandem type landing gear, in pounds.</summary>
    [Column("weight_bearing_dual_tandem")]
    public int? WeightBearingDualTandem { get; set; }

    /// <summary>FAA NASR field: GROSS_WT_DDTW. Runway weight-bearing capacity for two dual wheels in tandem/two dual wheels in double tandem body gear type landing gear, in pounds.</summary>
    [Column("weight_bearing_double_dual_tandem")]
    public int? WeightBearingDoubleDualTandem { get; set; }

    /// <summary>
    /// FAA NASR field: COND. Runway Surface Condition.
    /// <para>Possible values: EXCELLENT, GOOD, FAIR, POOR, FAILED.</para>
    /// </summary>
    [Column("surface_condition")]
    public string? SurfaceCondition { get; set; }

    /// <summary>
    /// FAA NASR field: PAVEMENT_TYPE_CODE. Pavement Type.
    /// <para>Possible values: R (Rigid), F (Flexible).</para>
    /// </summary>
    [Column("pavement_type_code")]
    public string? PavementTypeCode { get; set; }

    /// <summary>FAA NASR field: SUBGRADE_STRENGTH_CODE. Subgrade Strength (Letters A-F).</summary>
    [Column("subgrade_strength_code")]
    public string? SubgradeStrengthCode { get; set; }

    /// <summary>FAA NASR field: TIRE_PRES_CODE. Tire Pressure Code (Letters W-Z).</summary>
    [Column("tire_pressure_code")]
    public string? TirePressureCode { get; set; }

    /// <summary>
    /// FAA NASR field: DTRM_METHOD_CODE. Determination Method for pavement strength.
    /// <para>Possible values: T (Technical), U (Using Aircraft).</para>
    /// </summary>
    [Column("determination_method_code")]
    public string? DeterminationMethodCode { get; set; }

    /// <summary>FAA NASR field: RWY_LEN_SOURCE. Source of runway length information.</summary>
    [Column("runway_length_source")]
    public string? RunwayLengthSource { get; set; }

    /// <summary>FAA NASR field: LENGTH_SOURCE_DATE. Date of runway length source information.</summary>
    [Column("length_source_date")]
    public DateTime? LengthSourceDate { get; set; }

    /// <summary>ArcGIS runway polygon geometry (SRID 4326). Managed exclusively by the ArcGIS geometry sync — not part of NASR data.</summary>
    [Column("geometry", TypeName = "geometry(Polygon, 4326)")]
    public Geometry? Geometry { get; set; }

    /// <summary>Navigation property to the runway ends (typically two per runway, one for each direction).</summary>
    public virtual ICollection<RunwayEnd> RunwayEnds { get; set; } = new List<RunwayEnd>();

    // INasrEntity<Runway> implementation
    public string CreateUniqueKey()
    {
        return string.Join("|", SiteNo, RunwayId);
    }

    public void UpdateFrom(Runway source, HashSet<string>? limitToProperties = null)
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

    public Runway CreateSelectiveEntity(HashSet<string> properties)
    {
        var selective = new Runway();

        if (properties.Contains(nameof(SiteNo)))
            selective.SiteNo = SiteNo;
        if (properties.Contains(nameof(RunwayId)))
            selective.RunwayId = RunwayId;
        if (properties.Contains(nameof(Length)))
            selective.Length = Length;
        if (properties.Contains(nameof(Width)))
            selective.Width = Width;
        if (properties.Contains(nameof(SurfaceTypeCode)))
            selective.SurfaceTypeCode = SurfaceTypeCode;
        if (properties.Contains(nameof(SurfaceTreatmentCode)))
            selective.SurfaceTreatmentCode = SurfaceTreatmentCode;
        if (properties.Contains(nameof(PavementClassification)))
            selective.PavementClassification = PavementClassification;
        if (properties.Contains(nameof(EdgeLightIntensity)))
            selective.EdgeLightIntensity = EdgeLightIntensity;
        if (properties.Contains(nameof(WeightBearingSingleWheel)))
            selective.WeightBearingSingleWheel = WeightBearingSingleWheel;
        if (properties.Contains(nameof(WeightBearingDualWheel)))
            selective.WeightBearingDualWheel = WeightBearingDualWheel;
        if (properties.Contains(nameof(WeightBearingDualTandem)))
            selective.WeightBearingDualTandem = WeightBearingDualTandem;
        if (properties.Contains(nameof(WeightBearingDoubleDualTandem)))
            selective.WeightBearingDoubleDualTandem = WeightBearingDoubleDualTandem;
        if (properties.Contains(nameof(SurfaceCondition)))
            selective.SurfaceCondition = SurfaceCondition;
        if (properties.Contains(nameof(PavementTypeCode)))
            selective.PavementTypeCode = PavementTypeCode;
        if (properties.Contains(nameof(SubgradeStrengthCode)))
            selective.SubgradeStrengthCode = SubgradeStrengthCode;
        if (properties.Contains(nameof(TirePressureCode)))
            selective.TirePressureCode = TirePressureCode;
        if (properties.Contains(nameof(DeterminationMethodCode)))
            selective.DeterminationMethodCode = DeterminationMethodCode;
        if (properties.Contains(nameof(RunwayLengthSource)))
            selective.RunwayLengthSource = RunwayLengthSource;
        if (properties.Contains(nameof(LengthSourceDate)))
            selective.LengthSourceDate = LengthSourceDate;

        return selective;
    }

    private void UpdateAllProperties(Runway source)
    {
        Length = source.Length;
        Width = source.Width;
        SurfaceTypeCode = source.SurfaceTypeCode;
        SurfaceTreatmentCode = source.SurfaceTreatmentCode;
        PavementClassification = source.PavementClassification;
        EdgeLightIntensity = source.EdgeLightIntensity;
        WeightBearingSingleWheel = source.WeightBearingSingleWheel;
        WeightBearingDualWheel = source.WeightBearingDualWheel;
        WeightBearingDualTandem = source.WeightBearingDualTandem;
        WeightBearingDoubleDualTandem = source.WeightBearingDoubleDualTandem;
        SurfaceCondition = source.SurfaceCondition;
        PavementTypeCode = source.PavementTypeCode;
        SubgradeStrengthCode = source.SubgradeStrengthCode;
        TirePressureCode = source.TirePressureCode;
        DeterminationMethodCode = source.DeterminationMethodCode;
        RunwayLengthSource = source.RunwayLengthSource;
        LengthSourceDate = source.LengthSourceDate;
    }

    private void UpdateSelectiveProperties(Runway source, HashSet<string> limitToProperties)
    {
        if (limitToProperties.Contains(nameof(Length)) && source.Length != null)
            Length = source.Length;
        if (limitToProperties.Contains(nameof(Width)) && source.Width != null)
            Width = source.Width;
        if (limitToProperties.Contains(nameof(SurfaceTypeCode)) && source.SurfaceTypeCode != null)
            SurfaceTypeCode = source.SurfaceTypeCode;
        if (limitToProperties.Contains(nameof(SurfaceTreatmentCode)) && source.SurfaceTreatmentCode != null)
            SurfaceTreatmentCode = source.SurfaceTreatmentCode;
        if (limitToProperties.Contains(nameof(PavementClassification)) && source.PavementClassification != null)
            PavementClassification = source.PavementClassification;
        if (limitToProperties.Contains(nameof(EdgeLightIntensity)) && source.EdgeLightIntensity != null)
            EdgeLightIntensity = source.EdgeLightIntensity;
        if (limitToProperties.Contains(nameof(WeightBearingSingleWheel)) && source.WeightBearingSingleWheel != null)
            WeightBearingSingleWheel = source.WeightBearingSingleWheel;
        if (limitToProperties.Contains(nameof(WeightBearingDualWheel)) && source.WeightBearingDualWheel != null)
            WeightBearingDualWheel = source.WeightBearingDualWheel;
        if (limitToProperties.Contains(nameof(WeightBearingDualTandem)) && source.WeightBearingDualTandem != null)
            WeightBearingDualTandem = source.WeightBearingDualTandem;
        if (limitToProperties.Contains(nameof(WeightBearingDoubleDualTandem)) && source.WeightBearingDoubleDualTandem != null)
            WeightBearingDoubleDualTandem = source.WeightBearingDoubleDualTandem;
        if (limitToProperties.Contains(nameof(SurfaceCondition)) && source.SurfaceCondition != null)
            SurfaceCondition = source.SurfaceCondition;
        if (limitToProperties.Contains(nameof(PavementTypeCode)) && source.PavementTypeCode != null)
            PavementTypeCode = source.PavementTypeCode;
        if (limitToProperties.Contains(nameof(SubgradeStrengthCode)) && source.SubgradeStrengthCode != null)
            SubgradeStrengthCode = source.SubgradeStrengthCode;
        if (limitToProperties.Contains(nameof(TirePressureCode)) && source.TirePressureCode != null)
            TirePressureCode = source.TirePressureCode;
        if (limitToProperties.Contains(nameof(DeterminationMethodCode)) && source.DeterminationMethodCode != null)
            DeterminationMethodCode = source.DeterminationMethodCode;
        if (limitToProperties.Contains(nameof(RunwayLengthSource)) && source.RunwayLengthSource != null)
            RunwayLengthSource = source.RunwayLengthSource;
        if (limitToProperties.Contains(nameof(LengthSourceDate)) && source.LengthSourceDate != null)
            LengthSourceDate = source.LengthSourceDate;
    }
}
