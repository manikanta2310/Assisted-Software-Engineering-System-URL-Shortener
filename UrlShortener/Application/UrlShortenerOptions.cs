namespace UrlShortener.Application;

/// <summary>
/// Strongly-typed settings bound from the "UrlShortener" configuration section.
/// Using the Options pattern keeps magic numbers out of the code and makes the
/// behaviour configurable without recompiling.
/// </summary>
public sealed class UrlShortenerOptions
{
    public const string SectionName = "UrlShortener";

    /// <summary>
    /// Absolute base used to build the returned short URL, e.g. "https://sho.rt".
    /// If left blank, the API fills it from the incoming request's origin.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Length of generated Base62 codes.</summary>
    public int CodeLength { get; set; } = 7;

    /// <summary>How many times to retry code generation on a collision.</summary>
    public int MaxGenerationRetries { get; set; } = 5;

    /// <summary>Longest target URL we accept (characters).</summary>
    public int MaxUrlLength { get; set; } = 2048;

    /// <summary>Default link lifetime in days. Null means links never expire.</summary>
    public int? DefaultExpiryDays { get; set; }

    /// <summary>Salt used when hashing client IPs for analytics (privacy, S8).</summary>
    public string IpHashSalt { get; set; } = string.Empty;
}
