namespace UrlShortener.Domain.Entities;

/// <summary>
/// Aggregate root: one shortened link. State is guarded — callers cannot set an
/// invalid combination from outside. Business rules that belong to a single link
/// (expiry, click registration) live here as methods, not in services.
/// </summary>
public sealed class ShortUrl
{
    // Private parameterless constructor for EF Core materialisation only.
    private ShortUrl()
    {
        Code = string.Empty;
        LongUrl = string.Empty;
    }

    private ShortUrl(string code, string longUrl, DateTimeOffset createdAt, DateTimeOffset? expiresAt)
    {
        Code = code;
        LongUrl = longUrl;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    /// <summary>Database identity. Never exposed over the API.</summary>
    public long Id { get; private set; }

    /// <summary>The short code or custom alias. Unique. This is the public key.</summary>
    public string Code { get; private set; }

    /// <summary>The original (long) destination URL.</summary>
    public string LongUrl { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Null means the link never expires.</summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    public long ClickCount { get; private set; }

    public DateTimeOffset? LastAccessedAt { get; private set; }

    /// <summary>
    /// Factory: the only way to create a valid link. Keeps construction rules in
    /// one place and prevents an object from existing in an invalid state.
    /// </summary>
    public static ShortUrl Create(string code, string longUrl, DateTimeOffset createdAt, DateTimeOffset? expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(longUrl);

        if (expiresAt is not null && expiresAt <= createdAt)
        {
            throw new ArgumentException("Expiry must be in the future.", nameof(expiresAt));
        }

        return new ShortUrl(code, longUrl, createdAt, expiresAt);
    }

    /// <summary>Domain rule: is this link past its expiry at the given instant?</summary>
    public bool IsExpired(DateTimeOffset now) => ExpiresAt is not null && ExpiresAt <= now;

    /// <summary>
    /// Records a successful resolution. Kept on the entity so the "what counts as
    /// a click" rule stays with the data it mutates.
    /// </summary>
    public void RegisterClick(DateTimeOffset now)
    {
        ClickCount++;
        LastAccessedAt = now;
    }
}
