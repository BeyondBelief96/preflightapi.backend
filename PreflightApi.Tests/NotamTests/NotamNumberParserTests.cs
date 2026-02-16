using FluentAssertions;
using PreflightApi.Infrastructure.Services.NotamServices;
using Xunit;

namespace PreflightApi.Tests.NotamTests;

public class NotamNumberParserTests
{
    #region Bare number

    [Fact]
    public void Parse_BareNumber_ShouldReturnNumber()
    {
        var result = NotamNumberParser.Parse("3997");

        result.Should().NotBeNull();
        result!.Number.Should().Be("3997");
        result.Year.Should().BeNull();
        result.AccountId.Should().BeNull();
        result.Location.Should().BeNull();
    }

    [Fact]
    public void Parse_BareNumber_SingleDigit()
    {
        var result = NotamNumberParser.Parse("1");

        result.Should().NotBeNull();
        result!.Number.Should().Be("1");
    }

    #endregion

    #region Number/year

    [Fact]
    public void Parse_NumberWithFourDigitYear()
    {
        var result = NotamNumberParser.Parse("3997/2025");

        result.Should().NotBeNull();
        result!.Number.Should().Be("3997");
        result.Year.Should().Be("2025");
    }

    [Fact]
    public void Parse_NumberWithTwoDigitYear()
    {
        var result = NotamNumberParser.Parse("3997/25");

        result.Should().NotBeNull();
        result!.Number.Should().Be("3997");
        result.Year.Should().Be("2025");
    }

    [Fact]
    public void Parse_NumberWithTwoDigitYear_GreaterThan12()
    {
        var result = NotamNumberParser.Parse("420/24");

        result.Should().NotBeNull();
        result!.Number.Should().Be("420");
        result.Year.Should().Be("2024");
    }

    #endregion

    #region Month-prefix

    [Fact]
    public void Parse_MonthPrefix_ShouldStripMonth()
    {
        var result = NotamNumberParser.Parse("03/420");

        result.Should().NotBeNull();
        result!.Number.Should().Be("420");
        result.Year.Should().BeNull();
    }

    [Fact]
    public void Parse_MonthPrefix_SingleDigitMonth()
    {
        var result = NotamNumberParser.Parse("8/123");

        result.Should().NotBeNull();
        result!.Number.Should().Be("123");
    }

    #endregion

    #region Account prefix

    [Fact]
    public void Parse_AccountPrefix_WithBareNumber()
    {
        var result = NotamNumberParser.Parse("BNA 420");

        result.Should().NotBeNull();
        result!.Number.Should().Be("420");
        result.AccountId.Should().Be("BNA");
        result.Location.Should().BeNull();
    }

    [Fact]
    public void Parse_AccountPrefix_WithMonthPrefix()
    {
        var result = NotamNumberParser.Parse("BNA 03/420");

        result.Should().NotBeNull();
        result!.Number.Should().Be("420");
        result.AccountId.Should().Be("BNA");
    }

    [Fact]
    public void Parse_AccountPrefix_WithLocation()
    {
        var result = NotamNumberParser.Parse("BNA 03/420 JWN");

        result.Should().NotBeNull();
        result!.Number.Should().Be("420");
        result.AccountId.Should().Be("BNA");
        result.Location.Should().Be("JWN");
    }

    [Fact]
    public void Parse_AccountPrefix_FDC()
    {
        var result = NotamNumberParser.Parse("FDC 4/3997");

        result.Should().NotBeNull();
        result!.Number.Should().Be("3997");
        result.AccountId.Should().Be("FDC");
    }

    [Fact]
    public void Parse_AccountPrefix_WithBang()
    {
        var result = NotamNumberParser.Parse("!BNA 03/420");

        result.Should().NotBeNull();
        result!.Number.Should().Be("420");
        result.AccountId.Should().Be("BNA");
    }

    [Fact]
    public void Parse_AccountPrefix_FDC_WithBangAndLocation()
    {
        var result = NotamNumberParser.Parse("!FDC 4/3997 JWN");

        result.Should().NotBeNull();
        result!.Number.Should().Be("3997");
        result.AccountId.Should().Be("FDC");
        result.Location.Should().Be("JWN");
    }

    [Fact]
    public void Parse_AccountPrefix_CaseInsensitive()
    {
        var result = NotamNumberParser.Parse("bna 420");

        result.Should().NotBeNull();
        result!.AccountId.Should().Be("BNA");
        result.Number.Should().Be("420");
    }

    #endregion

    #region ICAO format

    [Fact]
    public void Parse_IcaoFormat_TwoDigitYear()
    {
        var result = NotamNumberParser.Parse("A1234/25");

        result.Should().NotBeNull();
        result!.Number.Should().Be("1234");
        result.Year.Should().Be("2025");
        result.AccountId.Should().BeNull();
    }

    [Fact]
    public void Parse_IcaoFormat_FourDigitYear()
    {
        var result = NotamNumberParser.Parse("B5678/2024");

        result.Should().NotBeNull();
        result!.Number.Should().Be("5678");
        result.Year.Should().Be("2024");
    }

    #endregion

    #region Edge cases

    [Fact]
    public void Parse_Null_ShouldReturnNull()
    {
        NotamNumberParser.Parse(null).Should().BeNull();
    }

    [Fact]
    public void Parse_Empty_ShouldReturnNull()
    {
        NotamNumberParser.Parse("").Should().BeNull();
    }

    [Fact]
    public void Parse_Whitespace_ShouldReturnNull()
    {
        NotamNumberParser.Parse("   ").Should().BeNull();
    }

    [Fact]
    public void Parse_OnlyBang_ShouldReturnNull()
    {
        NotamNumberParser.Parse("!").Should().BeNull();
    }

    [Fact]
    public void Parse_WhitespacePadding_ShouldTrim()
    {
        var result = NotamNumberParser.Parse("  3997  ");

        result.Should().NotBeNull();
        result!.Number.Should().Be("3997");
    }

    #endregion
}
