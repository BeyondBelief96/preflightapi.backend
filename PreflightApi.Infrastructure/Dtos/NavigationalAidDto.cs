using PreflightApi.Domain.Constants;

namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// Navigational aid (NAVAID) data from the FAA NASR database (NAV_BASE dataset).
/// Includes VOR, VORTAC, VOR/DME, NDB, TACAN, and other navigation facilities.
/// </summary>
public record NavigationalAidDto()
{
    /// <summary>Internal unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>NAVAID identifier (e.g., DFW, ABQ). Up to 4 characters.</summary>
    public string NavId { get; init; } = string.Empty;

    /// <summary>
    /// NAVAID facility type.
    /// </summary>
    /// <remarks>See <see cref="NavaidValues.NavType"/> for known values (e.g., VOR, VORTAC, VOR/DME, NDB, TACAN, DME, VOT).</remarks>
    public string NavType { get; init; } = string.Empty;

    /// <summary>FAA 28-day NASR publication effective date.</summary>
    public DateTime EffectiveDate { get; init; }

    /// <summary>Two-letter state/territory code (e.g., TX, CA).</summary>
    public string? StateCode { get; init; }

    /// <summary>City associated with the NAVAID.</summary>
    public string? City { get; init; }

    /// <summary>Two-letter country code (e.g., US, CQ for Puerto Rico).</summary>
    public string? CountryCode { get; init; }

    /// <summary>
    /// Current operational status of the NAVAID.
    /// </summary>
    /// <remarks>See <see cref="NavaidValues.NavStatus"/> for known values (e.g., OPERATIONAL IFR, SHUTDOWN).</remarks>
    public string? NavStatus { get; init; }

    /// <summary>Official name of the NAVAID (e.g., DALLAS/FORT WORTH).</summary>
    public string? Name { get; init; }

    /// <summary>Full state or territory name.</summary>
    public string? StateName { get; init; }

    /// <summary>
    /// FAA region code.
    /// </summary>
    /// <remarks>See <see cref="NavaidValues.RegionCode"/> for known values (e.g., ASW, ACE, AEA).</remarks>
    public string? RegionCode { get; init; }

    /// <summary>Full country name.</summary>
    public string? CountryName { get; init; }

    /// <summary>Fan marker identifier, if applicable.</summary>
    public string? FanMarker { get; init; }

    /// <summary>NAVAID facility owner name.</summary>
    public string? Owner { get; init; }

    /// <summary>NAVAID facility operator name.</summary>
    public string? Operator { get; init; }

    /// <summary>National Airspace System (NAS) use flag (Y/N).</summary>
    public string? NasUseFlag { get; init; }

    /// <summary>Public use flag (Y/N).</summary>
    public string? PublicUseFlag { get; init; }

    /// <summary>NDB classification code (e.g., HH, H, MH, MHW, COMPASS LOCATOR).</summary>
    public string? NdbClassCode { get; init; }

    /// <summary>Hours of operation (e.g., CONT for continuous, or specific hours).</summary>
    public string? OperHours { get; init; }

    /// <summary>High-altitude ARTCC identifier providing enroute service.</summary>
    public string? HighAltArtccId { get; init; }

    /// <summary>High-altitude ARTCC name.</summary>
    public string? HighArtccName { get; init; }

    /// <summary>Low-altitude ARTCC identifier providing enroute service.</summary>
    public string? LowAltArtccId { get; init; }

    /// <summary>Low-altitude ARTCC name.</summary>
    public string? LowArtccName { get; init; }

    /// <summary>Latitude in decimal degrees (positive = north).</summary>
    public decimal? LatDecimal { get; init; }

    /// <summary>Longitude in decimal degrees (negative = west).</summary>
    public decimal? LongDecimal { get; init; }

    /// <summary>
    /// Latitude/longitude survey accuracy code.
    /// </summary>
    /// <remarks>See <see cref="NavaidValues.SurveyAccuracy"/> for known values (0–7, from unknown to 3rd order triangulation).</remarks>
    public string? SurveyAccuracyCode { get; init; }

    /// <summary>TACAN/DME operational status (e.g., OPERATIONAL, SHUTDOWN).</summary>
    public string? TacanDmeStatus { get; init; }

    /// <summary>TACAN/DME latitude in decimal degrees, if different from NAVAID location.</summary>
    public decimal? TacanDmeLatDecimal { get; init; }

