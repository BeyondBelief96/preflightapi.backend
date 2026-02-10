namespace PreflightApi.Domain.ValueObjects.Pireps
{
    /// <summary>
    /// Quality control flags indicating potential data issues with a PIREP.
    /// </summary>
    public class PirepQualityControlFlags
    {
        /// <summary>The report location was assumed to be the midpoint of the route.</summary>
        public string? MidPointAssumed { get; set; }
        /// <summary>The report had no timestamp and was assigned one by the system.</summary>
        public string? NoTimeStamp { get; set; }
        /// <summary>The flight level was reported as a range rather than a single altitude.</summary>
        public string? FltLvlRange { get; set; }
        /// <summary>The altitude was indicated as AGL rather than the standard MSL.</summary>
        public string? AboveGroundLevelIndicated { get; set; }
        /// <summary>No flight level was reported.</summary>
        public string? NoFltLvl { get; set; }
        /// <summary>The reported location could not be reliably decoded.</summary>
        public string? BadLocation { get; set; }
    }
}
