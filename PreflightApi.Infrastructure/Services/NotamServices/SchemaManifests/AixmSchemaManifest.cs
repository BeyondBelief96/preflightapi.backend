using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace PreflightApi.Infrastructure.Services.NotamServices.SchemaManifests;

/// <summary>
/// Represents the expected AIXM XML structure for NMS NOTAM initial load responses.
/// Used for schema drift detection against the FAA NMS AIXM format.
/// </summary>
public class AixmSchemaManifest
{
    [JsonPropertyName("$schema")]
    public string Schema { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("lastVerified")]
    public string LastVerified { get; set; } = string.Empty;

    [JsonPropertyName("contexts")]
    public Dictionary<string, AixmContextDefinition> Contexts { get; set; } = new();
}

/// <summary>
/// Defines a validation context — a specific node in the AIXM XML tree with expected child elements and attributes.
/// </summary>
public class AixmContextDefinition
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("xpath")]
    public string Xpath { get; set; } = string.Empty;

    [JsonPropertyName("parentContext")]
    public string? ParentContext { get; set; }

    [JsonPropertyName("elements")]
    public Dictionary<string, AixmElementDefinition> Elements { get; set; } = new();

    [JsonPropertyName("attributes")]
    public Dictionary<string, AixmElementDefinition> Attributes { get; set; } = new();
}

/// <summary>
/// Defines an expected XML element or attribute in an AIXM context.
/// </summary>
public class AixmElementDefinition
{
    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Result of validating an AIXM XML member against the schema manifest.
/// </summary>
public class AixmSchemaValidationResult
{
    public bool HasDrift => MissingElements.Count > 0 || UnexpectedElements.Count > 0
        || MissingAttributes.Count > 0 || UnexpectedAttributes.Count > 0;

    public List<string> MissingElements { get; init; } = new();
    public List<string> UnexpectedElements { get; init; } = new();
    public List<string> MissingAttributes { get; init; } = new();
    public List<string> UnexpectedAttributes { get; init; } = new();
}

/// <summary>
/// Loads the AIXM schema manifest from embedded resources.
/// </summary>
public static class AixmSchemaManifestLoader
{
    private static readonly Assembly ManifestAssembly = typeof(AixmSchemaManifestLoader).Assembly;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Loads the NOTAM AIXM schema manifest from the embedded resource.
    /// </summary>
    public static AixmSchemaManifest? Load()
    {
        var resourceName = ManifestAssembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.Contains("NotamServices.SchemaManifests.") &&
                                 n.EndsWith("notam_aixm.manifest.json", StringComparison.OrdinalIgnoreCase));

        if (resourceName == null) return null;

        using var stream = ManifestAssembly.GetManifestResourceStream(resourceName);
        if (stream == null) return null;

        return JsonSerializer.Deserialize<AixmSchemaManifest>(stream, JsonOptions);
    }
}

/// <summary>
/// Validates an AIXM XML member element against the schema manifest to detect drift.
/// </summary>
public static class AixmSchemaValidator
{
    /// <summary>
    /// Validates a single AIXMBasicMessage member node against the schema manifest.
    /// Call once per API response with the first member element.
    /// </summary>
    public static AixmSchemaValidationResult ValidateMember(XmlNode member, XmlNamespaceManager nsMgr)
    {
        var result = new AixmSchemaValidationResult();

        var manifest = AixmSchemaManifestLoader.Load();
        if (manifest == null)
            return result;

        // Build a map of resolved nodes for contexts that have parentContext references
        var resolvedNodes = new Dictionary<string, XmlNode>();

        foreach (var (contextName, contextDef) in manifest.Contexts)
        {
            var targetNode = ResolveNode(member, contextDef, resolvedNodes, manifest, nsMgr);
            if (targetNode == null)
                continue;

            resolvedNodes[contextName] = targetNode;

            ValidateContext(contextName, targetNode, contextDef, nsMgr, result);
        }

        return result;
    }

