namespace UrlShortener.Application.Common;

public enum ResolveStatus
{
    Found,
    NotFound,
    Expired,
}

/// <summary>
/// Outcome of resolving a short code. Modelled as a result object rather than
/// exceptions, because "not found" and "expired" are expected control flow on
/// the hot redirect path, not exceptional conditions.
/// </summary>
public sealed record ResolveResult(ResolveStatus Status, string? LongUrl)
{
    public static ResolveResult Found(string longUrl) => new(ResolveStatus.Found, longUrl);

    public static readonly ResolveResult NotFound = new(ResolveStatus.NotFound, null);

    public static readonly ResolveResult Expired = new(ResolveStatus.Expired, null);
}
