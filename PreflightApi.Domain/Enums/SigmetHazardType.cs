using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// SIGMET hazard types
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SigmetHazardType
{
    /// <summary>
    /// Convective activity (thunderstorms)
    /// </summary>
    CONVECTIVE,

    /// <summary>
    /// Icing conditions
    /// </summary>
    ICE,

    /// <summary>
    /// Turbulence
    /// </summary>
    TURB,

    /// <summary>
    /// Instrument Flight Rules conditions (low visibility/ceiling)
    /// </summary>
    IFR,

    /// <summary>
    /// Mountain obscuration
    /// </summary>
    MTN_OBSCN
}
