using System.Text.Json;
using FluentAssertions;
using PreflightApi.Infrastructure.Services.NotamServices;
using Xunit;

namespace PreflightApi.Tests.NotamTests;

public class AixmNotamParserTests
{
    private const string SampleAixmXml = """
        <?xml version="1.0"?>
        <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
        <soap:Body>
        <ns3:FeatureCollection
        xmlns="http://www.aixm.aero/schema/5.1/message"
        xmlns:aixm="http://www.aixm.aero/schema/5.1"
        xmlns:event="http://www.aixm.aero/schema/5.1/event"
        xmlns:gml="http://www.opengis.net/gml/3.2"
        xmlns:fnse="http://www.aixm.aero/schema/5.1/extensions/FAA/FNSE"
        xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
        xmlns:xlink="http://www.w3.org/1999/xlink"
        xmlns:ns3="http://www.opengis.net/wfs/2.0"
        numberReturned="2" timeStamp="2025-09-12T17:24:02.017Z">
        <aixm:member><AIXMBasicMessage gml:id="NMS_ID_1757609538792382"><gml:boundedBy xsi:nil="true"/><hasMember><aixm:RunwayDirection gml:id="RWYDIR01_1757609538792382"><gml:identifier codeSpace="urn:uuid:">15b52516-d780-48fb-bdf7-291d5733fe97</gml:identifier><gml:boundedBy xsi:nil="true"/><aixm:timeSlice><aixm:RunwayDirectionTimeSlice gml:id="RWYDIR01_TS01_1757609538792382"><gml:validTime><gml:TimeInstant gml:id="RWYDIR01_TS01_TI01_1757609538792382"><gml:timePosition>2025-08-21T02:34:44.784Z</gml:timePosition></gml:TimeInstant></gml:validTime><aixm:interpretation>SNAPSHOT</aixm:interpretation><aixm:designator>20</aixm:designator><aixm:usedRunway xlink:href="#Runway_1_1757609538792382"/><aixm:extension><event:RunwayDirectionExtension gml:id="RDE_EVENT_1_1757609538792382"><event:theEvent xlink:href="#Event_1_1757609538792382"/></event:RunwayDirectionExtension></aixm:extension></aixm:RunwayDirectionTimeSlice></aixm:timeSlice></aixm:RunwayDirection></hasMember><hasMember><aixm:Runway gml:id="Runway_1_1757609538792382"><gml:identifier codeSpace="urn:uuid:">631cdd51-0944-46c0-aa9c-e4569f8b0a6b</gml:identifier><gml:boundedBy xsi:nil="true"/><aixm:timeSlice><aixm:RunwayTimeSlice gml:id="Runway_TS_1_1757609538792382"><gml:validTime><gml:TimeInstant gml:id="Runway_TS_I_1_1757609538792382"><gml:timePosition>2025-08-21T02:34:44.784Z</gml:timePosition></gml:TimeInstant></gml:validTime><aixm:interpretation>SNAPSHOT</aixm:interpretation><aixm:designator>02/20</aixm:designator><aixm:associatedAirportHeliport xlink:href="#Airport_1_1757609538792382"/><aixm:extension><event:RunwayExtension gml:id="RE_EVENT_1_1757609538792382"><event:theEvent xlink:href="#Event_1_1757609538792382"/></event:RunwayExtension></aixm:extension></aixm:RunwayTimeSlice></aixm:timeSlice></aixm:Runway></hasMember><hasMember><aixm:RunwayElement gml:id="RE1_1757609538792382"><gml:identifier codeSpace="urn:uuid:">cee68773-faf6-43a4-8314-cf4dd8cd5fa4</gml:identifier><gml:boundedBy xsi:nil="true"/><aixm:timeSlice><aixm:RunwayElementTimeSlice gml:id="RE01_TS1_1757609538792382"><gml:validTime><gml:TimeInstant gml:id="RE_TI1_1757609538792382"><gml:timePosition>2025-08-21T02:34:44.784Z</gml:timePosition></gml:TimeInstant></gml:validTime><aixm:interpretation>SNAPSHOT</aixm:interpretation><aixm:associatedRunway xlink:href="#Runway_1_1757609538792382"/><aixm:extent><aixm:ElevatedSurface gml:id="ES1_1757609538792382" srsDimension="2" srsName="urn:ogc:def:crs:EPSG::4326"><gml:patches><gml:PolygonPatch><gml:exterior><gml:LinearRing><gml:posList>37.9240226820457 -90.7338195078916 37.9239697067978 -90.7336224367121 37.9343677864238 -90.7291485077434 37.9344207690863 -90.7293456035274 37.9240226820457 -90.7338195078916</gml:posList></gml:LinearRing></gml:exterior></gml:PolygonPatch></gml:patches></aixm:ElevatedSurface></aixm:extent><aixm:extension><event:RunwayElementExtension gml:id="REE_EVENT_1_1757609538792382"><event:theEvent xlink:href="#Event_1_1757609538792382"/></event:RunwayElementExtension></aixm:extension></aixm:RunwayElementTimeSlice></aixm:timeSlice></aixm:RunwayElement></hasMember><hasMember><aixm:AirportHeliport gml:id="Airport_1_1757609538792382"><gml:identifier codeSpace="urn:uuid:">b7a0209e-942f-4d7e-ae8b-c708fce65328</gml:identifier><gml:boundedBy xsi:nil="true"/><aixm:timeSlice><aixm:AirportHeliportTimeSlice gml:id="Airport_TS_1_1757609538792382"><gml:validTime><gml:TimeInstant gml:id="Airport_TS_TI_1_1757609538792382"><gml:timePosition>2025-08-21T02:34:44.784Z</gml:timePosition></gml:TimeInstant></gml:validTime><aixm:interpretation>SNAPSHOT</aixm:interpretation><aixm:designator>8WC</aixm:designator><aixm:name>WASHINGTON COUNTY</aixm:name><aixm:ARP><aixm:ElevatedPoint gml:id="EP01_1757609538792382" srsDimension="2" srsName="urn:ogc:def:crs:EPSG::4326"><gml:pos>37.92919525 -90.7314840277778</gml:pos></aixm:ElevatedPoint></aixm:ARP><aixm:extension><event:AirportHeliportExtension gml:id="AHE_EVENT_1_1757609538792382"><event:theEvent xlink:href="#Event_1_1757609538792382"/></event:AirportHeliportExtension></aixm:extension></aixm:AirportHeliportTimeSlice></aixm:timeSlice></aixm:AirportHeliport></hasMember><hasMember><event:Event gml:id="Event_1_1757609538792382"><gml:identifier codeSpace="urn:uuid:">28c2b867-2028-4d12-b43c-8c0bb0525532</gml:identifier><gml:boundedBy xsi:nil="true"/><event:timeSlice><event:EventTimeSlice gml:id="Event_TS_1_1757609538792382"><gml:validTime><gml:TimePeriod gml:id="Event_TS_TP_1_1757609538792382"><gml:beginPosition>2025-08-21T02:34:00.000Z</gml:beginPosition><gml:endPosition>2025-10-01T23:59:00.000Z</gml:endPosition></gml:TimePeriod></gml:validTime><aixm:interpretation>BASELINE</aixm:interpretation><aixm:sequenceNumber>1</aixm:sequenceNumber><aixm:correctionNumber>0</aixm:correctionNumber><event:scenario>87</event:scenario><event:textNOTAM><event:NOTAM gml:id="NOTAM_1_1757609538792382"><event:number>430</event:number><event:year>2025</event:year><event:type>N</event:type><event:issued>2025-08-21T02:34:00.000Z</event:issued><event:location>8WC</event:location><event:effectiveStart>202508210234</event:effectiveStart><event:effectiveEnd>202510012359</event:effectiveEnd><event:text>RWY 20 RWY END ID LGT U/S</event:text><event:translation><event:NOTAMTranslation gml:id="NT01_1757609538792382"><event:type>LOCAL_FORMAT</event:type><event:simpleText>!STL 08/430 8WC RWY 20 RWY END ID LGT U/S 2508210234-2510012359</event:simpleText></event:NOTAMTranslation></event:translation></event:NOTAM></event:textNOTAM><event:extension><fnse:EventExtension gml:id="ext_01_1757609538792382"><fnse:classification>DOM</fnse:classification><fnse:accountId>STL</fnse:accountId><fnse:airportname>WASHINGTON COUNTY</fnse:airportname><fnse:lastUpdated>2025-08-21T02:34:00.000Z</fnse:lastUpdated></fnse:EventExtension></event:extension></event:EventTimeSlice></event:timeSlice></event:Event></hasMember></AIXMBasicMessage></aixm:member>
        <aixm:member><AIXMBasicMessage gml:id="NMS_ID_1757609468919567"><gml:boundedBy xsi:nil="true"/><hasMember><event:Event gml:id="Event_1_1757609468919567"><gml:identifier codeSpace="urn:uuid:">814da89f-17ab-445c-abd6-fcbcbedae766</gml:identifier><gml:boundedBy xsi:nil="true"/><event:timeSlice><event:EventTimeSlice gml:id="Event_TS_1_1757609468919567"><gml:validTime><gml:TimePeriod gml:id="Event_TS_TP_1_1757609468919567"><gml:beginPosition>2025-05-01T11:00:00.000Z</gml:beginPosition><gml:endPosition>2025-11-02T00:01:00.000Z</gml:endPosition></gml:TimePeriod></gml:validTime><aixm:interpretation>BASELINE</aixm:interpretation><aixm:sequenceNumber>1</aixm:sequenceNumber><aixm:correctionNumber>0</aixm:correctionNumber><event:scenario>101</event:scenario><event:textNOTAM><event:NOTAM gml:id="NOTAM_1_1757609468919567"><event:number>221</event:number><event:year>2025</event:year><event:type>N</event:type><event:issued>2025-04-30T22:28:00.000Z</event:issued><event:location>ZBW</event:location><event:effectiveStart>202505011100</event:effectiveStart><event:effectiveEnd>202511020001</event:effectiveEnd><event:schedule>Daily:1100-0001~DLY 1100-0001</event:schedule><event:text>AIRSPACE UAS WI AN AREA DEFINED AS 1.5NM RADIUS OF 430205N0753903W (12.2NM NW VGC) SFC-1200FT AGL DLY 1100-0001</event:text><event:translation><event:NOTAMTranslation gml:id="NT01_1757609468919567"><event:type>LOCAL_FORMAT</event:type><event:simpleText>!BDR 04/221 ZBW AIRSPACE UAS WI AN AREA DEFINED AS 1.5NM RADIUS OF 430205N0753903W (12.2NM NW VGC) SFC-1200FT AGL DLY 1100-0001 2505011100-2511020001</event:simpleText></event:NOTAMTranslation></event:translation></event:NOTAM></event:textNOTAM><event:extension><fnse:EventExtension gml:id="ext_01_1757609468919567"><fnse:classification>DOM</fnse:classification><fnse:accountId>BDR</fnse:accountId><fnse:airportname>ZBW ARTCC</fnse:airportname><fnse:lastUpdated>2025-04-30T22:28:00.000Z</fnse:lastUpdated><fnse:icaoLocation>KZBW</fnse:icaoLocation></fnse:EventExtension></event:extension></event:EventTimeSlice></event:timeSlice></event:Event></hasMember></AIXMBasicMessage></aixm:member>
        </ns3:FeatureCollection>
        </soap:Body>
        </soap:Envelope>
        """;

