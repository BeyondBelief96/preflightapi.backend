using FluentAssertions;
using PreflightApi.Infrastructure.Utilities;
using Xunit;

namespace PreflightApi.Tests.Utilities;

public class AirportIdentifierResolverTests
{
    // --- GetCandidateIdentifiers ---

    [Theory]
    [InlineData("KDFW", new[] { "KDFW", "DFW" })]
    [InlineData("KW05", new[] { "KW05", "W05" })]
    [InlineData("K9D4", new[] { "K9D4", "9D4" })]
    [InlineData("K9G8", new[] { "K9G8", "9G8" })]
    public void GetCandidateIdentifiers_KPrefix_ReturnsOriginalAndStripped(string input, string[] expected)
    {
        var result = AirportIdentifierResolver.GetCandidateIdentifiers(input);
        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("PA88", new[] { "PA88", "A88" })]
    [InlineData("PAFA", new[] { "PAFA", "AFA" })]
    [InlineData("PHNL", new[] { "PHNL", "HNL" })]
    [InlineData("PGUM", new[] { "PGUM", "GUM" })]
    public void GetCandidateIdentifiers_PPrefix_ReturnsOriginalAndStripped(string input, string[] expected)
    {
        var result = AirportIdentifierResolver.GetCandidateIdentifiers(input);
        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("DFW", new[] { "DFW", "KDFW" })]
    [InlineData("W05", new[] { "W05", "KW05" })]
    [InlineData("9D4", new[] { "9D4", "K9D4" })]
    [InlineData("BNA", new[] { "BNA", "KBNA" })]
    public void GetCandidateIdentifiers_ShortCode_ReturnsOriginalAndKPrefixed(string input, string[] expected)
    {
        var result = AirportIdentifierResolver.GetCandidateIdentifiers(input);
        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("A88", new[] { "A88", "KA88" })]
    [InlineData("HNL", new[] { "HNL", "KHNL" })]
    public void GetCandidateIdentifiers_ShortAlaskaHawaii_AddsKPrefix(string input, string[] expected)
    {
        // Short codes for Alaska/Hawaii airports get K prefix as a candidate.
        // The K-prefixed version won't match anything in the DB (since Alaska uses PA, Hawaii uses PH),
        // but it's harmless and keeps the logic simple.
        var result = AirportIdentifierResolver.GetCandidateIdentifiers(input);
        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("X")]
    public void GetCandidateIdentifiers_VeryShortCode_AddsKPrefix(string input)
    {
        var result = AirportIdentifierResolver.GetCandidateIdentifiers(input);
        result.Should().Contain(input.ToUpperInvariant());
        result.Should().Contain("K" + input.ToUpperInvariant());
        result.Should().HaveCount(2);
    }

    [Fact]
    public void GetCandidateIdentifiers_FiveCharCode_ReturnsOnlyOriginal()
    {
        // 5+ char codes don't match any known pattern — return as-is
        var result = AirportIdentifierResolver.GetCandidateIdentifiers("ABCDE");
        result.Should().ContainSingle().Which.Should().Be("ABCDE");
    }

    [Fact]
    public void GetCandidateIdentifiers_NonKPPrefix_FourChars_ReturnsOnlyOriginal()
    {
        // 4-char code not starting with K or P — no stripping
        var result = AirportIdentifierResolver.GetCandidateIdentifiers("LFPG");
        result.Should().ContainSingle().Which.Should().Be("LFPG");
    }

    [Theory]
    [InlineData("kdfw", "KDFW")]
    [InlineData("kw05", "KW05")]
    [InlineData("dfw", "DFW")]
    public void GetCandidateIdentifiers_LowercaseInput_UppercasedInOutput(string input, string expectedFirst)
    {
        var result = AirportIdentifierResolver.GetCandidateIdentifiers(input);
        result[0].Should().Be(expectedFirst);
        result.Should().AllSatisfy(c => c.Should().Be(c.ToUpperInvariant()));
    }

    [Theory]
    [InlineData(" KDFW ", "KDFW")]
    [InlineData("  W05  ", "W05")]
    public void GetCandidateIdentifiers_TrimsWhitespace(string input, string expectedFirst)
    {
        var result = AirportIdentifierResolver.GetCandidateIdentifiers(input);
        result[0].Should().Be(expectedFirst);
    }

    // --- ExpandCandidates ---

    [Fact]
    public void ExpandCandidates_MultipleInputs_ReturnsAllUniqueCandidates()
    {
        var inputs = new[] { "KW05", "KDFW", "PA88" };
        var result = AirportIdentifierResolver.ExpandCandidates(inputs);

        result.Should().Contain("KW05");
        result.Should().Contain("W05");
        result.Should().Contain("KDFW");
        result.Should().Contain("DFW");
        result.Should().Contain("PA88");
        result.Should().Contain("A88");
        result.Should().HaveCount(6);
    }

    [Fact]
    public void ExpandCandidates_OverlappingCandidates_Deduplicates()
    {
        // "DFW" expands to ["DFW", "KDFW"] and "KDFW" expands to ["KDFW", "DFW"]
        var inputs = new[] { "DFW", "KDFW" };
        var result = AirportIdentifierResolver.ExpandCandidates(inputs);

        result.Should().Contain("DFW");
        result.Should().Contain("KDFW");
        result.Should().HaveCount(2);
    }

    [Fact]
    public void ExpandCandidates_EmptyInput_ReturnsEmpty()
    {
        var result = AirportIdentifierResolver.ExpandCandidates(Array.Empty<string>());
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExpandCandidates_MixedFormats_ExpandsAll()
    {
        // Simulates a real batch request with mixed ICAO and FAA codes
        var inputs = new[] { "KDFW", "W05", "PA88", "9D4" };
        var result = AirportIdentifierResolver.ExpandCandidates(inputs);

        result.Should().Contain("KDFW").And.Contain("DFW");     // ICAO → FAA
        result.Should().Contain("W05").And.Contain("KW05");     // FAA → ICAO
        result.Should().Contain("PA88").And.Contain("A88");     // Alaska ICAO → FAA
        result.Should().Contain("9D4").And.Contain("K9D4");     // FAA → ICAO
    }
}
