using System.Xml.Linq;
using FluentAssertions;
using PreflightApi.Infrastructure.Services.CronJobServices.WeatherServices.SchemaManifests;
using Xunit;

namespace PreflightApi.Tests.WeatherServicesTests;

public class AvWxSchemaManifestLoaderTests
{
    [Fact]
    public void LoadAll_ShouldLoadAllFiveManifests()
    {
        var manifests = AvWxSchemaManifestLoader.LoadAll();

        manifests.Should().HaveCount(5);
        manifests.Select(m => m.Entity).Should().BeEquivalentTo(
            new[] { "Metar", "Taf", "Pirep", "Sigmet", "GAirmet" });
    }

    [Theory]
    [InlineData("metar", "METAR", "Metar")]
    [InlineData("taf", "TAF", "Taf")]
    [InlineData("pirep", "AircraftReport", "Pirep")]
    [InlineData("sigmet", "AIRSIGMET", "Sigmet")]
    [InlineData("gairmet", "GAIRMET", "GAirmet")]
    public void LoadByWeatherType_ShouldLoadCorrectManifest(string weatherType, string expectedRoot, string expectedEntity)
    {
        var manifest = AvWxSchemaManifestLoader.LoadByWeatherType(weatherType);

        manifest.Should().NotBeNull();
        manifest!.XmlRootElement.Should().Be(expectedRoot);
        manifest.Entity.Should().Be(expectedEntity);
        manifest.Schema.Should().Be("avwx-schema-manifest-v1");
        manifest.Elements.Should().NotBeEmpty();
    }

    [Fact]
    public void LoadByWeatherType_ShouldReturnNull_ForUnknownType()
    {
        var manifest = AvWxSchemaManifestLoader.LoadByWeatherType("unknown_type");

        manifest.Should().BeNull();
    }

    [Fact]
    public void MetarManifest_ShouldHaveExpectedElements()
    {
        var manifest = AvWxSchemaManifestLoader.LoadByWeatherType("metar");

        manifest.Should().NotBeNull();
        manifest!.Elements.Should().ContainKey("station_id");
        manifest.Elements.Should().ContainKey("raw_text");
        manifest.Elements.Should().ContainKey("temp_c");
        manifest.Elements.Should().ContainKey("wind_dir_degrees");
        manifest.Elements.Should().ContainKey("flight_category");
        manifest.Elements["station_id"].Required.Should().BeTrue();
        manifest.Elements["station_id"].OpenApiProperty.Should().Be("icaoId");
        manifest.Elements["station_id"].Captured.Should().BeTrue();
    }

    [Fact]
    public void MetarManifest_ShouldHaveNestedElements()
    {
        var manifest = AvWxSchemaManifestLoader.LoadByWeatherType("metar");

        manifest.Should().NotBeNull();
        manifest!.NestedElements.Should().NotBeNull();
        manifest.NestedElements.Should().ContainKey("quality_control_flags");
        manifest.NestedElements.Should().ContainKey("sky_condition");

        // quality_control_flags uses child elements
        manifest.NestedElements!["quality_control_flags"].Elements.Should().NotBeNull();
        manifest.NestedElements["quality_control_flags"].Elements.Should().ContainKey("corrected");
        manifest.NestedElements["quality_control_flags"].Elements.Should().ContainKey("auto");

        // sky_condition uses attributes
        manifest.NestedElements["sky_condition"].Attributes.Should().NotBeNull();
        manifest.NestedElements["sky_condition"].Attributes.Should().ContainKey("sky_cover");
        manifest.NestedElements["sky_condition"].Attributes.Should().ContainKey("cloud_base_ft_agl");
    }

    [Fact]
    public void TafManifest_ShouldHaveForecastNestedElement()
    {
        var manifest = AvWxSchemaManifestLoader.LoadByWeatherType("taf");

        manifest.Should().NotBeNull();
        manifest!.NestedElements.Should().ContainKey("forecast");
        manifest.NestedElements!["forecast"].Elements.Should().ContainKey("fcst_time_from");
        manifest.NestedElements["forecast"].Elements.Should().ContainKey("wind_speed_kt");
        manifest.NestedElements["forecast"].Elements.Should().ContainKey("sky_condition");
        manifest.NestedElements["forecast"].Elements.Should().ContainKey("turbulence_condition");
        manifest.NestedElements["forecast"].Elements.Should().ContainKey("icing_condition");
        manifest.NestedElements["forecast"].Elements.Should().ContainKey("temperature");
    }

