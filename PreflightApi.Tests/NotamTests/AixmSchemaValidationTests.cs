using System.Xml;
using FluentAssertions;
using PreflightApi.Infrastructure.Services.NotamServices.SchemaManifests;
using Xunit;

namespace PreflightApi.Tests.NotamTests;

public class AixmSchemaManifestLoaderTests
{
    [Fact]
    public void Load_ShouldLoadManifest()
    {
        var manifest = AixmSchemaManifestLoader.Load();

        manifest.Should().NotBeNull();
        manifest!.Schema.Should().Be("aixm-notam-schema-manifest-v1");
        manifest.Contexts.Should().NotBeEmpty();
    }

    [Fact]
    public void Load_ShouldHaveExpectedContexts()
    {
        var manifest = AixmSchemaManifestLoader.Load();

        manifest.Should().NotBeNull();
        manifest!.Contexts.Should().ContainKey("AIXMBasicMessage");
        manifest.Contexts.Should().ContainKey("EventTimeSlice");
        manifest.Contexts.Should().ContainKey("NOTAM");
        manifest.Contexts.Should().ContainKey("EventExtension");
        manifest.Contexts.Should().ContainKey("NOTAMTranslation");
    }

    [Fact]
    public void Load_AIXMBasicMessage_ShouldRequireGmlId()
    {
        var manifest = AixmSchemaManifestLoader.Load();

        manifest.Should().NotBeNull();
        var ctx = manifest!.Contexts["AIXMBasicMessage"];
        ctx.Attributes.Should().ContainKey("gml:id");
        ctx.Attributes["gml:id"].Required.Should().BeTrue();
    }

    [Fact]
    public void Load_EventTimeSlice_ShouldHaveExpectedElements()
    {
        var manifest = AixmSchemaManifestLoader.Load();

        manifest.Should().NotBeNull();
        var ctx = manifest!.Contexts["EventTimeSlice"];
        ctx.Elements.Should().ContainKey("gml:validTime");
        ctx.Elements.Should().ContainKey("aixm:interpretation");
        ctx.Elements.Should().ContainKey("event:textNOTAM");
        ctx.Elements.Should().ContainKey("event:extension");

        ctx.Elements["gml:validTime"].Required.Should().BeTrue();
        ctx.Elements["event:textNOTAM"].Required.Should().BeTrue();
        ctx.Elements["event:extension"].Required.Should().BeFalse();
    }

    [Fact]
    public void Load_NOTAM_ShouldHaveCorrectRequiredFlags()
    {
        var manifest = AixmSchemaManifestLoader.Load();

        manifest.Should().NotBeNull();
        var ctx = manifest!.Contexts["NOTAM"];
        ctx.Elements["event:number"].Required.Should().BeTrue();
        ctx.Elements["event:issued"].Required.Should().BeTrue();
        ctx.Elements["event:location"].Required.Should().BeTrue();
        ctx.Elements["event:effectiveStart"].Required.Should().BeTrue();
        ctx.Elements["event:text"].Required.Should().BeTrue();

        ctx.Elements["event:year"].Required.Should().BeFalse();
        ctx.Elements["event:type"].Required.Should().BeFalse();
        ctx.Elements["event:schedule"].Required.Should().BeFalse();
        ctx.Elements["event:effectiveEnd"].Required.Should().BeFalse();
    }

    [Fact]
    public void Load_EventExtension_ShouldRequireClassification()
    {
        var manifest = AixmSchemaManifestLoader.Load();

        manifest.Should().NotBeNull();
        var ctx = manifest!.Contexts["EventExtension"];
        ctx.Elements["fnse:classification"].Required.Should().BeTrue();
        ctx.Elements["fnse:accountId"].Required.Should().BeFalse();
        ctx.Elements["fnse:icaoLocation"].Required.Should().BeFalse();
    }

