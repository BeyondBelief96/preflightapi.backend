using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VerificationStatus
{
    Unknown,
    Verified,
    Unverified
}