    [Fact]
    public void PirepManifest_ShouldHaveConditionAttributes()
    {
        var manifest = AvWxSchemaManifestLoader.LoadByWeatherType("pirep");

        manifest.Should().NotBeNull();
        manifest!.NestedElements.Should().ContainKey("sky_condition");
        manifest.NestedElements!["sky_condition"].Attributes.Should().ContainKey("sky_cover");
        manifest.NestedElements["sky_condition"].Attributes.Should().ContainKey("cloud_base_ft_msl");
        manifest.NestedElements["sky_condition"].Attributes.Should().ContainKey("cloud_top_ft_msl");

        manifest.NestedElements.Should().ContainKey("turbulence_condition");
        manifest.NestedElements["turbulence_condition"].Attributes.Should().ContainKey("turbulence_type");
        manifest.NestedElements["turbulence_condition"].Attributes.Should().ContainKey("turbulence_freq");

        manifest.NestedElements.Should().ContainKey("icing_condition");
        manifest.NestedElements["icing_condition"].Attributes.Should().ContainKey("icing_type");
    }

    [Fact]
    public void SigmetManifest_ShouldHaveHazardAndAltitudeAttributes()
    {
        var manifest = AvWxSchemaManifestLoader.LoadByWeatherType("sigmet");

        manifest.Should().NotBeNull();
        manifest!.NestedElements.Should().ContainKey("hazard");
        manifest.NestedElements!["hazard"].Attributes.Should().ContainKey("type");
        manifest.NestedElements["hazard"].Attributes.Should().ContainKey("severity");

        manifest.NestedElements.Should().ContainKey("altitude");
        manifest.NestedElements["altitude"].Attributes.Should().ContainKey("min_ft_msl");
        manifest.NestedElements["altitude"].Attributes.Should().ContainKey("max_ft_msl");
    }

    [Fact]
    public void GAirmetManifest_ShouldHaveUniqueGAirmetElements()
    {
        var manifest = AvWxSchemaManifestLoader.LoadByWeatherType("gairmet");

        manifest.Should().NotBeNull();
        manifest!.Elements.Should().ContainKey("roughly_the_number_of_hours_between_the_issue_time_and_the_valid_time");
        manifest.Elements.Should().ContainKey("product");
        manifest.Elements.Should().ContainKey("due_to");
        manifest.NestedElements.Should().ContainKey("hazard");
        manifest.NestedElements!["hazard"].Attributes.Should().ContainKey("type");
        manifest.NestedElements["hazard"].Attributes.Should().ContainKey("severity");
    }
}

