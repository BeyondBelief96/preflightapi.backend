using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Survey Method for position or elevation determination. Corresponds to FAA NASR fields SURVEY_METHOD_CODE and ELEV_METHOD_CODE (APT_BASE).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SurveyMethod
{
    /// <summary>Survey method could not be determined from FAA data.</summary>
    Unknown,

    /// <summary>E - Estimated.</summary>
    Estimated,

    /// <summary>S - Surveyed.</summary>
    Surveyed
}