    [Fact]
    public void Load_NOTAMTranslation_ShouldRequireType()
    {
        var manifest = AixmSchemaManifestLoader.Load();

        manifest.Should().NotBeNull();
        var ctx = manifest!.Contexts["NOTAMTranslation"];
        ctx.Elements["event:type"].Required.Should().BeTrue();
        ctx.Elements["event:simpleText"].Required.Should().BeFalse();
        ctx.Elements["event:formattedText"].Required.Should().BeFalse();
    }

    [Fact]
    public void Load_ContextsWithParent_ShouldHaveParentContextSet()
    {
        var manifest = AixmSchemaManifestLoader.Load();

        manifest.Should().NotBeNull();
        manifest!.Contexts["AIXMBasicMessage"].ParentContext.Should().BeNull();
        manifest.Contexts["EventTimeSlice"].ParentContext.Should().BeNull();
        manifest.Contexts["NOTAM"].ParentContext.Should().Be("EventTimeSlice");
        manifest.Contexts["EventExtension"].ParentContext.Should().Be("EventTimeSlice");
        manifest.Contexts["NOTAMTranslation"].ParentContext.Should().Be("NOTAM");
    }
}

public class AixmSchemaValidatorTests
{
    [Fact]
    public void ValidateMember_ShouldReturnNoDrift_OnValidXml()
    {
        var (member, nsMgr) = ParseMember(BuildValidMemberXml());

        var result = AixmSchemaValidator.ValidateMember(member, nsMgr);

        result.HasDrift.Should().BeFalse();
        result.MissingElements.Should().BeEmpty();
        result.UnexpectedElements.Should().BeEmpty();
        result.MissingAttributes.Should().BeEmpty();
        result.UnexpectedAttributes.Should().BeEmpty();
    }

    [Fact]
    public void ValidateMember_ShouldDetectMissingRequiredElements()
    {
        // NOTAM missing event:number and event:text (both required)
        var xml = BuildMemberXml(
            notamBody: """
                <event:year>2025</event:year>
                <event:type>N</event:type>
                <event:issued>2025-01-01T00:00:00Z</event:issued>
                <event:location>TST</event:location>
                <event:effectiveStart>202501010000</event:effectiveStart>
                """,
            extensionBody: """<fnse:classification>DOM</fnse:classification>""");

        var (member, nsMgr) = ParseMember(xml);

        var result = AixmSchemaValidator.ValidateMember(member, nsMgr);

        result.HasDrift.Should().BeTrue();
        result.MissingElements.Should().Contain("NOTAM.event:number");
        result.MissingElements.Should().Contain("NOTAM.event:text");
    }

    [Fact]
    public void ValidateMember_ShouldNotFlagOptionalMissingElements()
    {
        // Valid NOTAM without optional fields like schedule, affectedFIR, Q-code fields
        var (member, nsMgr) = ParseMember(BuildValidMemberXml());

        var result = AixmSchemaValidator.ValidateMember(member, nsMgr);

        result.MissingElements.Should().BeEmpty();
    }

    [Fact]
    public void ValidateMember_ShouldDetectUnexpectedElements()
    {
        // NOTAM with an unexpected element
        var xml = BuildMemberXml(
            notamBody: """
                <event:number>1</event:number>
                <event:year>2025</event:year>
                <event:type>N</event:type>
                <event:issued>2025-01-01T00:00:00Z</event:issued>
                <event:location>TST</event:location>
                <event:effectiveStart>202501010000</event:effectiveStart>
                <event:text>TEST</event:text>
                <event:brandNewField>surprise</event:brandNewField>
                """,
            extensionBody: """<fnse:classification>DOM</fnse:classification>""");

        var (member, nsMgr) = ParseMember(xml);

        var result = AixmSchemaValidator.ValidateMember(member, nsMgr);

        result.HasDrift.Should().BeTrue();
        result.UnexpectedElements.Should().Contain("NOTAM.event:brandNewField");
    }

