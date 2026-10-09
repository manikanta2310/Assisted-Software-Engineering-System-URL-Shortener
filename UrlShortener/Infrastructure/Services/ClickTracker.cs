using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.Common;
using UrlShortener.Application;
using UrlShortener.Domain.Entities;
using UrlShortener.Infrastructure.Persistence;

namespace UrlShortener.Infrastructure.Services;

/// <summary>
/// Records clicks off the redirect hot path. Called fire-and-forget, so it owns
/// its own DI scope (the request scope may already be gone) and never throws into
/// the caller — any failure is logged and swallowed. The client IP is hashed
/// before storage so the raw address is never persisted (S8).
/// </summary>
public sealed class ClickTracker : IClickTracker
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;
    private readonly UrlShortenerOptions _options;
    private readonly ILogger<ClickTracker> _logger;

    public ClickTracker(
        IServiceScopeFactory scopeFactory,
        IMemoryCache cache,
        UrlShortenerOptions options,
        ILogger<ClickTracker> logger)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
        _options = options;
        _logger = logger;
    }

    public async Task TrackAsync(ClickRecord record, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var shortUrl = await db.ShortUrls
                .FirstOrDefaultAsync(u => u.Code == record.Code, cancellationToken);
            if (shortUrl is null)
            {
                return;
            }

            // The domain decides "what a click does"; infrastructure just persists it.
            shortUrl.RegisterClick(record.OccurredAt);
            db.ClickEvents.Add(ClickEvent.Create(
                record.Code, record.OccurredAt, HashIp(record.ClientIp), record.Referer));

            await db.SaveChangesAsync(cancellationToken);

            // Evict the cached copy so stats reflect the new count promptly.
            _cache.Remove(CacheKeys.ForCode(record.Code));
        }
        catch (Exception ex)
        {
            // Analytics must never break a redirect — log and move on.
            _logger.LogWarning(ex, "Failed to record click for code {Code}", record.Code);
        }
    }

    private string? HashIp(string? ip)
    {
        if (string.IsNullOrEmpty(ip))
        {
            return null;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(_options.IpHashSalt + ip));
        return Convert.ToHexString(bytes);
    }
}
