using System.Text.Json;
using FluentAssertions;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.ValueObjects;
using Xunit;

namespace PreflightApi.Tests.NavaidTests;

public class NavaidCronServiceTests
{
    #region CreateUniqueKey

    [Fact]
    public void CreateUniqueKey_ProducesCorrectCompositeKey()
    {
        var navaid = new Navaid
        {
            NavId = "DFW",
            NavType = "VORTAC",
            CountryCode = "US",
            City = "DALLAS-FT WORTH"
        };

        navaid.CreateUniqueKey().Should().Be("DFW|VORTAC|US|DALLAS-FT WORTH");
    }

    [Fact]
    public void CreateUniqueKey_HandlesEmptyFields()
    {
        var navaid = new Navaid
        {
            NavId = "ABC",
            NavType = "",
            CountryCode = "US",
            City = ""
        };

        navaid.CreateUniqueKey().Should().Be("ABC||US|");
    }

    #endregion

    #region UpdateFrom - Full Update

    [Fact]
    public void UpdateFrom_FullUpdate_CopiesAllProperties()
    {
        var target = new Navaid
        {
            Id = Guid.NewGuid(),
            NavId = "DFW",
            NavType = "VORTAC",
            CountryCode = "US",
            City = "DALLAS-FT WORTH"
        };

        var source = CreateFullNavaid();

        target.UpdateFrom(source);

        // Key properties should NOT be updated (they're the identity)
        target.Id.Should().NotBe(source.Id);

        // All other properties should be updated
        target.EffectiveDate.Should().Be(source.EffectiveDate);
        target.NavStatus.Should().Be("OPERATIONAL");
        target.Name.Should().Be("DALLAS-FT WORTH");
        target.StateName.Should().Be("TEXAS");
        target.RegionCode.Should().Be("ASW");
        target.CountryName.Should().Be("UNITED STATES");
        target.NasUseFlag.Should().Be("Y");
        target.PublicUseFlag.Should().Be("Y");
        target.HighAltArtccId.Should().Be("ZFW");
        target.LowAltArtccId.Should().Be("ZFW");
        target.LatDecimal.Should().Be(32.89m);
        target.LongDecimal.Should().Be(-97.04m);
        target.Freq.Should().Be(113.10m);
        target.Chan.Should().Be("78X");
        target.Elev.Should().Be(603.1m);
        target.MagVarn.Should().Be(5);
        target.MagVarnHemis.Should().Be("E");
        target.VoiceCall.Should().Be("DALLAS-FT WORTH");
        target.CheckpointsJson.Should().Be(source.CheckpointsJson);
        target.RemarksJson.Should().Be(source.RemarksJson);
    }

    #endregion

    #region UpdateFrom - Selective Update

    [Fact]
    public void UpdateFrom_SelectiveUpdate_OnlyUpdatesSpecifiedProperties()
    {
        var target = new Navaid
        {
            Id = Guid.NewGuid(),
            NavId = "DFW",
            NavType = "VORTAC",
            CountryCode = "US",
            City = "DALLAS-FT WORTH",
            NavStatus = "DECOMMISSIONED",
            Name = "OLD NAME",
            Freq = 100.00m
        };

        var source = new Navaid
        {
            NavStatus = "OPERATIONAL",
            Freq = 113.10m,
            Name = null! // null should not overwrite
        };

        var limitTo = new HashSet<string> { nameof(Navaid.NavStatus), nameof(Navaid.Freq), nameof(Navaid.Name) };
        target.UpdateFrom(source, limitTo);

        target.NavStatus.Should().Be("OPERATIONAL");
        target.Freq.Should().Be(113.10m);
        target.Name.Should().Be("OLD NAME"); // Not overwritten because source.Name is null
    }

    [Fact]
    public void UpdateFrom_SelectiveUpdate_DoesNotTouchUnspecifiedProperties()
    {
        var target = new Navaid
        {
            NavId = "DFW",
            NavType = "VORTAC",
            CountryCode = "US",
            City = "DALLAS-FT WORTH",
            NavStatus = "OPERATIONAL",
            Elev = 603.1m,
            VoiceCall = "DALLAS"
        };

        var source = new Navaid
        {
            NavStatus = "DECOMMISSIONED"
        };

        var limitTo = new HashSet<string> { nameof(Navaid.NavStatus) };
        target.UpdateFrom(source, limitTo);

        target.NavStatus.Should().Be("DECOMMISSIONED");
        target.Elev.Should().Be(603.1m); // Unchanged
        target.VoiceCall.Should().Be("DALLAS"); // Unchanged
    }

    #endregion

    #region CreateSelectiveEntity

    [Fact]
    public void CreateSelectiveEntity_CopiesOnlySpecifiedProperties()
    {
        var source = CreateFullNavaid();

        var properties = new HashSet<string>
        {
            nameof(Navaid.NavId),
            nameof(Navaid.NavType),
            nameof(Navaid.CountryCode),
            nameof(Navaid.City),
            nameof(Navaid.NavStatus),
            nameof(Navaid.Freq)
        };

        var selective = source.CreateSelectiveEntity(properties);

        selective.NavId.Should().Be(source.NavId);
        selective.NavType.Should().Be(source.NavType);
        selective.CountryCode.Should().Be(source.CountryCode);
        selective.City.Should().Be(source.City);
        selective.NavStatus.Should().Be(source.NavStatus);
        selective.Freq.Should().Be(source.Freq);

        // Properties not in the set should be default
        selective.Name.Should().Be(string.Empty);
        selective.Elev.Should().BeNull();
        selective.VoiceCall.Should().BeNull();
        selective.CheckpointsJson.Should().BeNull();
    }

