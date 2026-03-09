namespace PreflightApi.Infrastructure.Utilities;

/// <summary>
/// Resolves airport identifier format mismatches between ICAO and FAA identifier systems.
/// US airports have two identifier formats:
///   - FAA identifiers: typically 3-4 characters (e.g., DFW, W05, 9D4, A88)
///   - ICAO identifiers: always 4 characters with a regional prefix
///     - K = contiguous 48 US states (KDFW, KW05)
///     - PA = Alaska (PAFA, PA88)
///     - PH = Hawaii (PHNL)
///     - PG = Guam, PM = Midway, PW = Wake Island, etc.
///
/// Many small US airports only have FAA identifiers (no assigned ICAO code), but users
/// often try the ICAO format anyway (e.g., KW05 for W05). This resolver generates
/// candidate identifiers to try, enabling lookups regardless of which format the user provides.
/// </summary>
public static class AirportIdentifierResolver
{
    /// <summary>
    /// Returns candidate identifiers to try when looking up an airport.
    /// The first element is always the uppercased original input.
    /// </summary>
    public static List<string> GetCandidateIdentifiers(string input)
    {
        var upper = input.Trim().ToUpperInvariant();
        var candidates = new List<string> { upper };

        if (upper.Length == 4)
        {
            // K-prefix: contiguous US ICAO code → also try stripped FAA identifier
            // Examples: KW05 → W05, KDFW → DFW, K9D4 → 9D4
            if (upper[0] == 'K')
            {
                candidates.Add(upper[1..]);
            }
            // P-prefix: North Pacific ICAO code → also try stripped FAA identifier
            // Examples: PA88 → A88 (Alaska), PHNL → HNL (Hawaii), PGUM → GUM (Guam)
            else if (upper[0] == 'P')
            {
                candidates.Add(upper[1..]);
            }
        }
        else if (upper.Length <= 3)
        {
            // Short FAA identifier → also try with K prefix (contiguous US ICAO)
            // Examples: DFW → KDFW, W05 → KW05, 9D4 → K9D4
            candidates.Add("K" + upper);
        }

        return candidates;
    }

    /// <summary>
    /// Expands multiple identifiers into all candidate variations.
    /// Used for batch lookups to find airports regardless of identifier format.
    /// </summary>
    public static List<string> ExpandCandidates(IEnumerable<string> inputs)
    {
        var candidates = new HashSet<string>(StringComparer.Ordinal);
        foreach (var input in inputs)
        {
            foreach (var candidate in GetCandidateIdentifiers(input))
            {
                candidates.Add(candidate);
            }
        }

        return candidates.ToList();
    }
}
