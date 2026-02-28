using System.Text.Json;
using FluentAssertions;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Enums;
using PreflightApi.Domain.ValueObjects;
using PreflightApi.Infrastructure.Dtos.Mappers;
using Xunit;

namespace PreflightApi.Tests.NavaidTests;

public class NavaidMapperTests
{
    [Fact]
    public void ToDto_MapsAllFields_Correctly()
    {
        var navaid = CreateTestNavaid();

        var dto = NavaidMapper.ToDto(navaid);

        dto.Id.Should().Be(navaid.Id);
        dto.NavId.Should().Be("DFW");
        dto.NavType.Should().Be(NavaidType.Vortac);
        dto.NavStatus.Should().Be("OPERATIONAL");
        dto.Name.Should().Be("DALLAS-FORT WORTH");
        dto.City.Should().Be("DALLAS");
        dto.StateCode.Should().Be("TX");
        dto.StateName.Should().Be("TEXAS");
        dto.CountryCode.Should().Be("US");
        dto.CountryName.Should().Be("UNITED STATES");
        dto.Owner.Should().Be("FAA");
        dto.Operator.Should().Be("FAA");
        dto.Latitude.Should().Be(32.89680000m);
        dto.Longitude.Should().Be(-97.03800000m);
        dto.Elevation.Should().Be(607.0m);
        dto.MagneticVariation.Should().Be(4);
        dto.MagneticVariationDirection.Should().Be("E");
        dto.MagneticVariationYear.Should().Be(2020);
        dto.Frequency.Should().Be(113.10m);
        dto.Channel.Should().Be("78X");
        dto.VoiceCall.Should().Be("COWBOY");
        dto.OperatingHours.Should().Be("CONTINUOUS");
        dto.AltCode.Should().Be(VorServiceVolume.High);
        dto.DmeSsv.Should().Be(DmeServiceVolume.High);
        dto.PowerOutput.Should().Be(200);
        dto.HighAltArtccId.Should().Be("ZFW");
        dto.HighArtccName.Should().Be("FORT WORTH");
        dto.LowAltArtccId.Should().Be("ZFW");
        dto.LowArtccName.Should().Be("FORT WORTH");
        dto.FssId.Should().Be("FTW");
        dto.FssName.Should().Be("FORT WORTH");
        dto.NotamId.Should().Be("DFW");
        dto.TacanDmeStatus.Should().Be("OPERATIONAL");
        dto.TacanDmeLatitude.Should().Be(32.89700000m);
        dto.TacanDmeLongitude.Should().Be(-97.03900000m);
        dto.EffectiveDate.Should().Be(new DateTime(2024, 1, 25));
    }

