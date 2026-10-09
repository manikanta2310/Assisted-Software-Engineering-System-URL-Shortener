namespace UrlShortener.Domain;

/// <summary>
/// Rules that define what a valid short code / custom alias looks like — the
/// allowed alphabet, length bounds, and reserved words. Centralised so the code
/// generator and the validator share one source of truth, and so the rules can
/// be unit-tested with no infrastructure.
/// </summary>
public static class ShortCodeRules
{
    /// <summary>Base62 alphabet: digits, upper, lower. No look-alike-sensitive trimming
    /// is applied here — randomness + length make collisions negligible.</summary>
    public const string Alphabet =
        "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public const int MinLength = 4;
    public const int MaxLength = 32;

    /// <summary>
    /// Route prefixes and system paths that must never be handed out as a code
    /// or accepted as a custom alias, or they would shadow real endpoints.
    /// </summary>
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "api", "health", "healthz", "openapi", "swagger", "metrics",
        "admin", "static", "assets", "favicon.ico", "robots.txt",
    };

    /// <summary>Is this string made only of allowed Base62 characters?</summary>
    public static bool HasValidCharacters(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        foreach (var c in value)
        {
            if (Alphabet.IndexOf(c) < 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Is this a reserved word that must not be used as a code/alias?</summary>
    public static bool IsReserved(string value) => Reserved.Contains(value);

    /// <summary>
    /// Full validity check for a custom alias supplied by a user: correct length,
    /// allowed characters, and not reserved.
    /// </summary>
    public static bool IsValidAlias(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length >= MinLength
        && value.Length <= MaxLength
        && HasValidCharacters(value)
        && !IsReserved(value);
}
