using FluentAssertions;
using PreflightApi.Infrastructure.Utilities;
using Xunit;

namespace PreflightApi.Tests.Utilities;

public class CursorHelperTests
{
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
}
