using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace PreflightApi.Infrastructure.Services.CronJobServices.WeatherServices.SchemaManifests;

/// <summary>
/// Represents an aviation weather XML schema manifest that defines the expected XML structure
/// for a single weather data type. Used for schema drift detection against aviationweather.gov cache files.
/// </summary>
public class AvWxSchemaManifest
{
    [JsonPropertyName("$schema")]
    public string Schema { get; set; } = string.Empty;

    [JsonPropertyName("xmlRootElement")]
    public string XmlRootElement { get; set; } = string.Empty;

    [JsonPropertyName("entity")]
    public string Entity { get; set; } = string.Empty;

    [JsonPropertyName("lastVerified")]
    public string LastVerified { get; set; } = string.Empty;

    [JsonPropertyName("elements")]
    public Dictionary<string, AvWxElementDefinition> Elements { get; set; } = new();

    [JsonPropertyName("nestedElements")]
    public Dictionary<string, AvWxNestedElementDefinition>? NestedElements { get; set; }
}

/// <summary>
/// Defines the expected schema for a single XML child element in a weather data record.
/// </summary>
public class AvWxElementDefinition
{
    [JsonPropertyName("entityProperty")]
    public string EntityProperty { get; set; } = string.Empty;

    [JsonPropertyName("dataType")]
    public string DataType { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("openApiProperty")]
    public string? OpenApiProperty { get; set; }

    [JsonPropertyName("captured")]
    public bool Captured { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Defines a nested XML element that contains child elements or uses attributes
/// (e.g., quality_control_flags, sky_condition, forecast).
/// </summary>
public class AvWxNestedElementDefinition
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("elements")]
    public Dictionary<string, AvWxElementDefinition>? Elements { get; set; }

    [JsonPropertyName("attributes")]
    public Dictionary<string, AvWxElementDefinition>? Attributes { get; set; }
}

/// <summary>
/// Result of validating an XML element against its schema manifest.
/// </summary>
public class AvWxSchemaValidationResult
{
    public bool HasDrift => MissingElements.Count > 0 || UnexpectedElements.Count > 0
        || MissingAttributes.Count > 0 || UnexpectedAttributes.Count > 0;

    public string WeatherType { get; init; } = string.Empty;
    public List<string> MissingElements { get; init; } = new();
    public List<string> UnexpectedElements { get; init; } = new();
    public List<string> MissingAttributes { get; init; } = new();
    public List<string> UnexpectedAttributes { get; init; } = new();
}

/// <summary>
/// Loads aviation weather schema manifests from embedded resources.
/// </summary>
public static class AvWxSchemaManifestLoader
{
    private static readonly Assembly ManifestAssembly = typeof(AvWxSchemaManifestLoader).Assembly;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Loads all aviation weather schema manifests from embedded resources.
    /// </summary>
    public static List<AvWxSchemaManifest> LoadAll()
    {
        var manifests = new List<AvWxSchemaManifest>();
        var resourceNames = ManifestAssembly.GetManifestResourceNames()
            .Where(n => n.Contains("WeatherServices.SchemaManifests.") &&
                        n.EndsWith(".manifest.json", StringComparison.OrdinalIgnoreCase));

        foreach (var resourceName in resourceNames)
        {
            var manifest = Load(resourceName);
            if (manifest != null)
                manifests.Add(manifest);
        }

        return manifests;
    }

    /// <summary>
    /// Loads a single aviation weather schema manifest by weather type name
    /// (e.g., "metar", "taf", "pirep", "airsigmet", "gairmet").
    /// </summary>
    public static AvWxSchemaManifest? LoadByWeatherType(string weatherType)
    {
        var resourceName = ManifestAssembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.Contains("WeatherServices.SchemaManifests.") &&
                                 n.Contains($"{weatherType.ToLowerInvariant()}.manifest.json",
                                     StringComparison.OrdinalIgnoreCase));

        return resourceName != null ? Load(resourceName) : null;
    }

    private static AvWxSchemaManifest? Load(string resourceName)
    {
        using var stream = ManifestAssembly.GetManifestResourceStream(resourceName);
        if (stream == null) return null;

        return JsonSerializer.Deserialize<AvWxSchemaManifest>(stream, JsonOptions);
    }
}

