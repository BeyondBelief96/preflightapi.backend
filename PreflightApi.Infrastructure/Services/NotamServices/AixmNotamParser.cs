using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Extensions.Logging;
using PreflightApi.Infrastructure.Dtos.Notam;

namespace PreflightApi.Infrastructure.Services.NotamServices;

/// <summary>
/// Parses AIXM XML (SOAP-wrapped or standalone) from the NMS initial load endpoint into NotamDto objects
/// that match the same GeoJSON Feature structure produced by the delta sync (GeoJSON) endpoint.
/// </summary>
public static partial class AixmNotamParser
{
    private const string NsSoap = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string NsWfs = "http://www.opengis.net/wfs/2.0";
    private const string NsAixmMsg = "http://www.aixm.aero/schema/5.1/message";
    private const string NsAixm = "http://www.aixm.aero/schema/5.1";
    private const string NsEvent = "http://www.aixm.aero/schema/5.1/event";
    private const string NsGml = "http://www.opengis.net/gml/3.2";
    private const string NsFnse = "http://www.aixm.aero/schema/5.1/extensions/FAA/FNSE";
    private const string NsHtml = "http://www.w3.org/1999/xhtml";

    public static List<NotamDto> Parse(string xml, ILogger? logger = null)
    {
        var results = new List<NotamDto>();

        if (string.IsNullOrWhiteSpace(xml))
            return results;

        XmlDocument doc;
        try
        {
            doc = new XmlDocument();
            doc.LoadXml(xml);
        }
        catch (XmlException ex)
        {
            logger?.LogWarning(ex, "Failed to parse AIXM XML");
            return results;
        }

        var nsMgr = CreateNamespaceManager(doc);

        // Each <aixm:member> contains one AIXMBasicMessage (one NOTAM)
        var members = doc.SelectNodes("//aixm:member/msg:AIXMBasicMessage", nsMgr);
        if (members == null || members.Count == 0)
        {
            // Try without msg prefix — the default namespace on FeatureCollection is the message namespace,
            // so AIXMBasicMessage may resolve without a prefix in some documents.
            members = doc.SelectNodes("//aixm:member/AIXMBasicMessage", nsMgr);
        }

        if (members == null)
            return results;

        logger?.LogDebug("Found {Count} AIXM members to parse", members.Count);

        foreach (XmlNode member in members)
        {
            try
            {
                var notam = ParseMember(member, nsMgr, logger);
                if (notam != null)
                    results.Add(notam);
            }
            catch (Exception ex)
            {
                var nmsId = (member as XmlElement)?.GetAttribute("id", NsGml) ?? "unknown";
                logger?.LogWarning(ex, "Failed to parse AIXM member {NmsId}", nmsId);
            }
        }

        return results;
    }

    /// <summary>
    /// Parses a standalone AIXMBasicMessage XML string (not SOAP-wrapped).
    /// Used for the data.aixm response format where each array element is a single NOTAM XML string.
    /// Returns null if the XML is empty, malformed, or contains no parseable NOTAM.
    /// </summary>
    public static NotamDto? ParseSingle(string xml, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return null;

        XmlDocument doc;
        try
        {
            doc = new XmlDocument();
            doc.LoadXml(xml);
        }
        catch (XmlException ex)
        {
            logger?.LogWarning(ex, "Failed to parse standalone AIXM XML");
            return null;
        }

        var nsMgr = CreateNamespaceManager(doc);

        // Find the AIXMBasicMessage root — may have msg: prefix or be in the default namespace
        var member = doc.SelectSingleNode("//msg:AIXMBasicMessage", nsMgr)
                    ?? doc.SelectSingleNode("//AIXMBasicMessage", nsMgr);

        if (member == null)
        {
            logger?.LogDebug("No AIXMBasicMessage found in standalone XML");
            return null;
        }

        try
        {
            return ParseMember(member, nsMgr, logger);
        }
        catch (Exception ex)
        {
            var nmsId = (member as XmlElement)?.GetAttribute("id", NsGml) ?? "unknown";
            logger?.LogWarning(ex, "Failed to parse standalone AIXM member {NmsId}", nmsId);
            return null;
        }
    }

    private static XmlNamespaceManager CreateNamespaceManager(XmlDocument doc)
    {
        var nsMgr = new XmlNamespaceManager(doc.NameTable);
        nsMgr.AddNamespace("soap", NsSoap);
        nsMgr.AddNamespace("ns3", NsWfs);
        nsMgr.AddNamespace("msg", NsAixmMsg);
        nsMgr.AddNamespace("aixm", NsAixm);
        nsMgr.AddNamespace("event", NsEvent);
        nsMgr.AddNamespace("gml", NsGml);
        nsMgr.AddNamespace("fnse", NsFnse);
        nsMgr.AddNamespace("html", NsHtml);
        return nsMgr;
    }

