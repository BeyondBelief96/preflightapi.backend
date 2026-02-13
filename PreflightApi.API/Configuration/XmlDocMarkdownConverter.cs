using System.Text;
using System.Xml.Linq;

namespace PreflightApi.API.Configuration;

/// <summary>
/// Converts XML documentation elements to markdown text. Handles inline formatting
/// (<c>&lt;b&gt;</c>/<c>&lt;strong&gt;</c>, <c>&lt;i&gt;</c>/<c>&lt;em&gt;</c>, <c>&lt;c&gt;</c>,
/// <c>&lt;code&gt;</c>), references (<c>&lt;see&gt;</c>, <c>&lt;seealso&gt;</c>),
/// structure (<c>&lt;para&gt;</c>), and lists (<c>&lt;list&gt;</c> with
/// <c>&lt;item&gt;</c>/<c>&lt;term&gt;</c>/<c>&lt;description&gt;</c>).
/// </summary>
public static class XmlDocMarkdownConverter
{
    // Inline tags that simply wrap their content with markdown markers.
    // Adding a new inline tag is a single dictionary entry.
    internal static readonly Dictionary<string, (string Open, string Close)> InlineWrappers = new()
    {
        ["b"] = ("**", "**"),
        ["strong"] = ("**", "**"),
        ["i"] = ("*", "*"),
        ["em"] = ("*", "*"),
    };

    /// <summary>
    /// Converts an XML documentation element to markdown, normalizing whitespace
    /// within paragraphs while preserving paragraph breaks.
    /// </summary>
    public static string ConvertXmlDocToMarkdown(XElement element)
    {
        var result = ConvertNodes(element.Nodes());
        // Normalize whitespace within each paragraph, preserving paragraph breaks
        var paragraphs = result.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var normalized = paragraphs.Select(p =>
            string.Join(" ", p.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries)));
        return string.Join("\n\n", normalized);
    }

    internal static string ConvertNodes(IEnumerable<XNode> nodes)
    {
        var sb = new StringBuilder();
        foreach (var node in nodes)
        {
            switch (node)
            {
                case XText text:
                    sb.Append(text.Value);
                    break;
                case XElement el:
                    ConvertElement(el, sb);
                    break;
            }
        }
        return sb.ToString();
    }

    private static void ConvertElement(XElement el, StringBuilder sb)
    {
        var tag = el.Name.LocalName;

        // Inline wrapper tags — declarative lookup
        if (InlineWrappers.TryGetValue(tag, out var wrapper))
        {
            sb.Append(wrapper.Open).Append(ConvertNodes(el.Nodes())).Append(wrapper.Close);
            return;
        }

        switch (tag)
        {
            case "para":
                sb.Append("\n\n").Append(ConvertNodes(el.Nodes())).Append("\n\n");
                break;
            case "c":
                sb.Append('`').Append(el.Value).Append('`');
                break;
            case "code":
                sb.Append("\n\n```\n").Append(el.Value.Trim()).Append("\n```\n\n");
                break;
            case "see":
            case "seealso":
                AppendCref(el, sb);
                break;
            case "list":
                AppendList(el, sb);
                break;
            default:
                sb.Append(ConvertNodes(el.Nodes()));
                break;
        }
    }

    private static void AppendCref(XElement el, StringBuilder sb)
    {
        var cref = el.Attribute("cref")?.Value;
        if (cref == null)
        {
            sb.Append(el.Value);
            return;
        }

        // Strip prefix (T:, M:, P:, F:) and namespace, keep short name
        var name = cref.Contains(':') ? cref[(cref.IndexOf(':') + 1)..] : cref;
        var shortName = name.Contains('.') ? name[(name.LastIndexOf('.') + 1)..] : name;
        sb.Append('`').Append(shortName).Append('`');
    }

    private static void AppendList(XElement el, StringBuilder sb)
    {
        sb.Append("\n\n");
        foreach (var item in el.Elements("item"))
        {
            var term = item.Element("term");
            var desc = item.Element("description");

            sb.Append("\n\n- ");
            if (term != null)
            {
                sb.Append("**").Append(ConvertNodes(term.Nodes())).Append("**");
                if (desc != null)
                    sb.Append(" — ").Append(ConvertNodes(desc.Nodes()));
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
    }
}