    #endregion

    #region Checkpoint JSON Serialization

    [Fact]
    public void CheckpointJson_RoundTrip_PreservesAllData()
    {
        var checkpoints = new List<NavaidCheckpoint>
        {
            new()
            {
                Altitude = 3000,
                Bearing = 180,
                AirGroundCode = "A",
                Description = "OVER DFW AIRPORT",
                AirportId = "DFW",
                StateCode = "TX"
            },
            new()
            {
                Altitude = null,
                Bearing = 90,
                AirGroundCode = "G",
                Description = "ON TAXIWAY ALPHA",
                AirportId = null,
                StateCode = "TX"
            }
        };

        var json = JsonSerializer.Serialize(checkpoints);
        var deserialized = JsonSerializer.Deserialize<List<NavaidCheckpoint>>(json);

        deserialized.Should().HaveCount(2);
        deserialized![0].Altitude.Should().Be(3000);
        deserialized[0].Bearing.Should().Be(180);
        deserialized[0].AirGroundCode.Should().Be("A");
        deserialized[0].Description.Should().Be("OVER DFW AIRPORT");
        deserialized[0].AirportId.Should().Be("DFW");
        deserialized[0].StateCode.Should().Be("TX");

        deserialized[1].Altitude.Should().BeNull();
        deserialized[1].Bearing.Should().Be(90);
        deserialized[1].AirGroundCode.Should().Be("G");
        deserialized[1].AirportId.Should().BeNull();
    }

    #endregion

    #region Remark JSON Serialization

    [Fact]
    public void RemarkJson_RoundTrip_PreservesAllData()
    {
        var remarks = new List<NavaidRemark>
        {
            new()
            {
                TabName = "NAV_BASE",
                ReferenceColumnName = "NAV_STATUS",
                SequenceNumber = 1,
                Remark = "VOR UNUSABLE 090-180 BELOW 3000 FT MSL"
            },
            new()
            {
                TabName = "NAV_BASE",
                ReferenceColumnName = "OPER_HOURS",
                SequenceNumber = 1,
                Remark = "CONTINUOUS EXCEPT 0200-0600 UTC"
            }
        };

        var json = JsonSerializer.Serialize(remarks);
        var deserialized = JsonSerializer.Deserialize<List<NavaidRemark>>(json);

        deserialized.Should().HaveCount(2);
        deserialized![0].TabName.Should().Be("NAV_BASE");
        deserialized[0].ReferenceColumnName.Should().Be("NAV_STATUS");
        deserialized[0].SequenceNumber.Should().Be(1);
        deserialized[0].Remark.Should().Be("VOR UNUSABLE 090-180 BELOW 3000 FT MSL");

        deserialized[1].ReferenceColumnName.Should().Be("OPER_HOURS");
        deserialized[1].Remark.Should().Be("CONTINUOUS EXCEPT 0200-0600 UTC");
    }

    #endregion

    #region Helpers

    private static Navaid CreateFullNavaid()
    {
        return new Navaid
        {
            Id = Guid.NewGuid(),
            EffectiveDate = new DateTime(2026, 2, 19, 0, 0, 0, DateTimeKind.Utc),
            NavId = "DFW",
            NavType = "VORTAC",
            StateCode = "TX",
            City = "DALLAS-FT WORTH",
            CountryCode = "US",
            NavStatus = "OPERATIONAL",
            Name = "DALLAS-FT WORTH",
            StateName = "TEXAS",
            RegionCode = "ASW",
            CountryName = "UNITED STATES",
            NasUseFlag = "Y",
            PublicUseFlag = "Y",
            HighAltArtccId = "ZFW",
            HighArtccName = "FORT WORTH",
            LowAltArtccId = "ZFW",
            LowArtccName = "FORT WORTH",
            LatDecimal = 32.89m,
            LongDecimal = -97.04m,
            Freq = 113.10m,
            Chan = "78X",
            Elev = 603.1m,
            MagVarn = 5,
            MagVarnHemis = "E",
            MagVarnYear = 2020,
            VoiceCall = "DALLAS-FT WORTH",
            AltCode = "H",
            DmeSsv = "H",
            FssId = "FTW",
            FssName = "FORT WORTH",
            NotamId = "DFW",
            CheckpointsJson = "[{\"Altitude\":3000,\"Bearing\":180,\"AirGroundCode\":\"A\",\"Description\":\"OVER DFW\",\"AirportId\":\"DFW\",\"StateCode\":\"TX\"}]",
            RemarksJson = "[{\"TabName\":\"NAV_BASE\",\"ReferenceColumnName\":\"NAV_STATUS\",\"SequenceNumber\":1,\"Remark\":\"Test remark\"}]"
        };
    }

    #endregion
}
