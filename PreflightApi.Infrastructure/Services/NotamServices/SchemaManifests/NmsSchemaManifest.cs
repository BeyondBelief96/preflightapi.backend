using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PreflightApi.Infrastructure.Services.NotamServices.SchemaManifests;

/// <summary>
/// Represents the expected GeoJSON Feature structure for NMS NOTAM responses.
/// Used for schema drift detection against the FAA NMS API.
/// </summary>
public class NmsSchemaManifest
{
    [JsonPropertyName("$schema")]
    public string Schema { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("lastVerified")]
    public string LastVerified { get; set; } = string.Empty;

    [JsonPropertyName("topLevelProperties")]
    public Dictionary<string, NmsPropertyDefinition> TopLevelProperties { get; set; } = new();

    [JsonPropertyName("nestedObjects")]
    public Dictionary<string, NmsNestedObjectDefinition> NestedObjects { get; set; } = new();
}

/// <summary>
/// Defines an expected JSON property in the GeoJSON Feature.
/// </summary>
public class NmsPropertyDefinition
{
    [JsonPropertyName("dataType")]
    public string DataType { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Defines a nested JSON object with its expected child properties.
/// </summary>
public class NmsNestedObjectDefinition
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("properties")]
    public Dictionary<string, NmsPropertyDefinition> Properties { get; set; } = new();
}

/// <summary>
/// Result of validating a GeoJSON Feature against the NMS schema manifest.
/// </summary>
public class NmsSchemaValidationResult
{
    public bool HasDrift => MissingProperties.Count > 0 || UnexpectedProperties.Count > 0;

    public List<string> MissingProperties { get; init; } = new();
    public List<string> UnexpectedProperties { get; init; } = new();
}

/// <summary>
/// Loads the NMS GeoJSON schema manifest from embedded resources.
/// </summary>
public static class NmsSchemaManifestLoader
{
    private static readonly Assembly ManifestAssembly = typeof(NmsSchemaManifestLoader).Assembly;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Loads the NOTAM GeoJSON schema manifest from the embedded resource.
    /// </summary>
    public static NmsSchemaManifest? Load()
    {
        var resourceName = ManifestAssembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.Contains("NotamServices.SchemaManifests.") &&
                                 n.EndsWith("notam_geojson.manifest.json", StringComparison.OrdinalIgnoreCase));

        if (resourceName == null) return null;

        using var stream = ManifestAssembly.GetManifestResourceStream(resourceName);
        if (stream == null) return null;

        return JsonSerializer.Deserialize<NmsSchemaManifest>(stream, JsonOptions);
    }
}

/// <summary>
/// Validates a GeoJSON Feature element against the NMS schema manifest to detect drift.
/// </summary>
public static class NmsSchemaValidator
{
    /// <summary>
    /// Validates a single GeoJSON Feature from the NMS API response against the schema manifest.
    /// Call once per API response with the first feature element.
    /// </summary>
    public static NmsSchemaValidationResult ValidateFeature(JsonElement feature)
    {
        var result = new NmsSchemaValidationResult();

        var manifest = NmsSchemaManifestLoader.Load();
        if (manifest == null)
            return result;

        // Validate top-level properties
        ValidateProperties(feature, manifest.TopLevelProperties, "", result);

        // Validate nested objects by navigating dot-paths
        foreach (var (path, nestedDef) in manifest.NestedObjects)
        {
            var target = NavigateToElement(feature, path);
            if (target == null)
                continue;

            // Handle array paths (e.g., "properties.coreNOTAMData.notamTranslation[]")
            if (path.EndsWith("[]"))
            {
                if (target.Value.ValueKind == JsonValueKind.Array)
                {
                    var firstItem = target.Value.EnumerateArray().FirstOrDefault();
                    if (firstItem.ValueKind == JsonValueKind.Object)
                    {
                        var displayPath = path.TrimEnd('[', ']');
                        ValidateProperties(firstItem, nestedDef.Properties, displayPath, result);
                    }
                }
            }
            else
            {
                if (target.Value.ValueKind == JsonValueKind.Object)
                {
                    ValidateProperties(target.Value, nestedDef.Properties, path, result);
                }
            }
        }

        return result;
    }

    private static void ValidateProperties(
        JsonElement element,
        Dictionary<string, NmsPropertyDefinition> expectedProperties,
        string parentPath,
        NmsSchemaValidationResult result)
    {
        var prefix = string.IsNullOrEmpty(parentPath) ? "" : $"{parentPath}.";

        // Get actual property names
        var actualProperties = new HashSet<string>(StringComparer.Ordinal);
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                actualProperties.Add(prop.Name);
            }
        }

        // Missing: only flag required properties that are absent
        foreach (var (name, def) in expectedProperties)
        {
            if (def.Required && !actualProperties.Contains(name))
            {
                result.MissingProperties.Add($"{prefix}{name}");
            }
        }

        // Unexpected: properties in actual JSON that aren't in the manifest
        var expectedNames = new HashSet<string>(expectedProperties.Keys, StringComparer.Ordinal);
        foreach (var actual in actualProperties)
        {
            if (!expectedNames.Contains(actual))
            {
                result.UnexpectedProperties.Add($"{prefix}{actual}");
            }
        }
    }

    private static JsonElement? NavigateToElement(JsonElement root, string dotPath)
    {
        // Remove array suffix for navigation (e.g., "foo.bar[]" -> navigate to "foo.bar")
        var cleanPath = dotPath.TrimEnd('[', ']');
        var segments = cleanPath.Split('.');
        var current = root;

        foreach (var segment in segments)
        {
            if (current.ValueKind != JsonValueKind.Object)
                return null;

            if (!current.TryGetProperty(segment, out var next))
                return null;

            current = next;
        }

        return current;
    }
}
