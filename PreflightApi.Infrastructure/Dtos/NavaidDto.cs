using PreflightApi.Domain.Enums;
using PreflightApi.Domain.ValueObjects;

namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// Navigation aid (NAVAID) data from the FAA National Airspace System Resources (NASR) database.
/// Includes VOR, VORTAC, VOR/DME, NDB, NDB/DME, TACAN, DME, and other facility types.
/// Use the navaid's NavId to query related endpoints such as identifier lookup
/// (<c>GET /api/v1/navaids/{navId}</c>), batch lookup
/// (<c>GET /api/v1/navaids/batch?ids=DFW,AUS</c>), and nearby search
/// (<c>GET /api/v1/navaids/nearby?lat=32.897&amp;lon=-97.038</c>).
/// </summary>
public record NavaidDto
{
    // ── Identification ──────────────────────────────────────────────────

    /// <summary>Internal unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>FAA NASR field: NAV_ID. NAVAID facility identifier (e.g., DFW, AUS). Not globally unique — the same identifier may exist for different facility types.</summary>
    public string NavId { get; init; } = string.Empty;

    /// <summary>FAA NASR field: NAV_TYPE. Facility type (e.g., VOR, VORTAC, NDB, DME, TACAN).</summary>
    public NavaidType? NavType { get; init; }

    /// <summary>FAA NASR field: NAV_STATUS. Current operational status of the facility.</summary>
    public string NavStatus { get; init; } = string.Empty;

    /// <summary>FAA NASR field: NAME. Official name of the NAVAID facility.</summary>
    public string Name { get; init; } = string.Empty;

    // ── Location ────────────────────────────────────────────────────────

    /// <summary>FAA NASR field: CITY. City associated with the NAVAID.</summary>
    public string City { get; init; } = string.Empty;

    /// <summary>FAA NASR field: STATE_CODE. Two-letter state or territory code (e.g., TX, CA).</summary>
    public string? StateCode { get; init; }

    /// <summary>FAA NASR field: STATE_NAME. Full state or territory name.</summary>
    public string? StateName { get; init; }

    /// <summary>FAA NASR field: COUNTRY_CODE. Two-letter country code (e.g., US).</summary>
    public string CountryCode { get; init; } = string.Empty;

    /// <summary>FAA NASR field: COUNTRY_NAME. Full country name.</summary>
    public string CountryName { get; init; } = string.Empty;

    /// <summary>FAA NASR field: LAT_DECIMAL. Latitude in decimal degrees (WGS 84).</summary>
    public decimal? Latitude { get; init; }

    /// <summary>FAA NASR field: LONG_DECIMAL. Longitude in decimal degrees (WGS 84).</summary>
    public decimal? Longitude { get; init; }

    /// <summary>FAA NASR field: ELEV. Elevation in feet above MSL.</summary>
    public decimal? Elevation { get; init; }

    // ── Ownership ───────────────────────────────────────────────────────

    /// <summary>FAA NASR field: OWNER. Name of the facility owner.</summary>
    public string? Owner { get; init; }

    /// <summary>FAA NASR field: OPERATOR. Name of the facility operator.</summary>
    public string? Operator { get; init; }

    /// <summary>FAA NASR field: NAS_USE_FLAG. Whether the facility is part of the National Airspace System.</summary>
    public bool NasUse { get; init; }

    /// <summary>FAA NASR field: PUBLIC_USE_FLAG. Whether the facility is for public use.</summary>
    public bool PublicUse { get; init; }

    // ── Magnetic Variation ──────────────────────────────────────────────

    /// <summary>FAA NASR field: MAG_VARN. Magnetic variation in degrees.</summary>
    public int? MagneticVariation { get; init; }

    /// <summary>FAA NASR field: MAG_VARN_HEMIS. Magnetic variation direction (E or W).</summary>
    public string? MagneticVariationDirection { get; init; }

    /// <summary>FAA NASR field: MAG_VARN_YEAR. Year the magnetic variation was last determined.</summary>
    public int? MagneticVariationYear { get; init; }

    // ── Frequency & Channel ─────────────────────────────────────────────

    /// <summary>FAA NASR field: FREQ. Transmitted frequency in MHz (VOR) or kHz (NDB).</summary>
    public decimal? Frequency { get; init; }

    /// <summary>FAA NASR field: CHAN. TACAN/VORTAC channel designation (e.g., 78X).</summary>
    public string? Channel { get; init; }

    /// <summary>FAA NASR field: VOICE_CALL. Voice call name used by the facility.</summary>
    public string? VoiceCall { get; init; }

