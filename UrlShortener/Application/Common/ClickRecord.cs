namespace UrlShortener.Application.Common;


public sealed record ClickRecord(string Code, DateTimeOffset OccurredAt, string? ClientIp, string? Referer);
