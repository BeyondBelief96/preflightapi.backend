namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// Communication frequency data from the FAA NASR database, sourced from the FRQ CSV file.
/// Contains radio frequencies for ATC facilities, towers, approach/departure control, and other aviation communication services.
/// </summary>
public record CommunicationFrequencyDto()
{
    /// <summary>System-generated unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>FAA NASR field: FACILITY_CODE. FAA facility identifier code for the communication facility.</summary>
    public string? FacilityCode { get; init; }

    /// <summary>FAA NASR field: EFF_DATE. Effective date of the frequency record.</summary>
    public DateTime EffectiveDate { get; init; }

    /// <summary>FAA NASR field: FACILITY_NAME. Name of the communication facility.</summary>
    public string? FacilityName { get; init; }

    /// <summary>FAA NASR field: FACILITY_TYPE. Type of facility (e.g., ATCT, TRACON, ARTCC, FSS, CTAF).</summary>
    public string FacilityType { get; init; } = string.Empty;

    /// <summary>FAA NASR field: ARTCC_OR_FSS_ID. Associated Air Route Traffic Control Center (ARTCC) or Flight Service Station (FSS) identifier.</summary>
    public string? ArtccOrFssId { get; init; }

    /// <summary>FAA NASR field: CPDLC. Controller-Pilot Data Link Communications (CPDLC) information.</summary>
    public string? Cpdlc { get; init; }

    /// <summary>FAA NASR field: TOWER_HOURS. Tower operating hours (e.g., "0600-2200", "24 HRS", "SS-SR").</summary>
    public string? TowerHours { get; init; }

    /// <summary>FAA NASR field: SERVICED_FACILITY. FAA identifier of the facility being serviced by this frequency.</summary>
    public string ServicedFacility { get; init; } = string.Empty;

    /// <summary>FAA NASR field: SERVICED_FACILITY_NAME. Name of the facility being serviced.</summary>
    public string? ServicedFacilityName { get; init; }

    /// <summary>FAA NASR field: SERVICED_SITE_TYPE. Site type of the facility being serviced (e.g., AIRPORT, HELIPORT).</summary>
    public string? ServicedSiteType { get; init; }

    /// <summary>FAA NASR field: LATITUDE. Latitude of the serviced facility in decimal degrees.</summary>
    public decimal? Latitude { get; init; }

    /// <summary>FAA NASR field: LONGITUDE. Longitude of the serviced facility in decimal degrees.</summary>
    public decimal? Longitude { get; init; }

    /// <summary>FAA NASR field: SERVICED_CITY. City of the serviced facility.</summary>
    public string? ServicedCity { get; init; }

    /// <summary>FAA NASR field: SERVICED_STATE. Two-letter state code of the serviced facility.</summary>
    public string? ServicedState { get; init; }

    /// <summary>FAA NASR field: SERVICED_COUNTRY. Two-letter country code of the serviced facility.</summary>
    public string? ServicedCountry { get; init; }

    /// <summary>FAA NASR field: TOWER_OR_COMM_CALL. Tower or communications call sign (e.g., "DALLAS TOWER", "SOCAL APPROACH").</summary>
    public string? TowerOrCommCall { get; init; }

    /// <summary>FAA NASR field: PRIMARY_APPROACH_RADIO_CALL. Primary approach control radio call sign.</summary>
    public string? PrimaryApproachRadioCall { get; init; }

    /// <summary>FAA NASR field: FREQUENCY. Radio frequency in MHz (e.g., "118.700", "121.900").</summary>
    public string? Frequency { get; init; }

    /// <summary>FAA NASR field: SECTORIZATION. Sectorization or coverage area description for the frequency.</summary>
    public string? Sectorization { get; init; }

    /// <summary>FAA NASR field: FREQUENCY_USE. Intended use of the frequency (e.g., ATIS, LCL/P (Local/Tower), GND/P (Ground), CD/P (Clearance Delivery), APCH/P (Approach), DEP/P (Departure)).</summary>
    public string? FrequencyUse { get; init; }

    /// <summary>FAA NASR field: REMARK. Free-form remark text providing additional information about the frequency.</summary>
    public string? Remark { get; init; }
}
