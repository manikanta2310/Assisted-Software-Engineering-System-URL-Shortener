namespace UrlShortener.Domain.Entities;

/// <summary>
/// One recorded click, kept for time-series analytics (clicks per day, etc.).
/// Privacy note: the client IP is stored only as a salted hash, never raw — see
/// the ambiguous-analytics scenario in docs/scenarios.md and threat S8.
/// </summary>
public sealed class ClickEvent
{
    private ClickEvent()
    {
        Code = string.Empty;
    }

    private ClickEvent(string code, DateTimeOffset occurredAt, string? ipHash, string? referer)
    {
        Code = code;
        OccurredAt = occurredAt;
        IpHash = ipHash;
        Referer = referer;
    }

    public long Id { get; private set; }

    /// <summary>The short code that was accessed.</summary>
    public string Code { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Salted hash of the client IP. Never the raw address.</summary>
    public string? IpHash { get; private set; }

    /// <summary>Referring URL, if the browser supplied one.</summary>
    public string? Referer { get; private set; }

    public static ClickEvent Create(string code, DateTimeOffset occurredAt, string? ipHash, string? referer) =>
        new(code, occurredAt, ipHash, referer);
}