    private static XmlNode? ResolveNode(
        XmlNode member,
        AixmContextDefinition contextDef,
        Dictionary<string, XmlNode> resolvedNodes,
        AixmSchemaManifest manifest,
        XmlNamespaceManager nsMgr)
    {
        if (contextDef.ParentContext != null)
        {
            // Navigate relative to the parent context's resolved node
            if (!resolvedNodes.TryGetValue(contextDef.ParentContext, out var parentNode))
                return null;

            return parentNode.SelectSingleNode(contextDef.Xpath, nsMgr);
        }

        // Navigate from the member root
        return contextDef.Xpath == "."
            ? member
            : member.SelectSingleNode(contextDef.Xpath, nsMgr);
    }

    private static void ValidateContext(
        string contextName,
        XmlNode targetNode,
        AixmContextDefinition contextDef,
        XmlNamespaceManager nsMgr,
        AixmSchemaValidationResult result)
    {
        // Validate child elements
        if (contextDef.Elements.Count > 0)
        {
            var actualElements = GetChildElementNames(targetNode, nsMgr);

            // Missing: only flag required elements that are absent
            foreach (var (name, def) in contextDef.Elements)
            {
                if (def.Required && !actualElements.Contains(name))
                {
                    result.MissingElements.Add($"{contextName}.{name}");
                }
            }

            // Unexpected: elements in actual XML that aren't in the manifest
            var expectedNames = new HashSet<string>(contextDef.Elements.Keys, StringComparer.Ordinal);
            foreach (var actual in actualElements)
            {
                if (!expectedNames.Contains(actual))
                {
                    result.UnexpectedElements.Add($"{contextName}.{actual}");
                }
            }
        }

        // Validate attributes
        if (contextDef.Attributes.Count > 0)
        {
            var actualAttributes = GetAttributeNames(targetNode, nsMgr);

            // Missing: only flag required attributes that are absent
            foreach (var (name, def) in contextDef.Attributes)
            {
                if (def.Required && !actualAttributes.Contains(name))
                {
                    result.MissingAttributes.Add($"{contextName}.{name}");
                }
            }

            // Unexpected: attributes in actual XML that aren't in the manifest
            var expectedAttrNames = new HashSet<string>(contextDef.Attributes.Keys, StringComparer.Ordinal);
            foreach (var actual in actualAttributes)
            {
                if (!expectedAttrNames.Contains(actual))
                {
                    result.UnexpectedAttributes.Add($"{contextName}.{actual}");
                }
            }
        }
    }

    /// <summary>
    /// Gets prefixed child element names (e.g., "event:number", "fnse:classification")
    /// by resolving namespace URIs back to the prefixes registered in the namespace manager.
    /// </summary>
    private static HashSet<string> GetChildElementNames(XmlNode node, XmlNamespaceManager nsMgr)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (XmlNode child in node.ChildNodes)
        {
            if (child.NodeType != XmlNodeType.Element)
                continue;

            var prefixedName = GetPrefixedName(child, nsMgr);
            if (prefixedName != null)
                names.Add(prefixedName);
        }

        return names;
    }

    /// <summary>
    /// Gets prefixed attribute names, filtering out xmlns declarations and xsi attributes.
    /// </summary>
    private static HashSet<string> GetAttributeNames(XmlNode node, XmlNamespaceManager nsMgr)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        if (node.Attributes == null)
            return names;

        foreach (XmlAttribute attr in node.Attributes)
        {
            // Skip namespace declarations and xsi attributes
            if (attr.Prefix == "xmlns" || attr.Name == "xmlns" ||
                attr.NamespaceURI == "http://www.w3.org/2000/xmlns/" ||
                attr.NamespaceURI == "http://www.w3.org/2001/XMLSchema-instance")
                continue;

            var prefixedName = GetPrefixedName(attr, nsMgr);
            if (prefixedName != null)
                names.Add(prefixedName);
        }

        return names;
    }

    /// <summary>
    /// Resolves a node's namespace URI to the manifest's prefix convention.
    /// E.g., a node in "http://www.aixm.aero/schema/5.1/event" namespace with local name "number"
    /// becomes "event:number".
    /// </summary>
    private static string? GetPrefixedName(XmlNode node, XmlNamespaceManager nsMgr)
    {
        if (string.IsNullOrEmpty(node.NamespaceURI))
            return node.LocalName;

        var prefix = nsMgr.LookupPrefix(node.NamespaceURI);
        return prefix != null ? $"{prefix}:{node.LocalName}" : node.LocalName;
    }
}
