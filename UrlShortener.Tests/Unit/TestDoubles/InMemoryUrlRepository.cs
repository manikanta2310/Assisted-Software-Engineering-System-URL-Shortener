using UrlShortener.Application.Interfaces;
using UrlShortener.Application.DTOs;
using UrlShortener.Domain.Entities;

namespace UrlShortener.UnitTests.TestDoubles;

/// <summary>A dictionary-backed repository for fast, infrastructure-free service tests.</summary>
internal sealed class InMemoryUrlRepository : IUrlRepository
{
    private readonly Dictionary<string, ShortUrl> _store = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, ShortUrl> Store => _store;

    public void Seed(ShortUrl shortUrl) => _store[shortUrl.Code] = shortUrl;

    public Task<ShortUrl?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(_store.GetValueOrDefault(code));

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(_store.ContainsKey(code));

    public Task AddAsync(ShortUrl shortUrl, CancellationToken cancellationToken)
    {
        _store[shortUrl.Code] = shortUrl;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DailyClickCount>> GetDailyClickCountsAsync(
        string code, DateTimeOffset since, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DailyClickCount>>([]);
}