    private static NotamDto? ParseMember(XmlNode member, XmlNamespaceManager nsMgr, ILogger? logger)
    {
        var element = member as XmlElement;
        var nmsId = element?.GetAttribute("id", NsGml);
        if (string.IsNullOrEmpty(nmsId))
            return null;

        // Find the Event element — contains the actual NOTAM data
        var eventTimeSlice = member.SelectSingleNode(
            ".//event:Event/event:timeSlice/event:EventTimeSlice", nsMgr);
        if (eventTimeSlice == null)
            return null;

        // Parse NOTAM detail fields
        var notamNode = eventTimeSlice.SelectSingleNode("event:textNOTAM/event:NOTAM", nsMgr);
        var extensionNode = eventTimeSlice.SelectSingleNode("event:extension/fnse:EventExtension", nsMgr);

        var notamDetail = ParseNotamDetail(nmsId, notamNode, extensionNode, eventTimeSlice, nsMgr);

        // Parse scenario
        var scenario = GetText(eventTimeSlice, "event:scenario", nsMgr);

        // Parse translations
        var translations = ParseTranslations(notamNode, nsMgr);

        // Parse geometry (obstacle/structure preferred, then apron, runway, airport fallback)
        var geometry = ParseGeometry(member, nsMgr, logger);

        return new NotamDto
        {
            Type = "Feature",
            Id = nmsId,
            Geometry = geometry,
            Properties = new NotamPropertiesDto
            {
                CoreNotamData = new CoreNotamDataDto
                {
                    NotamEvent = new NotamEventDto { Scenario = scenario },
                    Notam = notamDetail,
                    NotamTranslation = translations.Count > 0 ? translations : null
                }
            }
        };
    }

    private static NotamDetailDto ParseNotamDetail(
        string nmsId, XmlNode? notamNode, XmlNode? extensionNode,
        XmlNode eventTimeSlice, XmlNamespaceManager nsMgr)
    {
        return new NotamDetailDto
        {
            Id = nmsId,
            Number = GetText(notamNode, "event:number", nsMgr),
            Year = GetText(notamNode, "event:year", nsMgr),
            Type = GetText(notamNode, "event:type", nsMgr),
            Issued = GetText(notamNode, "event:issued", nsMgr),
            Location = GetText(notamNode, "event:location", nsMgr),
            EffectiveStart = GetText(notamNode, "event:effectiveStart", nsMgr),
            EffectiveEnd = GetText(notamNode, "event:effectiveEnd", nsMgr),
            Text = GetText(notamNode, "event:text", nsMgr),
            Schedule = GetText(notamNode, "event:schedule", nsMgr),
            Classification = GetText(extensionNode, "fnse:classification", nsMgr),
            AccountId = GetText(extensionNode, "fnse:accountId", nsMgr),
            LastUpdated = GetText(extensionNode, "fnse:lastUpdated", nsMgr),
            IcaoLocation = GetText(extensionNode, "fnse:icaoLocation", nsMgr),
            // Q-code fields from event:NOTAM
            AffectedFir = GetText(notamNode, "event:affectedFIR", nsMgr),
            SelectionCode = GetText(notamNode, "event:selectionCode", nsMgr),
            Traffic = GetText(notamNode, "event:traffic", nsMgr),
            Purpose = GetText(notamNode, "event:purpose", nsMgr),
            Scope = GetText(notamNode, "event:scope", nsMgr),
            MinimumFl = GetText(notamNode, "event:minimumFL", nsMgr),
            MaximumFl = GetText(notamNode, "event:maximumFL", nsMgr),
            Coordinates = GetText(notamNode, "event:coordinates", nsMgr),
            Radius = GetText(notamNode, "event:radius", nsMgr),
            // Estimated flag from EventTimeSlice's TimePeriod endPosition
            Estimated = ParseEstimatedFlag(eventTimeSlice, nsMgr)
        };
    }

    /// <summary>
    /// Checks for indeterminatePosition="unknown" on gml:endPosition within the EventTimeSlice's TimePeriod.
    /// Returns "true" when the end time is estimated, null otherwise.
    /// </summary>
    private static string? ParseEstimatedFlag(XmlNode eventTimeSlice, XmlNamespaceManager nsMgr)
    {
        var endPosition = eventTimeSlice.SelectSingleNode(
            "gml:validTime/gml:TimePeriod/gml:endPosition", nsMgr) as XmlElement;

        if (endPosition == null)
            return null;

        var indeterminate = endPosition.GetAttribute("indeterminatePosition");
        return indeterminate == "unknown" ? "true" : null;
    }

    private static List<NotamTranslationDto> ParseTranslations(XmlNode? notamNode, XmlNamespaceManager nsMgr)
    {
        var translations = new List<NotamTranslationDto>();
        if (notamNode == null)
            return translations;

        var translationNodes = notamNode.SelectNodes("event:translation/event:NOTAMTranslation", nsMgr);
        if (translationNodes == null)
            return translations;

        foreach (XmlNode t in translationNodes)
        {
            translations.Add(new NotamTranslationDto
            {
                Type = GetText(t, "event:type", nsMgr),
                SimpleText = GetText(t, "event:simpleText", nsMgr),
                FormattedText = GetFormattedText(t, nsMgr)
            });
        }

        return translations;
    }

