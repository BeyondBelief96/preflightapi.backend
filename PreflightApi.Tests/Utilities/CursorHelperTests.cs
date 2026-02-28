using FluentAssertions;
using PreflightApi.Infrastructure.Utilities;
using Xunit;

namespace PreflightApi.Tests.Utilities;

public class CursorHelperTests
{
    // --- Existing legacy method tests ---

    [Fact]
    public void Encode_String_And_DecodeString_Should_RoundTrip()
    {
        var original = "ABC-12345";
        var encoded = CursorHelper.Encode(original);
        var decoded = CursorHelper.DecodeString(encoded);
        decoded.Should().Be(original);
    }

    [Fact]
    public void Encode_Int_And_DecodeInt_Should_RoundTrip()
    {
        var original = 42;
        var encoded = CursorHelper.Encode(original);
        var decoded = CursorHelper.DecodeInt(encoded);
        decoded.Should().Be(original);
    }

    [Fact]
    public void DecodeString_Null_Should_Return_Null()
    {
        CursorHelper.DecodeString(null).Should().BeNull();
    }

    [Fact]
    public void DecodeString_Empty_Should_Return_Null()
    {
        CursorHelper.DecodeString("").Should().BeNull();
    }

    [Fact]
    public void DecodeInt_Null_Should_Return_Null()
    {
        CursorHelper.DecodeInt(null).Should().BeNull();
    }

    [Fact]
    public void DecodeInt_Empty_Should_Return_Null()
    {
        CursorHelper.DecodeInt("").Should().BeNull();
    }

    [Fact]
    public void DecodeString_Invalid_Base64_Should_Return_Null()
    {
        CursorHelper.DecodeString("not-valid-base64!!!").Should().BeNull();
    }

    [Fact]
    public void DecodeInt_Invalid_Base64_Should_Return_Null()
    {
        CursorHelper.DecodeInt("not-valid-base64!!!").Should().BeNull();
    }

    [Fact]
    public void DecodeInt_Valid_Base64_But_Not_Int_Should_Return_Null()
    {
        var encoded = CursorHelper.Encode("not-a-number");
        CursorHelper.DecodeInt(encoded).Should().BeNull();
    }

    // --- Legacy Decode methods strip directional prefix ---

    [Fact]
    public void DecodeString_Strips_Forward_Prefix()
    {
        var encoded = CursorHelper.EncodeNext("ABC");
        CursorHelper.DecodeString(encoded).Should().Be("ABC");
    }

    [Fact]
    public void DecodeString_Strips_Backward_Prefix()
    {
        var encoded = CursorHelper.EncodePrevious("ABC");
        CursorHelper.DecodeString(encoded).Should().Be("ABC");
    }

    [Fact]
    public void DecodeInt_Strips_Forward_Prefix()
    {
        var encoded = CursorHelper.EncodeNext(42);
        CursorHelper.DecodeInt(encoded).Should().Be(42);
    }

    [Fact]
    public void DecodeGuid_Strips_Forward_Prefix()
    {
        var guid = Guid.NewGuid();
        var encoded = CursorHelper.EncodeNext(guid);
        CursorHelper.DecodeGuid(encoded).Should().Be(guid);
    }

    // --- EncodeNext / EncodePrevious + DecodeXxxWithDirection round-trips ---

    [Fact]
    public void EncodeNext_String_DecodeStringWithDirection_RoundTrips()
    {
        var original = "KATL";
        var encoded = CursorHelper.EncodeNext(original);
        var decoded = CursorHelper.DecodeStringWithDirection(encoded);

        decoded.Should().NotBeNull();
        decoded!.Value.Should().Be(original);
        decoded.Direction.Should().Be(CursorDirection.Forward);
    }

    [Fact]
    public void EncodePrevious_String_DecodeStringWithDirection_RoundTrips()
    {
        var original = "KATL";
        var encoded = CursorHelper.EncodePrevious(original);
        var decoded = CursorHelper.DecodeStringWithDirection(encoded);

        decoded.Should().NotBeNull();
        decoded!.Value.Should().Be(original);
        decoded.Direction.Should().Be(CursorDirection.Backward);
    }

    [Fact]
    public void EncodeNext_Int_DecodeIntWithDirection_RoundTrips()
    {
        var encoded = CursorHelper.EncodeNext(99);
        var decoded = CursorHelper.DecodeIntWithDirection(encoded);

        decoded.Should().NotBeNull();
        decoded!.Value.Should().Be(99);
        decoded.Direction.Should().Be(CursorDirection.Forward);
    }

    [Fact]
    public void EncodePrevious_Int_DecodeIntWithDirection_RoundTrips()
    {
        var encoded = CursorHelper.EncodePrevious(99);
        var decoded = CursorHelper.DecodeIntWithDirection(encoded);

        decoded.Should().NotBeNull();
        decoded!.Value.Should().Be(99);
        decoded.Direction.Should().Be(CursorDirection.Backward);
    }

