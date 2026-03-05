using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Segmented Circle Airport Marker. Corresponds to FAA NASR field SEG_CIRCLE_MKR_FLAG (APT_BASE).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SegmentedCircleMarkerType
{
    /// <summary>N - No segmented circle marker.</summary>
    None,

    /// <summary>Y - Segmented circle marker exists.</summary>
    Yes,

    /// <summary>Y-L - Segmented circle marker exists and is lighted.</summary>
    YesLighted
}
