using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Pavement Type. Corresponds to FAA NASR field PAVEMENT_TYPE_CODE (APT_RWY).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PavementType
{
    /// <summary>R - Rigid (e.g., Portland Cement Concrete).</summary>
    Rigid,

    /// <summary>F - Flexible (e.g., Asphalt).</summary>
    Flexible
}