    /// <summary>FAA NASR field: OPER_HOURS. Hours of operation (e.g., CONTINUOUS, 0600-2200).</summary>
    public string? OperatingHours { get; init; }

    // ── Classification ──────────────────────────────────────────────────

    /// <summary>FAA NASR field: NDB_CLASS_CODE. NDB class code (e.g., HH, MHW, H-SAB/LOM). Only applicable to NDB facility types.</summary>
    public string? NdbClassCode { get; init; }

    /// <summary>FAA NASR field: ALT_CODE. VOR Standard Service Volume classification (e.g., High, Low, Terminal).</summary>
    public VorServiceVolume? AltCode { get; init; }

    /// <summary>FAA NASR field: DME_SSV. DME Standard Service Volume classification (e.g., High, Low, Terminal).</summary>
    public DmeServiceVolume? DmeSsv { get; init; }

    // ── Capabilities ────────────────────────────────────────────────────

    /// <summary>FAA NASR field: SIMUL_VOICE_FLAG. Whether the facility broadcasts voice simultaneously on the navigation frequency.</summary>
    public bool SimultaneousVoice { get; init; }

    /// <summary>FAA NASR field: AUTO_VOICE_ID_FLAG. Whether the facility has automatic voice identification.</summary>
    public bool AutomaticVoiceId { get; init; }

    /// <summary>FAA NASR field: HIWAS_FLAG. Whether the facility broadcasts Hazardous Inflight Weather Advisory Service.</summary>
    public bool Hiwas { get; init; }

    /// <summary>FAA NASR field: LOW_NAV_ON_HIGH_CHART_FLAG. Whether this low-altitude NAVAID appears on high-altitude charts.</summary>
    public bool LowNavOnHighChart { get; init; }

    /// <summary>FAA NASR field: PWR_OUTPUT. Transmitter power output in watts.</summary>
    public int? PowerOutput { get; init; }

    // ── TACAN/DME ───────────────────────────────────────────────────────

    /// <summary>FAA NASR field: TACAN_DME_STATUS. Operational status of the co-located TACAN or DME component.</summary>
    public string? TacanDmeStatus { get; init; }

    /// <summary>FAA NASR field: TACAN_DME_LAT_DECIMAL. Latitude of the TACAN/DME antenna in decimal degrees (WGS 84). May differ from the VOR position.</summary>
    public decimal? TacanDmeLatitude { get; init; }

    /// <summary>FAA NASR field: TACAN_DME_LONG_DECIMAL. Longitude of the TACAN/DME antenna in decimal degrees (WGS 84).</summary>
    public decimal? TacanDmeLongitude { get; init; }

    // ── ARTCC & FSS ─────────────────────────────────────────────────────

    /// <summary>FAA NASR field: HIGH_ALT_ARTCC_ID. Identifier of the high-altitude Air Route Traffic Control Center.</summary>
    public string? HighAltArtccId { get; init; }

    /// <summary>FAA NASR field: HIGH_ARTCC_NAME. Name of the high-altitude ARTCC.</summary>
    public string? HighArtccName { get; init; }

    /// <summary>FAA NASR field: LOW_ALT_ARTCC_ID. Identifier of the low-altitude ARTCC.</summary>
    public string? LowAltArtccId { get; init; }

    /// <summary>FAA NASR field: LOW_ARTCC_NAME. Name of the low-altitude ARTCC.</summary>
    public string? LowArtccName { get; init; }

    /// <summary>FAA NASR field: FSS_ID. Identifier of the associated Flight Service Station.</summary>
    public string? FssId { get; init; }

    /// <summary>FAA NASR field: FSS_NAME. Name of the associated Flight Service Station.</summary>
    public string? FssName { get; init; }

    // ── Metadata ────────────────────────────────────────────────────────

    /// <summary>FAA NASR field: NOTAM_ID. NOTAM identifier for the facility.</summary>
    public string? NotamId { get; init; }

    /// <summary>FAA NASR field: EFFECTIVE_DATE. Date the current NASR data cycle became effective. ISO 8601 UTC format.</summary>
    public DateTime EffectiveDate { get; init; }

    // ── Related Data ────────────────────────────────────────────────────

    /// <summary>VOR receiver checkpoints associated with this NAVAID (from NAV2 records).</summary>
    public List<NavaidCheckpoint>? Checkpoints { get; init; }

    /// <summary>Remarks associated with this NAVAID (from NAV6 records).</summary>
    public List<NavaidRemark>? Remarks { get; init; }
}