    [Fact]
    public void ValidateMember_ShouldDetectUnexpectedExtensionElements()
    {
        var xml = BuildMemberXml(
            notamBody: """
                <event:number>1</event:number>
                <event:issued>2025-01-01T00:00:00Z</event:issued>
                <event:location>TST</event:location>
                <event:effectiveStart>202501010000</event:effectiveStart>
                <event:text>TEST</event:text>
                """,
            extensionBody: """
                <fnse:classification>DOM</fnse:classification>
                <fnse:newExtField>surprise</fnse:newExtField>
                """);

        var (member, nsMgr) = ParseMember(xml);

        var result = AixmSchemaValidator.ValidateMember(member, nsMgr);

        result.HasDrift.Should().BeTrue();
        result.UnexpectedElements.Should().Contain("EventExtension.fnse:newExtField");
    }

    [Fact]
    public void ValidateMember_ShouldDetectMissingRequiredAttributes()
    {
        // AIXMBasicMessage without gml:id attribute
        var xml = """
            <msg:AIXMBasicMessage
            xmlns:msg="http://www.aixm.aero/schema/5.1/message"
            xmlns:aixm="http://www.aixm.aero/schema/5.1"
            xmlns:event="http://www.aixm.aero/schema/5.1/event"
            xmlns:gml="http://www.opengis.net/gml/3.2"
            xmlns:fnse="http://www.aixm.aero/schema/5.1/extensions/FAA/FNSE"
            xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
            <gml:boundedBy xsi:nil="true"/>
            <hasMember><event:Event gml:id="Event_1"><gml:identifier codeSpace="urn:uuid:">test</gml:identifier><gml:boundedBy xsi:nil="true"/><event:timeSlice><event:EventTimeSlice gml:id="Event_TS_1"><gml:validTime><gml:TimePeriod gml:id="Event_TP_1"><gml:beginPosition>2025-01-01T00:00:00Z</gml:beginPosition><gml:endPosition>2025-12-31T23:59:00Z</gml:endPosition></gml:TimePeriod></gml:validTime><aixm:interpretation>BASELINE</aixm:interpretation><aixm:sequenceNumber>1</aixm:sequenceNumber><aixm:correctionNumber>0</aixm:correctionNumber><event:textNOTAM><event:NOTAM gml:id="NOTAM_1"><event:number>1</event:number><event:issued>2025-01-01T00:00:00Z</event:issued><event:location>TST</event:location><event:effectiveStart>202501010000</event:effectiveStart><event:text>TEST</event:text></event:NOTAM></event:textNOTAM><event:extension><fnse:EventExtension gml:id="ext_1"><fnse:classification>DOM</fnse:classification></fnse:EventExtension></event:extension></event:EventTimeSlice></event:timeSlice></event:Event></hasMember>
            </msg:AIXMBasicMessage>
            """;

        var (member, nsMgr) = ParseMember(xml);

        var result = AixmSchemaValidator.ValidateMember(member, nsMgr);

        result.HasDrift.Should().BeTrue();
        result.MissingAttributes.Should().Contain("AIXMBasicMessage.gml:id");
    }

    [Fact]
    public void ValidateMember_ShouldDetectMissingRequiredClassification()
    {
        var xml = BuildMemberXml(
            notamBody: """
                <event:number>1</event:number>
                <event:issued>2025-01-01T00:00:00Z</event:issued>
                <event:location>TST</event:location>
                <event:effectiveStart>202501010000</event:effectiveStart>
                <event:text>TEST</event:text>
                """,
            extensionBody: """
                <fnse:accountId>TST</fnse:accountId>
                """);

        var (member, nsMgr) = ParseMember(xml);

        var result = AixmSchemaValidator.ValidateMember(member, nsMgr);

        result.HasDrift.Should().BeTrue();
        result.MissingElements.Should().Contain("EventExtension.fnse:classification");
    }

