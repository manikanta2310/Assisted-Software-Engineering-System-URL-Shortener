namespace UrlShortener.Application.Interfaces;

/// <summary>
/// Abstraction over the system clock so time-dependent logic (expiry) can be
/// tested deterministically. The real implementation returns DateTimeOffset.UtcNow.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
