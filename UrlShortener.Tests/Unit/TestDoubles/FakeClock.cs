using UrlShortener.Application.Interfaces;

namespace UrlShortener.UnitTests.TestDoubles;

/// <summary>A clock frozen at a fixed instant, for deterministic expiry tests.</summary>
internal sealed class FakeClock : IClock
{
    public FakeClock(DateTimeOffset now) => UtcNow = now;

    public DateTimeOffset UtcNow { get; set; }
}