    [Fact]
    public void ValidateMember_ShouldValidateTranslationChildren()
    {
        // Translation with unexpected child element
        var xml = BuildMemberXml(
            notamBody: """
                <event:number>1</event:number>
                <event:issued>2025-01-01T00:00:00Z</event:issued>
                <event:location>TST</event:location>
                <event:effectiveStart>202501010000</event:effectiveStart>
                <event:text>TEST</event:text>
                <event:translation>
                    <event:NOTAMTranslation gml:id="NT01">
                        <event:type>LOCAL_FORMAT</event:type>
                        <event:simpleText>Test</event:simpleText>
                        <event:unknownTranslationField>surprise</event:unknownTranslationField>
                    </event:NOTAMTranslation>
                </event:translation>
                """,
            extensionBody: """<fnse:classification>DOM</fnse:classification>""");

        var (member, nsMgr) = ParseMember(xml);

        var result = AixmSchemaValidator.ValidateMember(member, nsMgr);

        result.HasDrift.Should().BeTrue();
        result.UnexpectedElements.Should().Contain("NOTAMTranslation.event:unknownTranslationField");
    }

    [Fact]
    public void ValidateMember_ShouldDetectMissingTranslationType()
    {
        // Translation missing required event:type
        var xml = BuildMemberXml(
            notamBody: """
                <event:number>1</event:number>
                <event:issued>2025-01-01T00:00:00Z</event:issued>
                <event:location>TST</event:location>
                <event:effectiveStart>202501010000</event:effectiveStart>
                <event:text>TEST</event:text>
                <event:translation>
                    <event:NOTAMTranslation gml:id="NT01">
                        <event:simpleText>Test</event:simpleText>
                    </event:NOTAMTranslation>
                </event:translation>
                """,
            extensionBody: """<fnse:classification>DOM</fnse:classification>""");

        var (member, nsMgr) = ParseMember(xml);

        var result = AixmSchemaValidator.ValidateMember(member, nsMgr);

        result.HasDrift.Should().BeTrue();
        result.MissingElements.Should().Contain("NOTAMTranslation.event:type");
    }

    [Fact]
    public void ValidateMember_ShouldHandleAbsentOptionalContexts()
    {
        // No extension and no translations — should not flag anything as missing
        var xml = BuildMemberXml(
            notamBody: """
                <event:number>1</event:number>
                <event:issued>2025-01-01T00:00:00Z</event:issued>
                <event:location>TST</event:location>
                <event:effectiveStart>202501010000</event:effectiveStart>
                <event:text>TEST</event:text>
                """,
            extensionBody: null);

        var (member, nsMgr) = ParseMember(xml);

        var result = AixmSchemaValidator.ValidateMember(member, nsMgr);

        // EventExtension and NOTAMTranslation contexts are absent — no drift from those
        result.MissingElements.Should().NotContain(e => e.StartsWith("EventExtension."));
        result.MissingElements.Should().NotContain(e => e.StartsWith("NOTAMTranslation."));
    }

    [Fact]
    public void ValidateMember_ShouldDetectUnexpectedEventTimeSliceElements()
    {
        // EventTimeSlice with unexpected child
        var xml = BuildMemberXml(
            notamBody: """
                <event:number>1</event:number>
                <event:issued>2025-01-01T00:00:00Z</event:issued>
                <event:location>TST</event:location>
                <event:effectiveStart>202501010000</event:effectiveStart>
                <event:text>TEST</event:text>
                """,
            extensionBody: """<fnse:classification>DOM</fnse:classification>""",
            extraEventTimeSliceElements: """<event:newTimeSliceField>surprise</event:newTimeSliceField>""");

        var (member, nsMgr) = ParseMember(xml);

        var result = AixmSchemaValidator.ValidateMember(member, nsMgr);

        result.HasDrift.Should().BeTrue();
        result.UnexpectedElements.Should().Contain("EventTimeSlice.event:newTimeSliceField");
    }

    #region Helpers

