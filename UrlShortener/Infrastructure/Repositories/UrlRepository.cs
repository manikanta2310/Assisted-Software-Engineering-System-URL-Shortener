using Microsoft.EntityFrameworkCore;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.DTOs;
using UrlShortener.Domain.Entities;
using UrlShortener.Infrastructure.Persistence;

namespace UrlShortener.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of the repository contract. Reads use AsNoTracking so
/// returned entities are detached and safe to cache. All queries are
/// parameterised by EF Core, which is our SQL-injection defence (S2).
/// </summary>
public sealed class UrlRepository : IUrlRepository
{
    private readonly AppDbContext _db;

    public UrlRepository(AppDbContext db) => _db = db;

    public async Task<ShortUrl?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        await _db.ShortUrls
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Code == code, cancellationToken);

    public async Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken) =>
        await _db.ShortUrls.AnyAsync(u => u.Code == code, cancellationToken);

    public async Task AddAsync(ShortUrl shortUrl, CancellationToken cancellationToken)
    {
        await _db.ShortUrls.AddAsync(shortUrl, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DailyClickCount>> GetDailyClickCountsAsync(
        string code, DateTimeOffset since, CancellationToken cancellationToken)
    {
        // Filter by code in SQL (translatable, index-backed). The DateTimeOffset
        // range filter and the daily grouping run in memory: the SQLite provider
        // cannot translate DateTimeOffset comparisons, and a single link's click
        // volume is small. A production (server) store would push both into SQL.
        var timestamps = await _db.ClickEvents
            .AsNoTracking()
            .Where(c => c.Code == code)
            .Select(c => c.OccurredAt)
            .ToListAsync(cancellationToken);

        return timestamps
            .Where(t => t >= since)
            .GroupBy(t => DateOnly.FromDateTime(t.UtcDateTime))
            .Select(g => new DailyClickCount(g.Key, g.LongCount()))
            .OrderBy(d => d.Date)
            .ToList();
    }
}