    [Theory]
    [InlineData("Y", true)]
    [InlineData("y", true)]
    [InlineData("N", false)]
    [InlineData("n", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("  ", false)]
    public void ParseBool_ConvertsFlags_Correctly(string? input, bool expected)
    {
        NavaidMapper.ParseBool(input).Should().Be(expected);
    }

    [Fact]
    public void ToDto_ConvertsBoolFlags_Correctly()
    {
        var navaid = CreateTestNavaid();
        navaid.NasUseFlag = "Y";
        navaid.PublicUseFlag = "Y";
        navaid.SimulVoiceFlag = "Y";
        navaid.AutoVoiceIdFlag = "N";
        navaid.HiwasFlag = "Y";
        navaid.LowNavOnHighChartFlag = "N";

        var dto = NavaidMapper.ToDto(navaid);

        dto.NasUse.Should().BeTrue();
        dto.PublicUse.Should().BeTrue();
        dto.SimultaneousVoice.Should().BeTrue();
        dto.AutomaticVoiceId.Should().BeFalse();
        dto.Hiwas.Should().BeTrue();
        dto.LowNavOnHighChart.Should().BeFalse();
    }

    [Fact]
    public void ToDto_DeserializesCheckpoints_FromValidJson()
    {
        var checkpoints = new List<NavaidCheckpoint>
        {
            new() { Bearing = 180, AirGroundCode = "G", Description = "ON AIRPORT", AirportId = "KDFW", StateCode = "TX" }
        };
        var navaid = CreateTestNavaid();
        navaid.CheckpointsJson = JsonSerializer.Serialize(checkpoints);

        var dto = NavaidMapper.ToDto(navaid);

        dto.Checkpoints.Should().NotBeNull();
        dto.Checkpoints.Should().HaveCount(1);
        dto.Checkpoints![0].Bearing.Should().Be(180);
        dto.Checkpoints[0].AirportId.Should().Be("KDFW");
    }

    [Fact]
    public void ToDto_DeserializesRemarks_FromValidJson()
    {
        var remarks = new List<NavaidRemark>
        {
            new() { TabName = "NAV1", ReferenceColumnName = "STATUS", SequenceNumber = 1, Remark = "VOR UNUSABLE 090-180" }
        };
        var navaid = CreateTestNavaid();
        navaid.RemarksJson = JsonSerializer.Serialize(remarks);

        var dto = NavaidMapper.ToDto(navaid);

        dto.Remarks.Should().NotBeNull();
        dto.Remarks.Should().HaveCount(1);
        dto.Remarks![0].Remark.Should().Be("VOR UNUSABLE 090-180");
    }

    [Fact]
    public void ToDto_ReturnsNullCheckpoints_WhenJsonIsNull()
    {
        var navaid = CreateTestNavaid();
        navaid.CheckpointsJson = null;

        var dto = NavaidMapper.ToDto(navaid);

        dto.Checkpoints.Should().BeNull();
    }

    [Fact]
    public void ToDto_ReturnsNullCheckpoints_WhenJsonIsMalformed()
    {
        var navaid = CreateTestNavaid();
        navaid.CheckpointsJson = "not valid json{{{";

        var dto = NavaidMapper.ToDto(navaid);

        dto.Checkpoints.Should().BeNull();
    }

    [Fact]
    public void ToDto_ReturnsNullRemarks_WhenJsonIsEmpty()
    {
        var navaid = CreateTestNavaid();
        navaid.RemarksJson = "";

        var dto = NavaidMapper.ToDto(navaid);

        dto.Remarks.Should().BeNull();
    }

    [Fact]
    public void ToDto_HandlesNullOptionalFields_Gracefully()
    {
        var navaid = new Navaid
        {
            Id = Guid.NewGuid(),
            NavId = "TST",
            NavType = "NDB",
            NavStatus = "OPERATIONAL",
            Name = "TEST",
            City = "TESTVILLE",
            CountryCode = "US",
            CountryName = "UNITED STATES",
            NasUseFlag = "N",
            PublicUseFlag = "N",
            EffectiveDate = DateTime.UtcNow
        };

        var dto = NavaidMapper.ToDto(navaid);

        dto.StateCode.Should().BeNull();
        dto.StateName.Should().BeNull();
        dto.Latitude.Should().BeNull();
        dto.Longitude.Should().BeNull();
        dto.Elevation.Should().BeNull();
        dto.Frequency.Should().BeNull();
        dto.Channel.Should().BeNull();
        dto.AltCode.Should().BeNull();
        dto.DmeSsv.Should().BeNull();
        dto.Checkpoints.Should().BeNull();
        dto.Remarks.Should().BeNull();
    }

    #region ParseNavaidType Tests

    [Theory]
    [InlineData("CONSOLAN", NavaidType.Consolan)]
    [InlineData("DME", NavaidType.Dme)]
    [InlineData("FAN MARKER", NavaidType.FanMarker)]
    [InlineData("MARINE NDB", NavaidType.MarineNdb)]
    [InlineData("MARINE NDB/DME", NavaidType.MarineNdbDme)]
    [InlineData("NDB", NavaidType.Ndb)]
    [InlineData("NDB/DME", NavaidType.NdbDme)]
    [InlineData("TACAN", NavaidType.Tacan)]
    [InlineData("UHF/NDB", NavaidType.UhfNdb)]
    [InlineData("VOR", NavaidType.Vor)]
    [InlineData("VORTAC", NavaidType.Vortac)]
    [InlineData("VOR/DME", NavaidType.VorDme)]
    [InlineData("VOT", NavaidType.Vot)]
    public void ParseNavaidType_KnownValues_ReturnsCorrectEnum(string input, NavaidType expected)
    {
        NavaidMapper.ParseNavaidType(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("  VOR  ")]
    [InlineData("vor")]
    [InlineData("Vor")]
    public void ParseNavaidType_HandlesWhitespaceAndCase(string input)
    {
        NavaidMapper.ParseNavaidType(input).Should().Be(NavaidType.Vor);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("INVALID")]
    public void ParseNavaidType_UnknownOrEmpty_ReturnsUnknown(string? input)
    {
        NavaidMapper.ParseNavaidType(input).Should().Be(NavaidType.Unknown);
    }

    #endregion

    #region ParseVorServiceVolume Tests

    [Theory]
    [InlineData("H", VorServiceVolume.High)]
    [InlineData("L", VorServiceVolume.Low)]
    [InlineData("T", VorServiceVolume.Terminal)]
    [InlineData("VH", VorServiceVolume.VorHigh)]
    [InlineData("VL", VorServiceVolume.VorLow)]
    public void ParseVorServiceVolume_KnownValues_ReturnsCorrectEnum(string input, VorServiceVolume expected)
    {
        NavaidMapper.ParseVorServiceVolume(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("  H  ")]
    [InlineData("h")]
    public void ParseVorServiceVolume_HandlesWhitespaceAndCase(string input)
    {
        NavaidMapper.ParseVorServiceVolume(input).Should().Be(VorServiceVolume.High);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ParseVorServiceVolume_NullOrEmpty_ReturnsNull(string? input)
    {
        NavaidMapper.ParseVorServiceVolume(input).Should().BeNull();
    }

    [Fact]
    public void ParseVorServiceVolume_UnrecognizedValue_ReturnsUnknown()
    {
        NavaidMapper.ParseVorServiceVolume("XYZ").Should().Be(VorServiceVolume.Unknown);
    }

    #endregion

    #region ParseDmeServiceVolume Tests

    [Theory]
    [InlineData("H", DmeServiceVolume.High)]
    [InlineData("L", DmeServiceVolume.Low)]
    [InlineData("T", DmeServiceVolume.Terminal)]
    [InlineData("DH", DmeServiceVolume.DmeHigh)]
    [InlineData("DL", DmeServiceVolume.DmeLow)]
    public void ParseDmeServiceVolume_KnownValues_ReturnsCorrectEnum(string input, DmeServiceVolume expected)
    {
        NavaidMapper.ParseDmeServiceVolume(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("  H  ")]
    [InlineData("h")]
    public void ParseDmeServiceVolume_HandlesWhitespaceAndCase(string input)
    {
        NavaidMapper.ParseDmeServiceVolume(input).Should().Be(DmeServiceVolume.High);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ParseDmeServiceVolume_NullOrEmpty_ReturnsNull(string? input)
    {
        NavaidMapper.ParseDmeServiceVolume(input).Should().BeNull();
    }

    [Fact]
    public void ParseDmeServiceVolume_UnrecognizedValue_ReturnsUnknown()
    {
        NavaidMapper.ParseDmeServiceVolume("XYZ").Should().Be(DmeServiceVolume.Unknown);
    }

    #endregion

    #region ToDbString Tests

    [Theory]
    [InlineData(NavaidType.Consolan, "CONSOLAN")]
    [InlineData(NavaidType.Dme, "DME")]
    [InlineData(NavaidType.FanMarker, "FAN MARKER")]
    [InlineData(NavaidType.MarineNdb, "MARINE NDB")]
    [InlineData(NavaidType.MarineNdbDme, "MARINE NDB/DME")]
    [InlineData(NavaidType.Ndb, "NDB")]
    [InlineData(NavaidType.NdbDme, "NDB/DME")]
    [InlineData(NavaidType.Tacan, "TACAN")]
    [InlineData(NavaidType.UhfNdb, "UHF/NDB")]
    [InlineData(NavaidType.Vor, "VOR")]
    [InlineData(NavaidType.Vortac, "VORTAC")]
    [InlineData(NavaidType.VorDme, "VOR/DME")]
    [InlineData(NavaidType.Vot, "VOT")]
    public void ToDbString_AllTypes_ReturnsCorrectFaaString(NavaidType type, string expected)
    {
        NavaidMapper.ToDbString(type).Should().Be(expected);
    }

    [Fact]
    public void ToDbString_RoundTrips_WithParseNavaidType()
    {
        foreach (NavaidType type in Enum.GetValues<NavaidType>())
        {
            if (type == NavaidType.Unknown) continue;

            var dbString = NavaidMapper.ToDbString(type);
            var parsed = NavaidMapper.ParseNavaidType(dbString);
            parsed.Should().Be(type, $"round-trip failed for {type} → \"{dbString}\"");
        }
    }

    #endregion

    private static Navaid CreateTestNavaid()
    {
        return new Navaid
        {
            Id = Guid.NewGuid(),
            NavId = "DFW",
            NavType = "VORTAC",
            NavStatus = "OPERATIONAL",
            Name = "DALLAS-FORT WORTH",
            City = "DALLAS",
            StateCode = "TX",
            StateName = "TEXAS",
            CountryCode = "US",
            CountryName = "UNITED STATES",
            Owner = "FAA",
            Operator = "FAA",
            NasUseFlag = "Y",
            PublicUseFlag = "Y",
            LatDecimal = 32.89680000m,
            LongDecimal = -97.03800000m,
            Elev = 607.0m,
            MagVarn = 4,
            MagVarnHemis = "E",
            MagVarnYear = 2020,
            Freq = 113.10m,
            Chan = "78X",
            VoiceCall = "COWBOY",
            OperHours = "CONTINUOUS",
            AltCode = "H",
            DmeSsv = "H",
            SimulVoiceFlag = "Y",
            AutoVoiceIdFlag = "Y",
            HiwasFlag = "N",
            LowNavOnHighChartFlag = "N",
            PwrOutput = 200,
            TacanDmeStatus = "OPERATIONAL",
            TacanDmeLatDecimal = 32.89700000m,
            TacanDmeLongDecimal = -97.03900000m,
            HighAltArtccId = "ZFW",
            HighArtccName = "FORT WORTH",
            LowAltArtccId = "ZFW",
            LowArtccName = "FORT WORTH",
            FssId = "FTW",
            FssName = "FORT WORTH",
            NotamId = "DFW",
            EffectiveDate = new DateTime(2024, 1, 25)
        };
    }
}
