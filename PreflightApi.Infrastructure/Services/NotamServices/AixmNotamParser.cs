using System.Text.Json;
using System.Xml;
using Microsoft.Extensions.Logging;
using PreflightApi.Infrastructure.Dtos.Notam;

namespace PreflightApi.Infrastructure.Services.NotamServices;

/// <summary>
/// Parses AIXM XML (SOAP-wrapped) from the NMS initial load endpoint into NotamDto objects
/// that match the same GeoJSON Feature structure produced by the delta sync (GeoJSON) endpoint.
/// </summary>
public static class AixmNotamParser
{
    private const string NsSoap = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string NsWfs = "http://www.opengis.net/wfs/2.0";
    private const string NsAixmMsg = "http://www.aixm.aero/schema/5.1/message";
    private const string NsAixm = "http://www.aixm.aero/schema/5.1";
    private const string NsEvent = "http://www.aixm.aero/schema/5.1/event";
    private const string NsGml = "http://www.opengis.net/gml/3.2";
    private const string NsFnse = "http://www.aixm.aero/schema/5.1/extensions/FAA/FNSE";

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

        var nsMgr = new XmlNamespaceManager(doc.NameTable);
        nsMgr.AddNamespace("soap", NsSoap);
        nsMgr.AddNamespace("ns3", NsWfs);
        nsMgr.AddNamespace("msg", NsAixmMsg);
        nsMgr.AddNamespace("aixm", NsAixm);
        nsMgr.AddNamespace("event", NsEvent);
        nsMgr.AddNamespace("gml", NsGml);
        nsMgr.AddNamespace("fnse", NsFnse);

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

        var notamDetail = ParseNotamDetail(nmsId, notamNode, extensionNode, nsMgr);

        // Parse scenario
        var scenario = GetText(eventTimeSlice, "event:scenario", nsMgr);

        // Parse translations
        var translations = ParseTranslations(notamNode, nsMgr);

        // Parse geometry (airport point preferred, then runway polygon)
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
        string nmsId, XmlNode? notamNode, XmlNode? extensionNode, XmlNamespaceManager nsMgr)
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
            IcaoLocation = GetText(extensionNode, "fnse:icaoLocation", nsMgr)
        };
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
                SimpleText = GetText(t, "event:simpleText", nsMgr)
            });
        }

        return translations;
    }

    private static NotamGeometryDto? ParseGeometry(XmlNode member, XmlNamespaceManager nsMgr, ILogger? logger)
    {
        // Priority 1: Airport point (most useful for spatial queries)
        var posNode = member.SelectSingleNode(
            ".//aixm:AirportHeliport//gml:pos", nsMgr);
        if (posNode != null)
        {
            var point = ParseGmlPos(posNode.InnerText.Trim());
            if (point != null)
                return point;
        }

        // Priority 2: Runway polygon
        var posListNode = member.SelectSingleNode(
            ".//aixm:RunwayElement//gml:posList", nsMgr);
        if (posListNode != null)
        {
            var polygon = ParseGmlPosList(posListNode.InnerText.Trim(), logger);
            if (polygon != null)
                return polygon;
        }

        return null;
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
