using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApproachLightSystemType
{
    Unknown,
    None,
    AirForceOverrun,
    Alsaf,
    Alsf1,
    Alsf2,
    Mals,
    Malsf,
    Malsr,
    Rail,
    Sals,
    Salsf,
    Ssals,
    Ssalf,
    Ssalr,
    Odals,
    Rlls,
    MilitaryOverrun,
    NonStandard
}
