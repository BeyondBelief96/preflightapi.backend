namespace PreflightApi.Domain.ValueObjects
{
    public class NavaidCheckpoint
    {
        /// <summary>Altitude only when checkpoint is in air.</summary>
        public int? Altitude { get; set; }

        /// <summary>Bearing of checkpoint.</summary>
        public int Bearing { get; set; }

        /// <summary>Air/Ground Code: A=AIR, G=GROUND, G1=GROUND ONE.</summary>
        public string AirGroundCode { get; set; } = string.Empty;

        /// <summary>Narrative description associated with the checkpoint.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Airport ID associated with the checkpoint.</summary>
        public string? AirportId { get; set; }

        /// <summary>State code in which associated city is located.</summary>
        public string StateCode { get; set; } = string.Empty;
    }
}