    private static (XmlNode member, XmlNamespaceManager nsMgr) ParseMember(string xml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var nsMgr = new XmlNamespaceManager(doc.NameTable);
        nsMgr.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
        nsMgr.AddNamespace("ns3", "http://www.opengis.net/wfs/2.0");
        nsMgr.AddNamespace("msg", "http://www.aixm.aero/schema/5.1/message");
        nsMgr.AddNamespace("aixm", "http://www.aixm.aero/schema/5.1");
        nsMgr.AddNamespace("event", "http://www.aixm.aero/schema/5.1/event");
        nsMgr.AddNamespace("gml", "http://www.opengis.net/gml/3.2");
        nsMgr.AddNamespace("fnse", "http://www.aixm.aero/schema/5.1/extensions/FAA/FNSE");

        var member = doc.SelectSingleNode("//msg:AIXMBasicMessage", nsMgr)
                    ?? doc.SelectSingleNode("//AIXMBasicMessage", nsMgr);

        member.Should().NotBeNull("test XML should contain an AIXMBasicMessage");
        return (member!, nsMgr);
    }

    private static string BuildValidMemberXml()
    {
        return BuildMemberXml(
            notamBody: """
                <event:number>1</event:number>
                <event:year>2025</event:year>
                <event:type>N</event:type>
                <event:issued>2025-01-01T00:00:00Z</event:issued>
                <event:location>TST</event:location>
                <event:effectiveStart>202501010000</event:effectiveStart>
                <event:effectiveEnd>202512312359</event:effectiveEnd>
                <event:text>TEST NOTAM</event:text>
                <event:translation>
                    <event:NOTAMTranslation gml:id="NT01_TEST">
                        <event:type>LOCAL_FORMAT</event:type>
                        <event:simpleText>!TST 01/001 TST TEST NOTAM</event:simpleText>
                    </event:NOTAMTranslation>
                </event:translation>
                """,
            extensionBody: """
                <fnse:classification>DOM</fnse:classification>
                <fnse:accountId>TST</fnse:accountId>
                <fnse:lastUpdated>2025-01-01T00:00:00Z</fnse:lastUpdated>
                """);
    }

    private static string BuildMemberXml(
        string notamBody,
        string? extensionBody,
        string? extraEventTimeSliceElements = null)
    {
        var extensionSection = extensionBody != null
            ? $"<event:extension><fnse:EventExtension gml:id=\"ext_TEST\">{extensionBody}</fnse:EventExtension></event:extension>"
            : "";

        return $"""
            <msg:AIXMBasicMessage
            xmlns:msg="http://www.aixm.aero/schema/5.1/message"
            xmlns:aixm="http://www.aixm.aero/schema/5.1"
            xmlns:event="http://www.aixm.aero/schema/5.1/event"
            xmlns:gml="http://www.opengis.net/gml/3.2"
            xmlns:fnse="http://www.aixm.aero/schema/5.1/extensions/FAA/FNSE"
            xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
            gml:id="NMS_ID_TEST">
            <gml:boundedBy xsi:nil="true"/>
            <hasMember><event:Event gml:id="Event_TEST"><gml:identifier codeSpace="urn:uuid:">test</gml:identifier><gml:boundedBy xsi:nil="true"/><event:timeSlice><event:EventTimeSlice gml:id="Event_TS_TEST"><gml:validTime><gml:TimePeriod gml:id="Event_TP_TEST"><gml:beginPosition>2025-01-01T00:00:00Z</gml:beginPosition><gml:endPosition>2025-12-31T23:59:00Z</gml:endPosition></gml:TimePeriod></gml:validTime><aixm:interpretation>BASELINE</aixm:interpretation><aixm:sequenceNumber>1</aixm:sequenceNumber><aixm:correctionNumber>0</aixm:correctionNumber><event:scenario>87</event:scenario><event:textNOTAM><event:NOTAM gml:id="NOTAM_TEST">{notamBody}</event:NOTAM></event:textNOTAM>{extensionSection}{extraEventTimeSliceElements ?? ""}</event:EventTimeSlice></event:timeSlice></event:Event></hasMember>
            </msg:AIXMBasicMessage>
            """;
    }

    #endregion
}
