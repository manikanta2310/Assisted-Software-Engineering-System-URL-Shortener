namespace UrlShortener.Infrastructure.Persistence;

/// <summary>Single source of truth for cache keys, shared by the cache decorator
/// and the click tracker (which evicts stale entries).</summary>
internal static class CacheKeys
{
    public static string ForCode(string code) => $"shorturl:{code}";
}
