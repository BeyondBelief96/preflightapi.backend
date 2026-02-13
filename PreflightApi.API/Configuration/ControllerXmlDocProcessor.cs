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
                var summaryText = XmlDocMarkdownConverter.ConvertXmlDocToMarkdown(summaryElement);
                if (!string.IsNullOrWhiteSpace(summaryText))
                    parts.Add(summaryText);
            }

            if (remarksElement != null)
            {
                var remarksText = XmlDocMarkdownConverter.ConvertXmlDocToMarkdown(remarksElement);
                if (!string.IsNullOrWhiteSpace(remarksText))
                    parts.Add(remarksText);
            }

            if (parts.Count > 0)
                descriptions[tagName] = string.Join("\n\n", parts);
        }

        return descriptions;
    }
}
