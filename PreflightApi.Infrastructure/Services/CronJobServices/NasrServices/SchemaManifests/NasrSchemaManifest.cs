using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PreflightApi.Infrastructure.Services.CronJobServices.NasrServices.SchemaManifests;

/// <summary>
/// Represents a NASR schema manifest that defines the expected CSV structure
/// for a single FAA NASR data file. Used for schema drift detection.
/// </summary>
public class NasrSchemaManifest
{
    [JsonPropertyName("$schema")]
    public string Schema { get; set; } = string.Empty;

    [JsonPropertyName("csvFile")]
    public string CsvFile { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("entity")]
    public string Entity { get; set; } = string.Empty;

    [JsonPropertyName("supplementaryTo")]
    public string? SupplementaryTo { get; set; }

    [JsonPropertyName("mergeKey")]
    public string? MergeKey { get; set; }

    [JsonPropertyName("lastVerified")]
    public string LastVerified { get; set; } = string.Empty;

    [JsonPropertyName("columns")]
    public Dictionary<string, NasrColumnDefinition> Columns { get; set; } = new();
}

/// <summary>
/// Defines the expected schema for a single CSV column in a NASR data file.
/// </summary>
public class NasrColumnDefinition
{
    [JsonPropertyName("entityProperty")]
    public string EntityProperty { get; set; } = string.Empty;

    [JsonPropertyName("dataType")]
    public string DataType { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("enumeratedValues")]
    public List<string>? EnumeratedValues { get; set; }
}

/// <summary>
/// Loads NASR schema manifests from embedded resources.
/// </summary>
public static class NasrSchemaManifestLoader
{
    private static readonly Assembly ManifestAssembly = typeof(NasrSchemaManifestLoader).Assembly;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Loads all NASR schema manifests from embedded resources.
    /// </summary>
    public static List<NasrSchemaManifest> LoadAll()
    {
        var manifests = new List<NasrSchemaManifest>();
        var resourceNames = ManifestAssembly.GetManifestResourceNames()
            .Where(n => n.EndsWith(".manifest.json", StringComparison.OrdinalIgnoreCase));

        foreach (var resourceName in resourceNames)
        {
            var manifest = Load(resourceName);
            if (manifest != null)
            {
                manifests.Add(manifest);
            }
        }

        return manifests;
    }

    /// <summary>
    /// Loads a single NASR schema manifest by CSV file name (e.g., "APT_BASE.csv").
    /// </summary>
    public static NasrSchemaManifest? LoadByCsvFile(string csvFileName)
    {
        var manifestName = csvFileName.Replace(".csv", "", StringComparison.OrdinalIgnoreCase)
            .ToLowerInvariant()
            .Replace("_", "_");

        var resourceName = ManifestAssembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.Contains($"{manifestName}.manifest.json", StringComparison.OrdinalIgnoreCase));

        return resourceName != null ? Load(resourceName) : null;
    }

    /// <summary>
    /// Gets the expected column names for a given CSV file.
    /// </summary>
    public static HashSet<string> GetExpectedColumns(string csvFileName)
    {
        var manifest = LoadByCsvFile(csvFileName);
        return manifest != null
            ? new HashSet<string>(manifest.Columns.Keys, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>();
    }

    private static NasrSchemaManifest? Load(string resourceName)
    {
        using var stream = ManifestAssembly.GetManifestResourceStream(resourceName);
        if (stream == null) return null;

        return JsonSerializer.Deserialize<NasrSchemaManifest>(stream, JsonOptions);
    }
}

/// <summary>
/// Result of validating CSV headers against a schema manifest.
/// </summary>
public class NasrSchemaValidationResult
{
    public bool HasDrift => MissingColumns.Count > 0 || UnexpectedColumns.Count > 0;
    public string CsvFile { get; init; } = string.Empty;
    public List<string> MissingColumns { get; init; } = new();
    public List<string> UnexpectedColumns { get; init; } = new();
}

/// <summary>
/// Validates actual CSV headers against expected schema manifests to detect drift.
/// </summary>
public static class NasrSchemaValidator
{
    public static NasrSchemaValidationResult ValidateHeaders(string csvFileName, string[] actualHeaders)
    {
        var result = new NasrSchemaValidationResult { CsvFile = csvFileName };

        var manifest = NasrSchemaManifestLoader.LoadByCsvFile(csvFileName);
        if (manifest == null)
            return result;

        var expectedColumns = new HashSet<string>(manifest.Columns.Keys, StringComparer.OrdinalIgnoreCase);
        var actualColumnsSet = new HashSet<string>(actualHeaders, StringComparer.OrdinalIgnoreCase);

        result.MissingColumns.AddRange(expectedColumns.Except(actualColumnsSet, StringComparer.OrdinalIgnoreCase));
        result.UnexpectedColumns.AddRange(actualColumnsSet.Except(expectedColumns, StringComparer.OrdinalIgnoreCase));

        return result;
    }
}
