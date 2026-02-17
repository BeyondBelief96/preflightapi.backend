using System.Text.RegularExpressions;

namespace PreflightApi.Infrastructure.Services.NotamServices;

/// <summary>
/// Parses flexible NOTAM number input formats into structured components for database lookup.
/// Supported formats:
///   - Bare number: "3997"
///   - Number/year: "3997/2025" or "3997/25"
///   - Month-prefix: "03/420"
///   - Account prefix: "BNA 420", "BNA 03/420"
///   - Domestic format: "!BNA 03/420", "!BNA 03/420 JWN"
///   - FDC format: "FDC 4/3997", "!FDC 4/3997"
///   - ICAO format: "A1234/25"
/// </summary>
public static partial class NotamNumberParser
{
    /// <summary>
    /// Parses a flexible NOTAM number string into its components.
    /// Returns null if the input cannot be parsed.
    /// </summary>
    public static ParsedNotamNumber? Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var trimmed = input.Trim();

        // Strip leading "!" if present
        if (trimmed.StartsWith('!'))
            trimmed = trimmed[1..].TrimStart();

        if (trimmed.Length == 0)
            return null;

        // 1. ICAO format: letter prefix + digits / 2-or-4-digit year (e.g., A1234/25)
        var icaoMatch = IcaoRegex().Match(trimmed);
        if (icaoMatch.Success)
        {
            var series = icaoMatch.Groups[1].Value.ToUpperInvariant();
            var number = icaoMatch.Groups[2].Value;
            var yearStr = icaoMatch.Groups[3].Value;
            return new ParsedNotamNumber
            {
                Number = number,
                Year = NormalizeYear(yearStr),
                Series = series
            };
        }

        // 2. Account prefix format: 2-4 letter code followed by number (e.g., "BNA 420", "BNA 03/420", "FDC 4/3997 JWN")
        var accountMatch = AccountPrefixRegex().Match(trimmed);
        if (accountMatch.Success)
        {
            var accountId = accountMatch.Groups[1].Value.ToUpperInvariant();
            var numberPart = accountMatch.Groups[2].Value;
            var location = accountMatch.Groups[3].Success ? accountMatch.Groups[3].Value.ToUpperInvariant() : null;

            return new ParsedNotamNumber
            {
                Number = StripMonthPrefix(numberPart),
                AccountId = accountId,
                Location = location
            };
        }

        // 3. digits/digits — disambiguate month-prefix vs number/year
        var slashMatch = NumberYearRegex().Match(trimmed);
        if (slashMatch.Success)
        {
            var left = slashMatch.Groups[1].Value;
            var right = slashMatch.Groups[2].Value;

            // If left is 1-2 digits and value 1-12, treat as month prefix: mm/number → right is NOTAM number
            if (left.Length <= 2 && int.TryParse(left, out var leftVal) && leftVal >= 1 && leftVal <= 12)
            {
                return new ParsedNotamNumber { Number = right };
            }

            // Otherwise left is the NOTAM number and right is the year
            if (right.Length == 4 || (int.TryParse(right, out var rightVal) && rightVal > 12))
            {
                return new ParsedNotamNumber
                {
                    Number = left,
                    Year = NormalizeYear(right)
                };
            }

            // Ambiguous fallback: treat left as the number
            return new ParsedNotamNumber { Number = left };
        }

        // 4. Bare number (digits only)
        var bareMatch = BareNumberRegex().Match(trimmed);
        if (bareMatch.Success)
        {
            return new ParsedNotamNumber
            {
                Number = bareMatch.Groups[1].Value
            };
        }

        return null;
    }

    private static string StripMonthPrefix(string numberPart)
    {
        var match = MonthPrefixRegex().Match(numberPart);
        return match.Success ? match.Groups[1].Value : numberPart;
    }

    private static string? NormalizeYear(string yearStr)
    {
        if (yearStr.Length == 4)
            return yearStr;

        // 2-digit year: assume 2000s
        if (int.TryParse(yearStr, out var y))
            return (2000 + y).ToString();

        return yearStr;
    }

    // ICAO: single letter + digits / 2-or-4-digit year
    [GeneratedRegex(@"^([A-Za-z])(\d+)/(\d{2,4})$")]
    private static partial Regex IcaoRegex();

    // Account prefix: 2-4 letters, space, then number part, optional trailing location
    [GeneratedRegex(@"^([A-Za-z]{2,4})\s+(\d{1,2}/\d+|\d+)(?:\s+([A-Za-z]{2,4}))?$")]
    private static partial Regex AccountPrefixRegex();

    // Number/year: digits / digits
    [GeneratedRegex(@"^(\d+)/(\d{1,4})$")]
    private static partial Regex NumberYearRegex();

    // Month prefix: 1-2 digits / digits (for stripping)
    [GeneratedRegex(@"^\d{1,2}/(\d+)$")]
    private static partial Regex MonthPrefixRegex();

    // Bare number
    [GeneratedRegex(@"^(\d+)$")]
    private static partial Regex BareNumberRegex();
}

/// <summary>
/// Structured result from parsing a NOTAM number input.
/// </summary>
public record ParsedNotamNumber
{
    /// <summary>Bare sequence number (e.g., "420", "3997")</summary>
    public required string Number { get; init; }

    /// <summary>4-digit year if provided (e.g., "2025")</summary>
    public string? Year { get; init; }

    /// <summary>Accountability code if provided (e.g., "BNA", "FDC")</summary>
    public string? AccountId { get; init; }

    /// <summary>Location code if provided (e.g., "JWN")</summary>
    public string? Location { get; init; }

    /// <summary>ICAO series letter if provided (e.g., "A")</summary>
    public string? Series { get; init; }
}
