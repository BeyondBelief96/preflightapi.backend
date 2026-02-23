using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace PreflightApi.Infrastructure.Services.CronJobServices.FaaDocServices.SchemaManifests;

/// <summary>
/// Represents an FAA document XML schema manifest that defines the expected XML structure
/// for FAA publication data (d-TPP terminal procedures, chart supplements).
/// Used for schema drift detection against FAA XML metadata files.
/// </summary>
public class FaaDocSchemaManifest
{
    [JsonPropertyName("$schema")]
    public string Schema { get; set; } = string.Empty;

    [JsonPropertyName("documentType")]
    public string DocumentType { get; set; } = string.Empty;

    [JsonPropertyName("entity")]
    public string Entity { get; set; } = string.Empty;

    [JsonPropertyName("lastVerified")]
    public string LastVerified { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The parent container element name (e.g., "airport_name" for d-TPP, "location" for chart supplements).
    /// </summary>
    [JsonPropertyName("containerElement")]
    public string ContainerElement { get; set; } = string.Empty;

    /// <summary>
    /// Expected attributes on the container element (e.g., ID, icao_ident on airport_name).
    /// Null if the container has no attributes to validate.
    /// </summary>
    [JsonPropertyName("containerAttributes")]
    public Dictionary<string, FaaDocFieldDefinition>? ContainerAttributes { get; set; }

    /// <summary>
    /// The record element name within the container (e.g., "record" for d-TPP, "airport" for chart supplements).
    /// </summary>
    [JsonPropertyName("recordElement")]
    public string RecordElement { get; set; } = string.Empty;

    /// <summary>
    /// Expected direct child elements of the record element.
    /// </summary>
    [JsonPropertyName("recordElements")]
    public Dictionary<string, FaaDocFieldDefinition> RecordElements { get; set; } = new();

    /// <summary>
    /// Nested elements within the record that contain their own child elements
    /// (e.g., pages/pdf in chart supplements).
    /// </summary>
    [JsonPropertyName("nestedElements")]
    public Dictionary<string, FaaDocNestedElementDefinition>? NestedElements { get; set; }
}

/// <summary>
/// Defines the expected schema for a single XML field (element or attribute) in an FAA document.
/// </summary>
public class FaaDocFieldDefinition
{
    [JsonPropertyName("entityProperty")]
    public string? EntityProperty { get; set; }

    [JsonPropertyName("dataType")]
    public string DataType { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("captured")]
    public bool Captured { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Defines a nested XML element that contains its own child elements
/// (e.g., the pages element containing pdf children in chart supplements).
/// </summary>
public class FaaDocNestedElementDefinition
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("elements")]
    public Dictionary<string, FaaDocFieldDefinition>? Elements { get; set; }
}

/// <summary>
/// Result of validating an XML document against its FAA document schema manifest.
/// </summary>
public class FaaDocSchemaValidationResult
{
    public bool HasDrift => MissingElements.Count > 0 || UnexpectedElements.Count > 0
        || MissingAttributes.Count > 0 || UnexpectedAttributes.Count > 0;

    public string DocumentType { get; init; } = string.Empty;
    public List<string> MissingElements { get; init; } = new();
    public List<string> UnexpectedElements { get; init; } = new();
    public List<string> MissingAttributes { get; init; } = new();
    public List<string> UnexpectedAttributes { get; init; } = new();
}

/// <summary>
/// Loads FAA document schema manifests from embedded resources.
/// </summary>
public static class FaaDocSchemaManifestLoader
{
    private static readonly Assembly ManifestAssembly = typeof(FaaDocSchemaManifestLoader).Assembly;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Loads a single FAA document schema manifest by document type name
    /// (e.g., "dtpp", "chartsupplement").
    /// </summary>
    public static FaaDocSchemaManifest? LoadByDocumentType(string documentType)
    {
        var resourceName = ManifestAssembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.Contains("FaaDocServices.SchemaManifests.") &&
                                 n.Contains($"{documentType.ToLowerInvariant()}.manifest.json",
                                     StringComparison.OrdinalIgnoreCase));

        return resourceName != null ? Load(resourceName) : null;
    }

    private static FaaDocSchemaManifest? Load(string resourceName)
    {
        using var stream = ManifestAssembly.GetManifestResourceStream(resourceName);
        if (stream == null) return null;

        return JsonSerializer.Deserialize<FaaDocSchemaManifest>(stream, JsonOptions);
    }
}

/// <summary>
/// Validates actual XML documents against expected FAA document schema manifests to detect drift.
/// </summary>
public static class FaaDocSchemaValidator
{
    /// <summary>
    /// Validates an XML document against the manifest for the given document type.
    /// Checks container element attributes and first record element children.
    /// Call once per processing cycle with the parsed XDocument.
    /// </summary>
    public static FaaDocSchemaValidationResult Validate(string documentType, XDocument doc)
    {
        var result = new FaaDocSchemaValidationResult { DocumentType = documentType };

        var manifest = FaaDocSchemaManifestLoader.LoadByDocumentType(documentType);
        if (manifest == null)
            return result;

        // Find the first container element
        var container = doc.Descendants(manifest.ContainerElement).FirstOrDefault();
        if (container == null)
            return result;

        // Validate container attributes (e.g., airport_name/@ID, @icao_ident, @apt_ident)
        if (manifest.ContainerAttributes is { Count: > 0 })
        {
            var actualAttributes = new HashSet<string>(
                container.Attributes().Select(a => a.Name.LocalName),
                StringComparer.OrdinalIgnoreCase);

            var expectedAttributes = new HashSet<string>(
                manifest.ContainerAttributes.Keys, StringComparer.OrdinalIgnoreCase);

            // Only flag required attributes as missing
            var requiredAttributes = manifest.ContainerAttributes
                .Where(a => a.Value.Required)
                .Select(a => a.Key);
            result.MissingAttributes.AddRange(
                requiredAttributes.Where(a => !actualAttributes.Contains(a)));

            result.UnexpectedAttributes.AddRange(
                actualAttributes.Except(expectedAttributes, StringComparer.OrdinalIgnoreCase));
        }

        // Find the first record element within the container
        var record = container.Elements(manifest.RecordElement).FirstOrDefault();
        if (record == null)
            return result;

        // All known elements = record elements + nested element names
        var allKnownElements = new HashSet<string>(manifest.RecordElements.Keys, StringComparer.OrdinalIgnoreCase);
        if (manifest.NestedElements != null)
        {
            foreach (var nested in manifest.NestedElements.Keys)
                allKnownElements.Add(nested);
        }

        // Get actual child elements of the record
        var actualElements = new HashSet<string>(
            record.Elements().Select(e => e.Name.LocalName),
            StringComparer.OrdinalIgnoreCase);

        // Missing: only flag required record elements that are absent
        var requiredElements = manifest.RecordElements
            .Where(e => e.Value.Required)
            .Select(e => e.Key);
        result.MissingElements.AddRange(
            requiredElements.Where(e => !actualElements.Contains(e)));

        // Unexpected: in actual XML but not in any manifest definition
        result.UnexpectedElements.AddRange(
            actualElements.Except(allKnownElements, StringComparer.OrdinalIgnoreCase));

        // Validate nested elements and their children
        if (manifest.NestedElements != null)
        {
            foreach (var (nestedName, nestedDef) in manifest.NestedElements)
            {
                var nestedElement = record.Elements(nestedName).FirstOrDefault();
                if (nestedElement == null)
                    continue;

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
                        requiredChildElements.Where(e => !actualChildElements.Contains(e))
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
