namespace UrlShortener.Application.DTOs;

/// <summary>Analytics for a single short URL.</summary>
public sealed record UrlStatsResponse
{
    public required string Code { get; init; }

    public required string LongUrl { get; init; }

    public required long TotalClicks { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }

    public DateTimeOffset? LastAccessedAt { get; init; }

    public required bool IsExpired { get; init; }

    /// <summary>Clicks per day for the recent window (time-series).</summary>
    public required IReadOnlyList<DailyClickCount> ClicksByDay { get; init; }
}
