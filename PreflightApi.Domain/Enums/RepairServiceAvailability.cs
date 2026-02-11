using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Repair Service Availability. Corresponds to FAA NASR fields AIRFRAME_REPAIR_SER_CODE and PWR_PLANT_REPAIR_SER (APT_BASE).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RepairServiceAvailability
{
    /// <summary>Repair service availability could not be determined from FAA data.</summary>
    Unknown,

    /// <summary>NONE - No repair service available.</summary>
    None,

    /// <summary>MAJOR - Major repair service available.</summary>
    Major,

    /// <summary>MINOR - Minor repair service available.</summary>
    Minor
}
