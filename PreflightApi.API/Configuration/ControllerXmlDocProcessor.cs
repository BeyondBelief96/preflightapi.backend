using System.Reflection;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace PreflightApi.API.Configuration;

/// <summary>
/// NSwag document processor that populates the OpenAPI top-level tags array with descriptions
/// from controller XML documentation. Maps each controller's class-level &lt;summary&gt; and
/// &lt;remarks&gt; to the tag description (as markdown), using the [Tags] attribute value
/// (if present) or the controller name as the tag key.
/// </summary>
public class ControllerXmlDocProcessor : IDocumentProcessor
{
    private readonly Dictionary<string, string> _tagDescriptions;

    public ControllerXmlDocProcessor()
    {
        _tagDescriptions = LoadControllerXmlDescriptions();
    }

    public void Process(DocumentProcessorContext context)
    {
        // Collect all tag names used across operations
        var usedTagNames = context.Document.Operations
            .SelectMany(op => op.Operation.Tags)
            .Distinct()
            .ToList();

        foreach (var tagName in usedTagNames)
        {
            if (!_tagDescriptions.TryGetValue(tagName, out var description))
                continue;

            // Find existing tag or create a new one
            var existingTag = context.Document.Tags.FirstOrDefault(t => t.Name == tagName);
            if (existingTag != null)
            {
                existingTag.Description = description;
            }
            else
            {
                context.Document.Tags.Add(new OpenApiTag { Name = tagName, Description = description });
            }
        }
    }

    private static Dictionary<string, string> LoadControllerXmlDescriptions()
    {
        var descriptions = new Dictionary<string, string>();
        var assembly = typeof(ControllerXmlDocProcessor).Assembly;
        var xmlFile = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");

        if (!File.Exists(xmlFile))
            return descriptions;

        var xml = XDocument.Load(xmlFile);
        var members = xml.Descendants("member");

        var controllerTypes = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ControllerBase)) && !t.IsAbstract);

        foreach (var type in controllerTypes)
        {
            var tagsAttr = type.GetCustomAttribute<TagsAttribute>();
            var tagName = tagsAttr?.Tags.FirstOrDefault()
                          ?? type.Name.Replace("Controller", "");

            var memberName = $"T:{type.FullName}";
            var member = members.FirstOrDefault(m => m.Attribute("name")?.Value == memberName);
            if (member == null)
                continue;

            var summaryElement = member.Element("summary");
            var remarksElement = member.Element("remarks");

            if (summaryElement == null && remarksElement == null)
                continue;

            var parts = new List<string>();

            if (summaryElement != null)
            {
                var summaryText = ConvertXmlDocToMarkdown(summaryElement);
                if (!string.IsNullOrWhiteSpace(summaryText))
                    parts.Add(summaryText);
            }

            if (remarksElement != null)
            {
                var remarksText = ConvertXmlDocToMarkdown(remarksElement);
                if (!string.IsNullOrWhiteSpace(remarksText))
                    parts.Add(remarksText);
            }

            if (parts.Count > 0)
                descriptions[tagName] = string.Join("\n\n", parts);
        }

        return descriptions;
    }

    /// <summary>
    /// Converts an XML documentation element to markdown, handling inline elements
    /// like &lt;para&gt;, &lt;b&gt;/&lt;strong&gt;, &lt;i&gt;/&lt;em&gt;, &lt;c&gt;, &lt;code&gt;,
    /// &lt;see&gt;, &lt;seealso&gt;, and &lt;list&gt; with &lt;item&gt;/&lt;term&gt;/&lt;description&gt;.
    /// </summary>
    private static string ConvertXmlDocToMarkdown(XElement element)
    {
        var result = ConvertNodes(element.Nodes());
        // Normalize whitespace within each paragraph, preserving paragraph breaks
        var paragraphs = result.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var normalized = paragraphs.Select(p =>
            string.Join(" ", p.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries)));
        return string.Join("\n\n", normalized);
    }

    private static string ConvertNodes(IEnumerable<XNode> nodes)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var node in nodes)
        {
            switch (node)
            {
                case XText text:
                    sb.Append(text.Value);
                    break;
                case XElement el:
                    switch (el.Name.LocalName)
                    {
                        case "para":
                            sb.Append("\n\n");
                            sb.Append(ConvertNodes(el.Nodes()));
                            sb.Append("\n\n");
                            break;
                        case "b":
                        case "strong":
                            sb.Append("**");
                            sb.Append(ConvertNodes(el.Nodes()));
                            sb.Append("**");
                            break;
                        case "i":
                        case "em":
                            sb.Append('*');
                            sb.Append(ConvertNodes(el.Nodes()));
                            sb.Append('*');
                            break;
                        case "c":
                            sb.Append('`');
                            sb.Append(el.Value);
                            sb.Append('`');
                            break;
                        case "code":
                            sb.Append("\n\n```\n");
                            sb.Append(el.Value.Trim());
                            sb.Append("\n```\n\n");
                            break;
                        case "see":
                        case "seealso":
                            var cref = el.Attribute("cref")?.Value;
                            if (cref != null)
                            {
                                // Strip prefix (T:, M:, P:, F:) and namespace
                                var name = cref.Contains(':') ? cref[(cref.IndexOf(':') + 1)..] : cref;
                                var shortName = name.Contains('.') ? name[(name.LastIndexOf('.') + 1)..] : name;
                                sb.Append('`');
                                sb.Append(shortName);
                                sb.Append('`');
                            }
                            else
                            {
                                sb.Append(el.Value);
                            }
                            break;
                        case "list":
                            sb.Append("\n\n");
                            foreach (var item in el.Elements("item"))
                            {
                                var term = item.Element("term");
                                var desc = item.Element("description");
                                sb.Append("\n\n- ");
                                if (term != null)
                                {
                                    sb.Append("**");
                                    sb.Append(ConvertNodes(term.Nodes()));
                                    sb.Append("**");
                                    if (desc != null)
                                    {
                                        sb.Append(" — ");
                                        sb.Append(ConvertNodes(desc.Nodes()));
                                    }
                                }
                                else if (desc != null)
                                {
                                    sb.Append(ConvertNodes(desc.Nodes()));
                                }
                                else
                                {
                                    sb.Append(ConvertNodes(item.Nodes()));
                                }
                            }
                            sb.Append("\n\n");
                            break;
                        default:
                            // Unknown element — just include its inner content
                            sb.Append(ConvertNodes(el.Nodes()));
                            break;
                    }
                    break;
            }
        }
        return sb.ToString();
    }
}