    [Fact]
    public void Parse_ShouldReturnTwoNotams_FromSampleXml()
    {
        var results = AixmNotamParser.Parse(SampleAixmXml);

        results.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_FirstNotam_ShouldHaveCorrectNmsId()
    {
        var results = AixmNotamParser.Parse(SampleAixmXml);
        var first = results[0];

        first.Id.Should().Be("NMS_ID_1757609538792382");
        first.Type.Should().Be("Feature");
    }

    [Fact]
    public void Parse_FirstNotam_ShouldHaveCorrectDetailFields()
    {
        var results = AixmNotamParser.Parse(SampleAixmXml);
        var detail = results[0].Properties!.CoreNotamData!.Notam!;

        detail.Id.Should().Be("NMS_ID_1757609538792382");
        detail.Number.Should().Be("430");
        detail.Year.Should().Be("2025");
        detail.Type.Should().Be("N");
        detail.Issued.Should().Be("2025-08-21T02:34:00.000Z");
        detail.Location.Should().Be("8WC");
        detail.EffectiveStart.Should().Be("202508210234");
        detail.EffectiveEnd.Should().Be("202510012359");
        detail.Text.Should().Be("RWY 20 RWY END ID LGT U/S");
        detail.Classification.Should().Be("DOM");
        detail.AccountId.Should().Be("STL");
        detail.LastUpdated.Should().Be("2025-08-21T02:34:00.000Z");
        detail.Schedule.Should().BeNull();
    }

    [Fact]
    public void Parse_FirstNotam_ShouldHaveScenario()
    {
        var results = AixmNotamParser.Parse(SampleAixmXml);
        var notamEvent = results[0].Properties!.CoreNotamData!.NotamEvent!;

        notamEvent.Scenario.Should().Be("87");
    }

    [Fact]
    public void Parse_FirstNotam_ShouldHavePointGeometryFromAirport()
    {
        var results = AixmNotamParser.Parse(SampleAixmXml);
        var geometry = results[0].Geometry;

        geometry.Should().NotBeNull();
        geometry!.Type.Should().Be("Point");
        geometry.Coordinates.Should().NotBeNull();

        // GML pos is "37.92919525 -90.7314840277778" (lat lon)
        // GeoJSON should be [-90.7314840277778, 37.92919525] (lon, lat)
        var coords = (JsonElement)geometry.Coordinates!;
        coords[0].GetDouble().Should().BeApproximately(-90.7314840277778, 0.0001);
        coords[1].GetDouble().Should().BeApproximately(37.92919525, 0.0001);
    }

    [Fact]
    public void Parse_FirstNotam_GeometryShouldWorkWithNotamGeometryParser()
    {
        // Verify the parsed geometry is compatible with the downstream NotamGeometryParser
        var results = AixmNotamParser.Parse(SampleAixmXml);
        var geometry = results[0].Geometry;

        var ntsGeometry = NotamGeometryParser.Parse(geometry);

        ntsGeometry.Should().NotBeNull();
        ntsGeometry.Should().BeOfType<NetTopologySuite.Geometries.Point>();
        var point = (NetTopologySuite.Geometries.Point)ntsGeometry!;
        point.X.Should().BeApproximately(-90.7315, 0.001);
        point.Y.Should().BeApproximately(37.9292, 0.001);
    }

    [Fact]
    public void Parse_SecondNotam_ShouldHaveCorrectFields()
    {
        var results = AixmNotamParser.Parse(SampleAixmXml);
        var second = results[1];

        second.Id.Should().Be("NMS_ID_1757609468919567");

        var detail = second.Properties!.CoreNotamData!.Notam!;
        detail.Location.Should().Be("ZBW");
        detail.IcaoLocation.Should().Be("KZBW");
        detail.Schedule.Should().Be("Daily:1100-0001~DLY 1100-0001");
        detail.Number.Should().Be("221");
    }

    [Fact]
    public void Parse_SecondNotam_ShouldHaveNullGeometry()
    {
        // Airspace NOTAM with no airport or runway elements
        var results = AixmNotamParser.Parse(SampleAixmXml);
        var second = results[1];

        second.Geometry.Should().BeNull();
    }

    [Fact]
    public void Parse_ShouldExtractTranslations()
    {
        var results = AixmNotamParser.Parse(SampleAixmXml);

        var translations = results[0].Properties!.CoreNotamData!.NotamTranslation;
        translations.Should().NotBeNull();
        translations.Should().HaveCount(1);
        translations![0].Type.Should().Be("LOCAL_FORMAT");
        translations[0].SimpleText.Should().Contain("!STL 08/430 8WC RWY 20 RWY END ID LGT U/S");
    }

    [Fact]
    public void Parse_SecondNotam_ShouldHaveTranslation()
    {
        var results = AixmNotamParser.Parse(SampleAixmXml);

        var translations = results[1].Properties!.CoreNotamData!.NotamTranslation;
        translations.Should().NotBeNull();
        translations.Should().HaveCount(1);
        translations![0].Type.Should().Be("LOCAL_FORMAT");
        translations[0].SimpleText.Should().Contain("!BDR 04/221 ZBW");
    }

    [Fact]
    public void Parse_ShouldReturnEmptyList_ForEmptyString()
    {
        var results = AixmNotamParser.Parse("");
        results.Should().BeEmpty();
    }

    [Fact]
    public void Parse_ShouldReturnEmptyList_ForNullString()
    {
        var results = AixmNotamParser.Parse(null!);
        results.Should().BeEmpty();
    }

    [Fact]
    public void Parse_ShouldReturnEmptyList_ForMalformedXml()
    {
        var results = AixmNotamParser.Parse("<not valid xml");
        results.Should().BeEmpty();
    }

    [Fact]
    public void Parse_ShouldReturnEmptyList_ForValidXmlWithNoMembers()
    {
        var xml = """
            <?xml version="1.0"?>
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
            <soap:Body>
            <ns3:FeatureCollection xmlns:ns3="http://www.opengis.net/wfs/2.0"
                xmlns:aixm="http://www.aixm.aero/schema/5.1"
                numberReturned="0" timeStamp="2025-09-12T17:24:02.017Z">
            </ns3:FeatureCollection>
            </soap:Body>
            </soap:Envelope>
            """;

        var results = AixmNotamParser.Parse(xml);
        results.Should().BeEmpty();
    }

    [Fact]
    public void Parse_PolygonGeometry_ShouldBeExtractedWhenNoAirport()
    {
        // Member with runway element but no airport — should fall back to polygon
        var xml = """
            <?xml version="1.0"?>
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
            <soap:Body>
            <ns3:FeatureCollection
            xmlns="http://www.aixm.aero/schema/5.1/message"
            xmlns:aixm="http://www.aixm.aero/schema/5.1"
            xmlns:event="http://www.aixm.aero/schema/5.1/event"
            xmlns:gml="http://www.opengis.net/gml/3.2"
            xmlns:fnse="http://www.aixm.aero/schema/5.1/extensions/FAA/FNSE"
            xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
            xmlns:ns3="http://www.opengis.net/wfs/2.0">
            <aixm:member><AIXMBasicMessage gml:id="NMS_ID_TEST_POLYGON"><gml:boundedBy xsi:nil="true"/>
            <hasMember><aixm:RunwayElement gml:id="RE1_TEST"><gml:identifier codeSpace="urn:uuid:">test</gml:identifier><gml:boundedBy xsi:nil="true"/><aixm:timeSlice><aixm:RunwayElementTimeSlice gml:id="RE01_TS1_TEST"><gml:validTime><gml:TimeInstant gml:id="RE_TI1_TEST"><gml:timePosition>2025-08-21T00:00:00Z</gml:timePosition></gml:TimeInstant></gml:validTime><aixm:interpretation>SNAPSHOT</aixm:interpretation><aixm:extent><aixm:ElevatedSurface gml:id="ES1_TEST" srsDimension="2" srsName="urn:ogc:def:crs:EPSG::4326"><gml:patches><gml:PolygonPatch><gml:exterior><gml:LinearRing><gml:posList>10.0 20.0 10.0 21.0 11.0 21.0 11.0 20.0 10.0 20.0</gml:posList></gml:LinearRing></gml:exterior></gml:PolygonPatch></gml:patches></aixm:ElevatedSurface></aixm:extent></aixm:RunwayElementTimeSlice></aixm:timeSlice></aixm:RunwayElement></hasMember>
            <hasMember><event:Event gml:id="Event_TEST"><gml:identifier codeSpace="urn:uuid:">test</gml:identifier><gml:boundedBy xsi:nil="true"/><event:timeSlice><event:EventTimeSlice gml:id="Event_TS_TEST"><gml:validTime><gml:TimePeriod gml:id="Event_TS_TP_TEST"><gml:beginPosition>2025-08-21T00:00:00Z</gml:beginPosition><gml:endPosition>2025-10-01T00:00:00Z</gml:endPosition></gml:TimePeriod></gml:validTime><aixm:interpretation>BASELINE</aixm:interpretation><aixm:sequenceNumber>1</aixm:sequenceNumber><aixm:correctionNumber>0</aixm:correctionNumber><event:scenario>87</event:scenario><event:textNOTAM><event:NOTAM gml:id="NOTAM_TEST"><event:number>1</event:number><event:year>2025</event:year><event:type>N</event:type><event:issued>2025-08-21T00:00:00Z</event:issued><event:location>TST</event:location><event:effectiveStart>202508210000</event:effectiveStart><event:effectiveEnd>202510010000</event:effectiveEnd><event:text>TEST</event:text></event:NOTAM></event:textNOTAM><event:extension><fnse:EventExtension gml:id="ext_TEST"><fnse:classification>DOM</fnse:classification><fnse:accountId>TST</fnse:accountId><fnse:lastUpdated>2025-08-21T00:00:00Z</fnse:lastUpdated></fnse:EventExtension></event:extension></event:EventTimeSlice></event:timeSlice></event:Event></hasMember>
            </AIXMBasicMessage></aixm:member>
            </ns3:FeatureCollection>
            </soap:Body>
            </soap:Envelope>
            """;

        var results = AixmNotamParser.Parse(xml);

        results.Should().HaveCount(1);
        var geometry = results[0].Geometry;
        geometry.Should().NotBeNull();
        geometry!.Type.Should().Be("Polygon");

        // Verify polygon coordinates are swapped from GML lat/lon to GeoJSON lon/lat
        var coords = (JsonElement)geometry.Coordinates!;
        var ring = coords[0]; // first (only) ring
        ring.GetArrayLength().Should().Be(5); // 5 points (closed ring)

        // First point: GML "10.0 20.0" → GeoJSON [20.0, 10.0]
        ring[0][0].GetDouble().Should().BeApproximately(20.0, 0.001);
        ring[0][1].GetDouble().Should().BeApproximately(10.0, 0.001);
    }
}
