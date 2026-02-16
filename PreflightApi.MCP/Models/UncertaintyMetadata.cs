using System.Text.Json.Serialization;

namespace PreflightApi.MCP.Models;

/// <summary>
/// Types of uncertainty that can occur during flight planning operations.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UncertaintyType
{
    /// <summary>A required field is missing from the request.</summary>
    MissingRequiredField,
    /// <summary>The input is ambiguous (e.g., multiple airports match a search).</summary>
    AmbiguousInput,
    /// <summary>Weather conditions may affect flight safety.</summary>
    WeatherConcern,
    /// <summary>Fuel margin is lower than recommended minimums.</summary>
    FuelMarginWarning,
    /// <summary>Airspace or NOTAM requires attention.</summary>
    SafetyConcern,
    /// <summary>Data is stale or may be outdated.</summary>
    StaleData,
    /// <summary>A requested resource was not found.</summary>
    NotFound
}

/// <summary>
/// Severity levels for uncertainty items.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UncertaintySeverity
{
    /// <summary>Informational only, no action required.</summary>
    Info,
    /// <summary>May require attention or clarification.</summary>
    Warning,
    /// <summary>Must be resolved before proceeding.</summary>
    Critical
}

/// <summary>
/// Represents a single uncertainty or concern that the AI model should surface to the user.
/// </summary>
public record UncertaintyItem
{
    /// <summary>The type of uncertainty.</summary>
    public required UncertaintyType Type { get; init; }

    /// <summary>Human-readable explanation of the uncertainty.</summary>
    public required string Message { get; init; }

    /// <summary>
    /// Suggested prompt for the AI to ask the user for clarification.
    /// If null, no follow-up question is needed.
    /// </summary>
    public string? SuggestedPrompt { get; init; }

    /// <summary>The severity of this uncertainty.</summary>
    public UncertaintySeverity Severity { get; init; } = UncertaintySeverity.Warning;
}

/// <summary>
/// Standard response wrapper for all MCP tool responses, including uncertainty metadata.
/// </summary>
/// <typeparam name="T">The type of data returned by the tool.</typeparam>
public record McpToolResponse<T>
{
    /// <summary>Whether the operation completed successfully.</summary>
    public bool Success { get; init; }

    /// <summary>The data returned by the tool, if successful.</summary>
    public T? Data { get; init; }

    /// <summary>List of uncertainties or concerns that should be surfaced to the user.</summary>
    public List<UncertaintyItem> Uncertainties { get; init; } = [];

    /// <summary>Optional human-readable summary of the result.</summary>
    public string? Summary { get; init; }

    /// <summary>Error message if the operation failed.</summary>
    public string? Error { get; init; }

    /// <summary>Creates a successful response with data.</summary>
    public static McpToolResponse<T> Ok(T data, string? summary = null, List<UncertaintyItem>? uncertainties = null) =>
        new()
        {
            Success = true,
            Data = data,
            Summary = summary,
            Uncertainties = uncertainties ?? []
        };

    /// <summary>Creates a failed response with an error message.</summary>
    public static McpToolResponse<T> Fail(string error, List<UncertaintyItem>? uncertainties = null) =>
        new()
        {
            Success = false,
            Error = error,
            Uncertainties = uncertainties ?? []
        };
}
