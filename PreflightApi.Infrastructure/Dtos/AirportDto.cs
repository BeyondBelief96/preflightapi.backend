namespace PreflightApi.Infrastructure.Dtos
{
    /// <summary>
    /// Airport data from the FAA NASR database.
    /// </summary>
    public record AirportDto
    {
        /// <summary>FAA site number (unique identifier).</summary>
        public string SiteNo { get; init; } = string.Empty;
        /// <summary>ICAO identifier (e.g., KDFW).</summary>
        public string? IcaoId { get; init; }
        /// <summary>FAA airport identifier (e.g., DFW).</summary>
        public string? ArptId { get; init; }
        /// <summary>Official airport name.</summary>
        public string? ArptName { get; init; }
        /// <summary>Site type code (e.g., A for airport, H for heliport).</summary>
        public string? SiteTypeCode { get; init; }
        /// <summary>City the airport is associated with.</summary>
        public string? City { get; init; }
        /// <summary>Two-letter state code.</summary>
        public string? StateCode { get; init; }
        /// <summary>Country code.</summary>
        public string? CountryCode { get; init; }
        /// <summary>Full state name.</summary>
        public string? StateName { get; init; }
        /// <summary>Latitude in decimal degrees.</summary>
        public decimal? LatDecimal { get; init; }
        /// <summary>Longitude in decimal degrees.</summary>
        public decimal? LongDecimal { get; init; }
        /// <summary>Field elevation in feet MSL.</summary>
        public decimal? Elev { get; init; }
        /// <summary>Magnetic variation in degrees.</summary>
        public decimal? MagVarn { get; init; }
        /// <summary>Magnetic variation hemisphere (E or W).</summary>
        public string? MagHemis { get; init; }
        /// <summary>Sectional chart name.</summary>
        public string? ChartName { get; init; }
        /// <summary>Airport operational status.</summary>
        public string? ArptStatus { get; init; }
        /// <summary>Available fuel types.</summary>
        public string? FuelTypes { get; init; }
        /// <summary>Date of the last FAA inspection.</summary>
        public DateTime? LastInspection { get; init; }
        /// <summary>Date of the last information response.</summary>
        public DateTime? LastInfoResponse { get; init; }
        /// <summary>Customs landing rights flag (Y/N).</summary>
        public string? CustomsFlag { get; init; }
        /// <summary>Landing rights flag (Y/N).</summary>
        public string? LndgRightsFlag { get; init; }
        /// <summary>Joint use (civil/military) flag (Y/N).</summary>
        public string? JointUseFlag { get; init; }
        /// <summary>Military landing rights flag (Y/N).</summary>
        public string? MilLndgFlag { get; init; }
        /// <summary>Airport manager title.</summary>
        public string? ContactTitle { get; init; }
        /// <summary>Airport manager name.</summary>
        public string? ContactName { get; init; }
        /// <summary>Contact address line 1.</summary>
        public string? ContactAddress1 { get; init; }
        /// <summary>Contact address line 2.</summary>
        public string? ContactAddress2 { get; init; }
        /// <summary>Contact city.</summary>
        public string? ContactCity { get; init; }
        /// <summary>Contact state.</summary>
        public string? ContactState { get; init; }
        /// <summary>Contact ZIP code.</summary>
        public string? ContactZipCode { get; init; }
        /// <summary>Contact ZIP+4 code.</summary>
        public string? ContactZipPlusFour { get; init; }
        /// <summary>Airport manager phone number.</summary>
        public string? ContactPhoneNumber { get; init; }
    }
}
