using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LoadingGraphFormat
{
    /// <summary>
    /// Loading graph X-axis is CG arm in inches (or centimeters depending on ArmUnits)
    /// </summary>
    Arm,

    /// <summary>
    /// Loading graph X-axis is Moment/1000 in lb-inches (or kg-cm depending on units)
    /// </summary>
    MomentDividedBy1000
}