/// <summary>
/// Validates actual XML elements against expected schema manifests to detect drift.
/// </summary>
public static class AvWxSchemaValidator
{
    /// <summary>
    /// Validates an XML element's children and nested element attributes against the manifest
    /// for the given weather type. Call once per poll cycle with the first record element.
    /// </summary>
    public static AvWxSchemaValidationResult ValidateElement(string weatherType, XElement element)
    {
        var result = new AvWxSchemaValidationResult { WeatherType = weatherType };

        var manifest = AvWxSchemaManifestLoader.LoadByWeatherType(weatherType);
        if (manifest == null)
            return result;

        // Get actual child element names (direct children only, deduplicated)
        var actualElements = new HashSet<string>(
            element.Elements().Select(e => e.Name.LocalName),
            StringComparer.OrdinalIgnoreCase);

        // All known elements = top-level elements + nested element names
        var allKnownElements = new HashSet<string>(manifest.Elements.Keys, StringComparer.OrdinalIgnoreCase);
        if (manifest.NestedElements != null)
        {
            foreach (var nested in manifest.NestedElements.Keys)
                allKnownElements.Add(nested);
        }

        // Missing: only flag required top-level elements that are absent.
        // In XML, optional elements simply don't appear when they have no data
        // (unlike CSV where all headers are always present).
        var requiredElements = manifest.Elements
            .Where(e => e.Value.Required)
            .Select(e => e.Key);
        result.MissingElements.AddRange(
            requiredElements.Where(e => !actualElements.Contains(e, StringComparer.OrdinalIgnoreCase)));

        // Unexpected: in actual XML but not in any manifest definition
        result.UnexpectedElements.AddRange(
            actualElements.Except(allKnownElements, StringComparer.OrdinalIgnoreCase));

        // Validate nested element attributes and child elements
        if (manifest.NestedElements != null)
        {
            foreach (var (nestedName, nestedDef) in manifest.NestedElements)
            {
                // Find the first instance of this nested element in the actual XML
                var nestedElement = element.Elements(nestedName).FirstOrDefault();
                if (nestedElement == null)
                    continue;

                // Validate attributes (e.g., sky_condition/@sky_cover)
                if (nestedDef.Attributes is { Count: > 0 })
                {
                    var actualAttributes = new HashSet<string>(
                        nestedElement.Attributes().Select(a => a.Name.LocalName),
                        StringComparer.OrdinalIgnoreCase);

                    var expectedAttributes = new HashSet<string>(
                        nestedDef.Attributes.Keys, StringComparer.OrdinalIgnoreCase);

                    // Only flag required attributes as missing
                    var requiredAttributes = nestedDef.Attributes
                        .Where(a => a.Value.Required)
                        .Select(a => a.Key);
                    result.MissingAttributes.AddRange(
                        requiredAttributes.Where(a => !actualAttributes.Contains(a, StringComparer.OrdinalIgnoreCase))
                            .Select(a => $"{nestedName}.{a}"));

                    result.UnexpectedAttributes.AddRange(
                        actualAttributes.Except(expectedAttributes, StringComparer.OrdinalIgnoreCase)
                            .Select(a => $"{nestedName}.{a}"));
                }

                // Validate child elements (e.g., quality_control_flags/corrected)
                if (nestedDef.Elements is { Count: > 0 })
                {
                    var actualChildElements = new HashSet<string>(
                        nestedElement.Elements().Select(e => e.Name.LocalName),
                        StringComparer.OrdinalIgnoreCase);

                    var expectedChildElements = new HashSet<string>(
                        nestedDef.Elements.Keys, StringComparer.OrdinalIgnoreCase);

                    // Only flag required child elements as missing
                    var requiredChildElements = nestedDef.Elements
                        .Where(e => e.Value.Required)
                        .Select(e => e.Key);
                    result.MissingElements.AddRange(
                        requiredChildElements.Where(e => !actualChildElements.Contains(e, StringComparer.OrdinalIgnoreCase))
                            .Select(e => $"{nestedName}.{e}"));

                    result.UnexpectedElements.AddRange(
                        actualChildElements.Except(expectedChildElements, StringComparer.OrdinalIgnoreCase)
                            .Select(e => $"{nestedName}.{e}"));
                }
            }
        }

        return result;
    }
}
