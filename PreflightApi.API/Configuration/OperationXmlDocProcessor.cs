using System.Reflection;
using System.Xml.Linq;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace PreflightApi.API.Configuration;

/// <summary>
/// NSwag operation processor that converts XML doc <c>&lt;remarks&gt;</c> on controller methods
/// into markdown and sets the OpenAPI operation <c>description</c> field. Runs after NSwag's
/// built-in processor, so the <c>summary</c> (from <c>&lt;summary&gt;</c>) is already populated
/// as plain text. This adds the rich formatted description alongside it.
/// </summary>
public class OperationXmlDocProcessor : IOperationProcessor
{
    private readonly Dictionary<string, XElement>? _memberLookup;

    public OperationXmlDocProcessor()
    {
        _memberLookup = LoadXmlDoc();
    }

    public bool Process(OperationProcessorContext context)
    {
        if (_memberLookup == null)
            return true;

        var memberName = GetXmlMemberName(context.MethodInfo);
        if (!_memberLookup.TryGetValue(memberName, out var member))
            return true;

        var remarks = member.Element("remarks");
        if (remarks == null)
            return true;

        var markdown = XmlDocMarkdownConverter.ConvertXmlDocToMarkdown(remarks);
        if (!string.IsNullOrWhiteSpace(markdown))
            context.OperationDescription.Operation.Description = markdown;

        return true;
    }

    private static string GetXmlMemberName(MethodInfo method)
    {
        var declaringType = method.DeclaringType!;
        var typeName = declaringType.FullName!.Replace('+', '.');

        var parameters = method.GetParameters();
        if (parameters.Length == 0)
            return $"M:{typeName}.{method.Name}";

        var paramTypes = string.Join(",", parameters.Select(p => GetXmlTypeName(p.ParameterType)));
        return $"M:{typeName}.{method.Name}({paramTypes})";
    }

    private static string GetXmlTypeName(Type type)
    {
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition().FullName!;
            // XML doc uses `N for generic arity, e.g. System.Nullable`1
            var backtickIndex = genericDef.IndexOf('`');
            var baseName = backtickIndex >= 0 ? genericDef[..backtickIndex] : genericDef;
            var args = string.Join(",", type.GetGenericArguments().Select(GetXmlTypeName));
            return $"{baseName}{{{args}}}";
        }

        if (type.IsArray)
            return GetXmlTypeName(type.GetElementType()!) + "[]";

        return type.FullName ?? type.Name;
    }

    private static Dictionary<string, XElement>? LoadXmlDoc()
    {
        var assembly = typeof(OperationXmlDocProcessor).Assembly;
        var xmlFile = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");

        if (!File.Exists(xmlFile))
            return null;

        var xml = XDocument.Load(xmlFile);
        return xml.Descendants("member")
            .Where(m => m.Attribute("name")?.Value.StartsWith("M:") == true)
            .ToDictionary(
                m => m.Attribute("name")!.Value,
                m => m);
    }
}
