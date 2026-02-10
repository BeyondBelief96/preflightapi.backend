using System.Reflection;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace PreflightApi.API.Configuration;

/// <summary>
/// NSwag document processor that populates the OpenAPI top-level tags array with descriptions
/// from controller XML documentation. Maps each controller's class-level &lt;summary&gt; to the
/// tag description, using the [Tags] attribute value (if present) or the controller name as the tag key.
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
            var summary = members
                .FirstOrDefault(m => m.Attribute("name")?.Value == memberName)?
                .Element("summary")?.Value;

            if (string.IsNullOrWhiteSpace(summary))
                continue;

            // Normalize whitespace (XML docs have indentation artifacts)
            summary = string.Join(" ", summary.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries));
            descriptions[tagName] = summary;
        }

        return descriptions;
    }
}