    [Fact]
    public void EncodeNext_Guid_DecodeGuidWithDirection_RoundTrips()
    {
        var guid = Guid.NewGuid();
        var encoded = CursorHelper.EncodeNext(guid);
        var decoded = CursorHelper.DecodeGuidWithDirection(encoded);

        decoded.Should().NotBeNull();
        decoded!.Value.Should().Be(guid);
        decoded.Direction.Should().Be(CursorDirection.Forward);
    }

    [Fact]
    public void EncodePrevious_Guid_DecodeGuidWithDirection_RoundTrips()
    {
        var guid = Guid.NewGuid();
        var encoded = CursorHelper.EncodePrevious(guid);
        var decoded = CursorHelper.DecodeGuidWithDirection(encoded);

        decoded.Should().NotBeNull();
        decoded!.Value.Should().Be(guid);
        decoded.Direction.Should().Be(CursorDirection.Backward);
    }

    // --- Legacy cursors (no prefix) decode as Forward ---

    [Fact]
    public void Legacy_Cursor_DecodeStringWithDirection_Returns_Forward()
    {
        var encoded = CursorHelper.Encode("KORD");
        var decoded = CursorHelper.DecodeStringWithDirection(encoded);

        decoded.Should().NotBeNull();
        decoded!.Value.Should().Be("KORD");
        decoded.Direction.Should().Be(CursorDirection.Forward);
    }

    [Fact]
    public void Legacy_Cursor_DecodeIntWithDirection_Returns_Forward()
    {
        var encoded = CursorHelper.Encode(7);
        var decoded = CursorHelper.DecodeIntWithDirection(encoded);

        decoded.Should().NotBeNull();
        decoded!.Value.Should().Be(7);
        decoded.Direction.Should().Be(CursorDirection.Forward);
    }

    [Fact]
    public void Legacy_Cursor_DecodeGuidWithDirection_Returns_Forward()
    {
        var guid = Guid.NewGuid();
        var encoded = CursorHelper.Encode(guid);
        var decoded = CursorHelper.DecodeGuidWithDirection(encoded);

        decoded.Should().NotBeNull();
        decoded!.Value.Should().Be(guid);
        decoded.Direction.Should().Be(CursorDirection.Forward);
    }

    // --- Null / empty / invalid for WithDirection methods ---

    [Fact]
    public void DecodeStringWithDirection_Null_Returns_Null()
    {
        CursorHelper.DecodeStringWithDirection(null).Should().BeNull();
    }

    [Fact]
    public void DecodeStringWithDirection_Empty_Returns_Null()
    {
        CursorHelper.DecodeStringWithDirection("").Should().BeNull();
    }

    [Fact]
    public void DecodeIntWithDirection_Null_Returns_Null()
    {
        CursorHelper.DecodeIntWithDirection(null).Should().BeNull();
    }

    [Fact]
    public void DecodeIntWithDirection_Empty_Returns_Null()
    {
        CursorHelper.DecodeIntWithDirection("").Should().BeNull();
    }

    [Fact]
    public void DecodeGuidWithDirection_Null_Returns_Null()
    {
        CursorHelper.DecodeGuidWithDirection(null).Should().BeNull();
    }

    [Fact]
    public void DecodeGuidWithDirection_Empty_Returns_Null()
    {
        CursorHelper.DecodeGuidWithDirection("").Should().BeNull();
    }

    [Fact]
    public void DecodeStringWithDirection_Invalid_Base64_Returns_Null()
    {
        CursorHelper.DecodeStringWithDirection("not-valid!!!").Should().BeNull();
    }

    [Fact]
    public void DecodeIntWithDirection_Invalid_Base64_Returns_Null()
    {
        CursorHelper.DecodeIntWithDirection("not-valid!!!").Should().BeNull();
    }

    [Fact]
    public void DecodeGuidWithDirection_Invalid_Base64_Returns_Null()
    {
        CursorHelper.DecodeGuidWithDirection("not-valid!!!").Should().BeNull();
    }

    [Fact]
    public void DecodeIntWithDirection_Valid_Base64_But_Not_Int_Returns_Null()
    {
        var encoded = CursorHelper.EncodeNext("not-a-number");
        CursorHelper.DecodeIntWithDirection(encoded).Should().BeNull();
    }

    [Fact]
    public void DecodeGuidWithDirection_Valid_Base64_But_Not_Guid_Returns_Null()
    {
        var encoded = CursorHelper.EncodeNext("not-a-guid");
        CursorHelper.DecodeGuidWithDirection(encoded).Should().BeNull();
    }
}