    /// <summary>
    /// Extracts formattedText from a NOTAMTranslation node.
    /// The AIXM XML contains HTML-encoded text (with br, b tags).
    /// XmlDocument auto-decodes XML entities; we strip remaining HTML tags to produce plain text.
    /// </summary>
    private static string? GetFormattedText(XmlNode translationNode, XmlNamespaceManager nsMgr)
    {
        var formattedNode = translationNode.SelectSingleNode("event:formattedText", nsMgr);
        if (formattedNode == null)
            return null;

        var text = formattedNode.InnerText?.Trim();
        if (string.IsNullOrEmpty(text))
            return null;

        // Clean HTML artifacts: <br/> → \n, strip <b>/<em>/etc tags
        text = BrTagRegex().Replace(text, "\n");
        text = HtmlTagRegex().Replace(text, "");

        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex BrTagRegex();

    [GeneratedRegex(@"</?[a-zA-Z][^>]*>")]
    private static partial Regex HtmlTagRegex();

    private static NotamGeometryDto? ParseGeometry(XmlNode member, XmlNamespaceManager nsMgr, ILogger? logger)
    {
        NotamGeometryDto? innerGeometry = null;

        // Priority 1: VerticalStructure point — actual obstacle/tower location
        var vsPos = member.SelectSingleNode(".//aixm:VerticalStructure//gml:pos", nsMgr);
        if (vsPos != null)
        {
            innerGeometry = ParseGmlPos(vsPos.InnerText.Trim());
        }

        // Priority 2: ApronElement polygon
        if (innerGeometry == null)
        {
            var apronPosList = member.SelectSingleNode(
                ".//aixm:ApronElement//aixm:ElevatedSurface//gml:posList", nsMgr);
            if (apronPosList != null)
                innerGeometry = ParseGmlPosList(apronPosList.InnerText.Trim(), logger);
        }

        // Priority 3: RunwayElement polygon
        if (innerGeometry == null)
        {
            var rwyPosList = member.SelectSingleNode(
                ".//aixm:RunwayElement//aixm:ElevatedSurface//gml:posList", nsMgr);
            if (rwyPosList != null)
                innerGeometry = ParseGmlPosList(rwyPosList.InnerText.Trim(), logger);
        }

        // Priority 4: AirportHeliport point — airport reference point (fallback only)
        if (innerGeometry == null)
        {
            var airportPos = member.SelectSingleNode(".//aixm:AirportHeliport//gml:pos", nsMgr);
            if (airportPos != null)
                innerGeometry = ParseGmlPos(airportPos.InnerText.Trim());
        }

        if (innerGeometry == null)
            return null;

        // Wrap in GeometryCollection to match GeoJSON format from delta sync
        return new NotamGeometryDto
        {
            Type = "GeometryCollection",
            Geometries = [innerGeometry]
        };
    }

    /// <summary>
    /// Parses GML pos "lat lon" into a GeoJSON Point geometry with [lon, lat] coordinates.
    /// </summary>
    private static NotamGeometryDto? ParseGmlPos(string posText)
    {
        var parts = posText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return null;

        if (!double.TryParse(parts[0], out var lat) || !double.TryParse(parts[1], out var lon))
            return null;

        // GeoJSON uses [lon, lat]; GML pos is "lat lon"
        var coordsJson = $"[{lon},{lat}]";
        var coords = JsonDocument.Parse(coordsJson).RootElement.Clone();

        return new NotamGeometryDto
        {
            Type = "Point",
            Coordinates = coords
        };
    }

    /// <summary>
    /// Parses GML posList "lat1 lon1 lat2 lon2 ..." into a GeoJSON Polygon geometry.
    /// </summary>
    private static NotamGeometryDto? ParseGmlPosList(string posListText, ILogger? logger)
    {
        var parts = posListText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 8) // Need at least 4 coordinate pairs (8 values) for a valid polygon ring
            return null;

        if (parts.Length % 2 != 0)
        {
            logger?.LogWarning("GML posList has odd number of values ({Count}), skipping", parts.Length);
            return null;
        }

        var ring = new List<double[]>();
        for (var i = 0; i < parts.Length; i += 2)
        {
            if (!double.TryParse(parts[i], out var lat) || !double.TryParse(parts[i + 1], out var lon))
                return null;

            ring.Add([lon, lat]); // GeoJSON [lon, lat]
        }

        // GeoJSON Polygon: [[[lon,lat], [lon,lat], ...]]
        var coordsJson = JsonSerializer.Serialize(new[] { ring });
        var coords = JsonDocument.Parse(coordsJson).RootElement.Clone();

        return new NotamGeometryDto
        {
            Type = "Polygon",
            Coordinates = coords
        };
    }

    private static string? GetText(XmlNode? parent, string xpath, XmlNamespaceManager nsMgr)
    {
        if (parent == null)
            return null;

        var node = parent.SelectSingleNode(xpath, nsMgr);
        var text = node?.InnerText?.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }
}