    /// <summary>TACAN/DME longitude in decimal degrees, if different from NAVAID location.</summary>
    public decimal? TacanDmeLongDecimal { get; init; }

    /// <summary>Elevation in feet MSL.</summary>
    public decimal? Elevation { get; init; }

    /// <summary>Magnetic variation in degrees.</summary>
    public string? MagVarn { get; init; }

    /// <summary>Magnetic variation hemisphere (E or W).</summary>
    public string? MagVarnHemis { get; init; }

    /// <summary>Year the magnetic variation was measured.</summary>
    public string? MagVarnYear { get; init; }

    /// <summary>Simultaneous voice transmission capability flag (Y/N).</summary>
    public string? SimulVoiceFlag { get; init; }

    /// <summary>Transmitter power output in watts.</summary>
    public string? PowerOutput { get; init; }

    /// <summary>Automatic voice identification flag (Y/N).</summary>
    public string? AutoVoiceIdFlag { get; init; }

    /// <summary>
    /// Monitoring category code defining how the NAVAID is monitored.
    /// </summary>
    /// <remarks>See <see cref="NavaidValues.MonitoringCategory"/> for known values (1–4).</remarks>
    public string? MonitoringCategoryCode { get; init; }

    /// <summary>Radio voice call name (e.g., DALLAS RADIO).</summary>
    public string? VoiceCall { get; init; }

    /// <summary>TACAN channel (e.g., 70X, 116Y).</summary>
    public string? Channel { get; init; }

    /// <summary>Frequency in MHz (VOR/DME) or kHz (NDB).</summary>
    public string? Frequency { get; init; }

    /// <summary>Fan marker or marine beacon identifier.</summary>
    public string? MarkerIdent { get; init; }

    /// <summary>Fan marker beacon shape (e.g., ELLIPTICAL, BONE).</summary>
    public string? MarkerShape { get; init; }

    /// <summary>Fan marker beacon bearing from NAVAID.</summary>
    public string? MarkerBearing { get; init; }

    /// <summary>
    /// VOR standard service volume altitude code.
    /// </summary>
    /// <remarks>See <see cref="NavaidValues.AltitudeServiceVolume"/> for known values (H, L, T, VH, VL).</remarks>
    public string? AltitudeCode { get; init; }

    /// <summary>
    /// DME standard service volume code.
    /// </summary>
    /// <remarks>See <see cref="NavaidValues.DmeServiceVolume"/> for known values (H, L, T, DH, DL).</remarks>
    public string? DmeSsv { get; init; }

    /// <summary>Low altitude NAVAID depicted on high altitude chart flag (Y/N).</summary>
    public string? LowNavOnHighChartFlag { get; init; }

    /// <summary>Z-marker present flag (Y/N).</summary>
    public string? ZMarkerFlag { get; init; }

    /// <summary>Flight Service Station (FSS) identifier providing NAVAID support.</summary>
    public string? FssId { get; init; }

    /// <summary>Flight Service Station name.</summary>
    public string? FssName { get; init; }

    /// <summary>FSS operating hours at this NAVAID.</summary>
    public string? FssHours { get; init; }

    /// <summary>NOTAM accountability identifier.</summary>
    public string? NotamId { get; init; }

    /// <summary>Quadrant identification for IFR procedures.</summary>
    public string? QuadIdent { get; init; }

    /// <summary>Pitch (tilt) flag for directional aid (Y/N).</summary>
    public string? PitchFlag { get; init; }

    /// <summary>Catch flag indicating a catch-type directional aid (Y/N).</summary>
    public string? CatchFlag { get; init; }

    /// <summary>Special Use Airspace / ATC Assigned Airspace flag (Y/N).</summary>
    public string? SuaAtcaaFlag { get; init; }

    /// <summary>Restriction flag indicating usage restrictions apply (Y/N).</summary>
    public string? RestrictionFlag { get; init; }

    /// <summary>Hazardous Inflight Weather Advisory Service (HIWAS) broadcast flag (Y/N).</summary>
    public string? HiwasFlag { get; init; }

    /// <summary>Concatenated remarks from FAA NASR supplementary data.</summary>
    public string? Remarks { get; init; }

    /// <summary>VOR receiver checkpoints for this NAVAID, from the NAV_CKPT supplementary dataset.</summary>
    public List<NavaidCheckpointDto>? Checkpoints { get; init; }
}
