using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CgEnvelopeFormat
{
    /// <summary>
    /// X-axis is CG arm in inches (or centimeters depending on ArmUnits)
    /// </summary>
    Arm,

    /// <summary>
    /// X-axis is Moment/1000 in lb-inches (or kg-cm depending on units)
    /// </summary>
    MomentDividedBy1000
}