public class AvWxSchemaValidatorTests
{
    [Fact]
    public void ValidateElement_ShouldReturnNoDrift_WhenAllRequiredElementsPresent()
    {
        // All required METAR elements present, all known elements from manifest
        var xml = XElement.Parse(@"
            <METAR>
                <raw_text>KORD 111951Z 36012KT 10SM FEW250 M04/M18 A3021</raw_text>
                <station_id>KORD</station_id>
                <observation_time>2026-02-11T19:51:00Z</observation_time>
                <latitude>41.98</latitude>
                <longitude>-87.9</longitude>
                <temp_c>-4.0</temp_c>
                <dewpoint_c>-18.0</dewpoint_c>
                <wind_dir_degrees>360</wind_dir_degrees>
                <wind_speed_kt>12</wind_speed_kt>
                <visibility_statute_mi>10</visibility_statute_mi>
                <altim_in_hg>30.21</altim_in_hg>
                <quality_control_flags>
                    <auto>TRUE</auto>
                </quality_control_flags>
                <sky_condition sky_cover=""FEW"" cloud_base_ft_agl=""25000""/>
                <flight_category>VFR</flight_category>
                <metar_type>METAR</metar_type>
                <elevation_m>201.0</elevation_m>
            </METAR>");

        var result = AvWxSchemaValidator.ValidateElement("metar", xml);

        result.HasDrift.Should().BeFalse();
        result.MissingElements.Should().BeEmpty();
        result.UnexpectedElements.Should().BeEmpty();
        result.MissingAttributes.Should().BeEmpty();
        result.UnexpectedAttributes.Should().BeEmpty();
    }

    [Fact]
    public void ValidateElement_ShouldDetectMissingRequiredElements()
    {
        // XML missing station_id (required) and observation_time (required)
        var xml = XElement.Parse(@"
            <METAR>
                <raw_text>KORD 111951Z</raw_text>
                <latitude>41.98</latitude>
                <longitude>-87.9</longitude>
                <temp_c>-4.0</temp_c>
                <dewpoint_c>-18.0</dewpoint_c>
                <wind_dir_degrees>360</wind_dir_degrees>
                <wind_speed_kt>12</wind_speed_kt>
                <sky_condition sky_cover=""FEW"" cloud_base_ft_agl=""25000""/>
                <flight_category>VFR</flight_category>
                <metar_type>METAR</metar_type>
                <elevation_m>201.0</elevation_m>
            </METAR>");

        var result = AvWxSchemaValidator.ValidateElement("metar", xml);

        result.HasDrift.Should().BeTrue();
        result.MissingElements.Should().Contain("station_id");
        result.MissingElements.Should().Contain("observation_time");
    }

    [Fact]
    public void ValidateElement_ShouldNotFlagOptionalMissingElements()
    {
        // Minimal METAR with only required elements - no drift expected
        var xml = XElement.Parse(@"
            <METAR>
                <station_id>KORD</station_id>
                <observation_time>2026-02-11T19:51:00Z</observation_time>
                <latitude>41.98</latitude>
                <longitude>-87.9</longitude>
            </METAR>");

        var result = AvWxSchemaValidator.ValidateElement("metar", xml);

        result.MissingElements.Should().BeEmpty();
    }

    [Fact]
    public void ValidateElement_ShouldDetectUnexpectedElements()
    {
        var xml = XElement.Parse(@"
            <METAR>
                <station_id>KORD</station_id>
                <observation_time>2026-02-11T19:51:00Z</observation_time>
                <latitude>41.98</latitude>
                <longitude>-87.9</longitude>
                <temp_c>-4.0</temp_c>
                <new_unknown_element>some_value</new_unknown_element>
                <another_new_field>42</another_new_field>
            </METAR>");

        var result = AvWxSchemaValidator.ValidateElement("metar", xml);

        result.HasDrift.Should().BeTrue();
        result.UnexpectedElements.Should().Contain("new_unknown_element");
        result.UnexpectedElements.Should().Contain("another_new_field");
        result.MissingElements.Should().BeEmpty();
    }

    [Fact]
    public void ValidateElement_ShouldDetectMissingRequiredAttributes_OnNestedElement()
    {
        // sky_condition missing sky_cover attribute (which is required)
        var xml = XElement.Parse(@"
            <METAR>
                <station_id>KORD</station_id>
                <observation_time>2026-02-11T19:51:00Z</observation_time>
                <latitude>41.98</latitude>
                <longitude>-87.9</longitude>
                <sky_condition cloud_base_ft_agl=""25000""/>
            </METAR>");

        var result = AvWxSchemaValidator.ValidateElement("metar", xml);

        result.HasDrift.Should().BeTrue();
        result.MissingAttributes.Should().Contain("sky_condition.sky_cover");
    }

    [Fact]
    public void ValidateElement_ShouldNotFlagOptionalMissingAttributes()
    {
        // sky_condition with only required sky_cover, missing optional cloud_base_ft_agl
        var xml = XElement.Parse(@"
            <METAR>
                <station_id>KORD</station_id>
                <observation_time>2026-02-11T19:51:00Z</observation_time>
                <latitude>41.98</latitude>
                <longitude>-87.9</longitude>
                <sky_condition sky_cover=""CLR""/>
            </METAR>");

        var result = AvWxSchemaValidator.ValidateElement("metar", xml);

        result.MissingAttributes.Should().BeEmpty();
    }

    [Fact]
    public void ValidateElement_ShouldDetectUnexpectedAttributes_OnNestedElement()
    {
        var xml = XElement.Parse(@"
            <METAR>
                <station_id>KORD</station_id>
                <observation_time>2026-02-11T19:51:00Z</observation_time>
                <latitude>41.98</latitude>
                <longitude>-87.9</longitude>
                <sky_condition sky_cover=""FEW"" cloud_base_ft_agl=""25000"" new_cloud_attr=""CB""/>
            </METAR>");

        var result = AvWxSchemaValidator.ValidateElement("metar", xml);

        result.HasDrift.Should().BeTrue();
        result.UnexpectedAttributes.Should().Contain("sky_condition.new_cloud_attr");
    }

    [Fact]
    public void ValidateElement_ShouldReturnEmptyResult_ForUnknownWeatherType()
    {
        var xml = XElement.Parse("<UNKNOWN><field>value</field></UNKNOWN>");

        var result = AvWxSchemaValidator.ValidateElement("unknown_type", xml);

        result.HasDrift.Should().BeFalse();
        result.WeatherType.Should().Be("unknown_type");
    }

    [Fact]
    public void ValidateElement_ShouldValidateNestedChildElements()
    {
        // quality_control_flags with unexpected child element
        var xml = XElement.Parse(@"
            <METAR>
                <station_id>KORD</station_id>
                <observation_time>2026-02-11T19:51:00Z</observation_time>
                <latitude>41.98</latitude>
                <longitude>-87.9</longitude>
                <quality_control_flags>
                    <auto>TRUE</auto>
                    <new_qc_flag>TRUE</new_qc_flag>
                </quality_control_flags>
            </METAR>");

        var result = AvWxSchemaValidator.ValidateElement("metar", xml);

        result.HasDrift.Should().BeTrue();
        result.UnexpectedElements.Should().Contain("quality_control_flags.new_qc_flag");
    }

    [Fact]
    public void ValidateElement_ShouldWorkWithPirepXml()
    {
        var xml = XElement.Parse(@"
            <AircraftReport>
                <receipt_time>2026-02-11T19:30:00Z</receipt_time>
                <observation_time>2026-02-11T19:25:00Z</observation_time>
                <quality_control_flags>
                    <mid_point_assumed>TRUE</mid_point_assumed>
                </quality_control_flags>
                <aircraft_ref>B738</aircraft_ref>
                <latitude>40.0</latitude>
                <longitude>-88.0</longitude>
                <altitude_ft_msl>35000</altitude_ft_msl>
                <sky_condition sky_cover=""OVC"" cloud_base_ft_msl=""2000"" cloud_top_ft_msl=""5000""/>
                <turbulence_condition turbulence_type=""CAT"" turbulence_intensity=""MOD"" turbulence_base_ft_msl=""30000"" turbulence_top_ft_msl=""38000"" turbulence_freq=""OCNL""/>
                <icing_condition icing_type=""RIME"" icing_intensity=""LGT"" icing_base_ft_msl=""10000"" icing_top_ft_msl=""15000""/>
                <visibility_statute_mi>5</visibility_statute_mi>
                <wx_string>-RA</wx_string>
                <temp_c>-50.0</temp_c>
                <wind_dir_degrees>270</wind_dir_degrees>
                <wind_speed_kt>45</wind_speed_kt>
                <vert_gust_kt>10</vert_gust_kt>
                <report_type>PIREP</report_type>
                <raw_text>UA /OV ORD/TM 1925/FL350/TP B738/SK OVC020-TOP050/TA -50/WV 27045KT/TB MOD CAT 300-380 OCNL/IC LGT RIME 100-150</raw_text>
            </AircraftReport>");

        var result = AvWxSchemaValidator.ValidateElement("pirep", xml);

        result.HasDrift.Should().BeFalse();
    }

    [Fact]
    public void ValidateElement_ShouldWorkWithSigmetXml()
    {
        var xml = XElement.Parse(@"
            <AIRSIGMET>
                <raw_text>SIGMET TANGO 1 VALID</raw_text>
                <valid_time_from>2026-02-11T18:00:00Z</valid_time_from>
                <valid_time_to>2026-02-11T22:00:00Z</valid_time_to>
                <movement_dir_degrees>270</movement_dir_degrees>
                <movement_spd_kt>25</movement_spd_kt>
                <airsigmet_type>SIGMET</airsigmet_type>
                <altitude min_ft_msl=""10000"" max_ft_msl=""40000""/>
                <hazard type=""TURB"" severity=""SEV""/>
                <area num_points=""4"">
                    <point><latitude>40.0</latitude><longitude>-90.0</longitude></point>
                    <point><latitude>42.0</latitude><longitude>-90.0</longitude></point>
                    <point><latitude>42.0</latitude><longitude>-85.0</longitude></point>
                    <point><latitude>40.0</latitude><longitude>-85.0</longitude></point>
                </area>
            </AIRSIGMET>");

        var result = AvWxSchemaValidator.ValidateElement("sigmet", xml);

        result.HasDrift.Should().BeFalse();
    }

    [Fact]
    public void ValidateElement_ShouldWorkWithGAirmetXml()
    {
        var xml = XElement.Parse(@"
            <GAIRMET>
                <receipt_time>2026-02-11T18:00:00Z</receipt_time>
                <issue_time>2026-02-11T17:45:00Z</issue_time>
                <expire_time>2026-02-12T00:00:00Z</expire_time>
                <valid_time>2026-02-11T21:00:00Z</valid_time>
                <product>SIERRA</product>
                <tag>IFR_CIG</tag>
                <roughly_the_number_of_hours_between_the_issue_time_and_the_valid_time>3</roughly_the_number_of_hours_between_the_issue_time_and_the_valid_time>
                <hazard type=""IFR"" severity=""MOD""/>
                <geometry_type>AREA</geometry_type>
                <due_to>CIG BLW 010/VIS BLW 3SM PCPN/BR</due_to>
                <altitude min_ft_msl=""SFC"" max_ft_msl=""10000""/>
                <area num_points=""3"">
                    <point><latitude>40.0</latitude><longitude>-90.0</longitude></point>
                    <point><latitude>42.0</latitude><longitude>-88.0</longitude></point>
                    <point><latitude>40.0</latitude><longitude>-86.0</longitude></point>
                </area>
            </GAIRMET>");

        var result = AvWxSchemaValidator.ValidateElement("gairmet", xml);

        result.HasDrift.Should().BeFalse();
    }
}
