namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// Communication frequency data from the FAA NASR database, sourced from the FRQ CSV file.
/// Contains radio frequencies for ATC facilities, towers, approach/departure control, and other aviation communication services.
/// </summary>
public record CommunicationFrequencyDto()
{
    /// <summary>System-generated unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>FAA NASR field: FACILITY. Contains FACILITY ID except for FACILITY TYPE AFIS, CTAF, GCO, UNICOM and RCAG. The FACILITY NAME is used for RCAG sites. AFIS, CTAF, GCO and UNICOM are NULL.</summary>
    public string? FacilityCode { get; init; }

    /// <summary>FAA NASR field: EFF_DATE. The 28 Day NASR Subscription Effective Date (YYYY/MM/DD).</summary>
    public DateTime EffectiveDate { get; init; }

    /// <summary>FAA NASR field: FAC_NAME. Official Facility Name. NULL for AFIS, CTAF, GCO, UNICOM (no FACILITY ID or NAME in NASR) and ASOS/AWOS (no FACILITY NAME in NASR).</summary>
    public string? FacilityName { get; init; }

    /// <summary>FAA NASR field: FACILITY_TYPE. All records contain a FACILITY TYPE. Note: RCO and RCO1 serve the same function (remote communication outlet). An RCO1 may exist if two separate sites share the same identifier.</summary>
    public string FacilityType { get; init; } = string.Empty;

    /// <summary>FAA NASR field: ARTCC_OR_FSS_ID. RCAG facilities contain an ARTCC ID; RCO/RCO1 facilities contain an FSS ID. Included for convenience to identify the parent ARTCC or FSS resource in NASR.</summary>
    public string? ArtccOrFssId { get; init; }

    /// <summary>FAA NASR field: CPDLC. A Controller Pilot Data Link Communications (CPDLC) remark associated with a FACILITY.</summary>
    public string? Cpdlc { get; init; }

    /// <summary>FAA NASR field: TOWER_HRS. Tower operating hours. Only listed for ATCT FACILITY TYPEs where the FACILITY equals the SERVICED FACILITY.</summary>
    public string? TowerHours { get; init; }

    /// <summary>FAA NASR field: SERVICED_FACILITY. The FACILITY ID (or FACILITY NAME if FACILITY TYPE is RCAG) that is serviced by the frequencies listed. This is a NON-NULL field.</summary>
    public string ServicedFacility { get; init; } = string.Empty;

    /// <summary>FAA NASR field: SERVICED_FAC_NAME. The FACILITY NAME that is serviced by the frequencies listed.</summary>
    public string? ServicedFacilityName { get; init; }

    /// <summary>FAA NASR field: SERVICED_SITE_TYPE. Facility Type of SERVICED FACILITY.</summary>
    public string? ServicedSiteType { get; init; }

    /// <summary>FAA NASR field: LAT_DECIMAL. Facility Reference Point Latitude in Decimal Format.</summary>
    public decimal? Latitude { get; init; }

    /// <summary>FAA NASR field: LONG_DECIMAL. Facility Reference Point Longitude in Decimal Format.</summary>
    public decimal? Longitude { get; init; }

    /// <summary>FAA NASR field: SERVICED_CITY. Serviced Facility Associated City Name.</summary>
    public string? ServicedCity { get; init; }

    /// <summary>FAA NASR field: SERVICED_STATE. Two-letter state ID of the SERVICED FACILITY.</summary>
    public string? ServicedState { get; init; }

    /// <summary>FAA NASR field: SERVICED_COUNTRY. Country Post Office Code of Serviced Facility.</summary>
    public string? ServicedCountry { get; init; }

    /// <summary>FAA NASR field: TOWER_OR_COMM_CALL. Radio call used by pilot to contact ATC or FSS facility.</summary>
    public string? TowerOrCommCall { get; init; }

    /// <summary>FAA NASR field: PRIMARY_APPROACH_RADIO_CALL. Radio call of facility that furnishes primary approach control.</summary>
    public string? PrimaryApproachRadioCall { get; init; }

    /// <summary>FAA NASR field: FREQ. Frequency for SERVICED FACILITY use. In the case of a NAVAID with DME/TACAN Channel, the Frequency is displayed with the Channel (FREQ/CHAN).</summary>
    public string? Frequency { get; init; }

    /// <summary>FAA NASR field: SECTORIZATION. Sectorization based on SERVICED FACILITY or airway boundaries, or limitations based on runway usage. For ARTCC and RCAG, identifies the Frequency Altitude as Low, High, Low/High or Ultra-High.</summary>
    public string? Sectorization { get; init; }

    /// <summary>FAA NASR field: FREQ_USE. Intended use of the frequency (e.g., ATIS, LCL/P (Local/Tower), GND/P (Ground), CD/P (Clearance Delivery), APCH/P (Approach), DEP/P (Departure)).</summary>
    public string? FrequencyUse { get; init; }

    /// <summary>FAA NASR field: REMARK. Remark Text (Free Form Text that further describes a specific Information Item).</summary>
    public string? Remark { get; init; }
}
