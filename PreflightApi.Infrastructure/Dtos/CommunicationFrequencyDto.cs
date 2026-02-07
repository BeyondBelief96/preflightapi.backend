namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// Airport communication frequency data from the FAA NASR database.
/// </summary>
public record CommunicationFrequencyDto()
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; init; }
    /// <summary>FAA facility code.</summary>
    public string? FacilityCode { get; init; }
    /// <summary>Date the frequency record became effective.</summary>
    public DateTime EffectiveDate { get; init; }
    /// <summary>Name of the facility.</summary>
    public string? FacilityName { get; init; }
    /// <summary>Type of facility (e.g., ATCT, TRACON).</summary>
    public string FacilityType { get; init; } = string.Empty;
    /// <summary>Associated ARTCC or FSS identifier.</summary>
    public string? ArtccOrFssId { get; init; }
    /// <summary>Controller-Pilot Data Link Communications information.</summary>
    public string? Cpdlc { get; init; }
    /// <summary>Tower operating hours.</summary>
    public string? TowerHours { get; init; }
    /// <summary>FAA identifier of the serviced facility.</summary>
    public string ServicedFacility { get; init; } = string.Empty;
    /// <summary>Name of the serviced facility.</summary>
    public string? ServicedFacilityName { get; init; }
    /// <summary>Site type of the serviced facility.</summary>
    public string? ServicedSiteType { get; init; }
    /// <summary>Latitude in decimal degrees.</summary>
    public decimal? Latitude { get; init; }
    /// <summary>Longitude in decimal degrees.</summary>
    public decimal? Longitude { get; init; }
    /// <summary>City of the serviced facility.</summary>
    public string? ServicedCity { get; init; }
    /// <summary>State of the serviced facility.</summary>
    public string? ServicedState { get; init; }
    /// <summary>Country of the serviced facility.</summary>
    public string? ServicedCountry { get; init; }
    /// <summary>Tower or communications call sign.</summary>
    public string? TowerOrCommCall { get; init; }
    /// <summary>Primary approach radio call sign.</summary>
    public string? PrimaryApproachRadioCall { get; init; }
    /// <summary>Radio frequency (e.g., 118.700).</summary>
    public string? Frequency { get; init; }
    /// <summary>Sectorization or coverage area description.</summary>
    public string? Sectorization { get; init; }
    /// <summary>Intended use of the frequency (e.g., ATIS, TWR, GND).</summary>
    public string? FrequencyUse { get; init; }
    /// <summary>Additional remarks.</summary>
    public string? Remark { get; init; }
}
