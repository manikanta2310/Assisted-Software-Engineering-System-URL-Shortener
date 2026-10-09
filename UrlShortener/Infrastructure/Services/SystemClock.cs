using UrlShortener.Application.Interfaces;

namespace UrlShortener.Infrastructure.Services;

/// <summary>The real clock. Tests substitute a fixed-time fake.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
