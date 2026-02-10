namespace PreflightApi.Infrastructure.Dtos
{
    /// <summary>
    /// US state identification data with code and full name.
    /// </summary>
    public record StateInfoDto
    {
        /// <summary>Two-letter US state code (e.g., TX, CA, NY).</summary>
        public string StateCode { get; init; } = string.Empty;
        /// <summary>Full state name (e.g., Texas, California, New York).</summary>
        public string StateName { get; init; } = string.Empty;
    }
}
