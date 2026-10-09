using UrlShortener.Application.Interfaces;
using UrlShortener.Application.DTOs;

namespace UrlShortener.Application.Services;

/// <summary>
/// Read-side analytics. Combines the aggregate counters stored on the link with
/// a recent daily time-series pulled from the click-event store.
/// </summary>
public sealed class AnalyticsService : IAnalyticsService
{
    private const int RecentWindowDays = 7;

    private readonly IUrlRepository _repository;
    private readonly IClock _clock;

    public AnalyticsService(IUrlRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<UrlStatsResponse?> GetStatsAsync(string code, CancellationToken cancellationToken)
    {
        var shortUrl = await _repository.GetByCodeAsync(code, cancellationToken);
        if (shortUrl is null)
        {
            return null;
        }

        var now = _clock.UtcNow;
        var since = now.AddDays(-RecentWindowDays);
        var clicksByDay = await _repository.GetDailyClickCountsAsync(code, since, cancellationToken);

        return new UrlStatsResponse
        {
            Code = shortUrl.Code,
            LongUrl = shortUrl.LongUrl,
            TotalClicks = shortUrl.ClickCount,
            CreatedAt = shortUrl.CreatedAt,
            ExpiresAt = shortUrl.ExpiresAt,
            LastAccessedAt = shortUrl.LastAccessedAt,
            IsExpired = shortUrl.IsExpired(now),
            ClicksByDay = clicksByDay,
        };
    }
}
