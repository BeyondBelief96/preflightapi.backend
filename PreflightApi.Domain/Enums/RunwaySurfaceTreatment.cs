using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RunwaySurfaceTreatment
{
    Unknown,
    None,
    Grooved,
    PorousFrictionCourse,
    AggregateFrictionSealCoat,
    RubberizedFrictionSealCoat,
    WireComb
}
