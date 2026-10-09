namespace UrlShortener.Application.DTOs;

/// <summary>Number of clicks on a single calendar day (UTC).</summary>
public sealed record DailyClickCount(DateOnly Date, long Count);
