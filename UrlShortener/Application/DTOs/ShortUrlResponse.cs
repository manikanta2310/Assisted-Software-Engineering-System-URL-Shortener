namespace UrlShortener.Application.DTOs;

/// <summary>Returned when a short URL is created or looked up.</summary>
public sealed record ShortUrlResponse
{
    public required string Code { get; init; }

    /// <summary>The full short URL a user would share.</summary>
    public required string ShortUrl { get; init; }

    public required string LongUrl { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }
}
