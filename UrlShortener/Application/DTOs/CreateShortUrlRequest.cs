namespace UrlShortener.Application.DTOs;

/// <summary>
/// Request to create a short URL. Validated by CreateShortUrlRequestValidator
/// before it reaches the service.
/// </summary>
public sealed record CreateShortUrlRequest
{
    /// <summary>The original long URL to shorten. Required.</summary>
    public string LongUrl { get; init; } = string.Empty;

    /// <summary>Optional custom alias. If omitted, a code is generated.</summary>
    public string? CustomAlias { get; init; }

    /// <summary>Optional link lifetime in days. If omitted, the configured default applies.</summary>
    public int? ExpiresInDays { get; init; }
}
