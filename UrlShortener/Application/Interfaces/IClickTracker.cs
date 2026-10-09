using UrlShortener.Application.Common;

namespace UrlShortener.Application.Interfaces;

/// <summary>
/// Records a click for analytics: increments the link's counters and stores a
/// time-series event. Called fire-and-forget off the redirect hot path, so it
/// must never throw into the caller. The implementation hashes the client IP
/// before storage.
/// </summary>
public interface IClickTracker
{
    Task TrackAsync(ClickRecord record, CancellationToken cancellationToken);
}
