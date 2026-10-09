using Microsoft.Extensions.Caching.Memory;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.DTOs;
using UrlShortener.Domain.Entities;
using UrlShortener.Infrastructure.Persistence;

namespace UrlShortener.Infrastructure.Repositories;

/// <summary>
/// Decorator that adds a short-lived in-memory cache to the redirect lookup
/// (the hot path), without changing the underlying repository. Only GetByCode is
/// cached; writes and analytics pass straight through. The TTL is deliberately
/// short, and the click tracker evicts the entry on write, so stats stay close
/// to real time (trade-off R4 in docs/risks.md).
/// </summary>
public sealed class CachedUrlRepository : IUrlRepository
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    private readonly IUrlRepository _inner;
    private readonly IMemoryCache _cache;

    public CachedUrlRepository(IUrlRepository inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<ShortUrl?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var key = CacheKeys.ForCode(code);
        if (_cache.TryGetValue(key, out ShortUrl? cached))
        {
            return cached;
        }

        var result = await _inner.GetByCodeAsync(code, cancellationToken);
        if (result is not null)
        {
            _cache.Set(key, result, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheTtl,
            });
        }

        return result;
    }

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken) =>
        _inner.CodeExistsAsync(code, cancellationToken);

    public Task AddAsync(ShortUrl shortUrl, CancellationToken cancellationToken) =>
        _inner.AddAsync(shortUrl, cancellationToken);

    public Task<IReadOnlyList<DailyClickCount>> GetDailyClickCountsAsync(
        string code, DateTimeOffset since, CancellationToken cancellationToken) =>
        _inner.GetDailyClickCountsAsync(code, since, cancellationToken);
}
