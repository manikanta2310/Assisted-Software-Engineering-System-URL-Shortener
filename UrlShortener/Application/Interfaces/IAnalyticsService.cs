using UrlShortener.Application.DTOs;

namespace UrlShortener.Application.Interfaces;

/// <summary>Read-side service for link analytics.</summary>
public interface IAnalyticsService
{
    /// <summary>Returns stats for a code, or null if the code does not exist.</summary>
    Task<UrlStatsResponse?> GetStatsAsync(string code, CancellationToken cancellationToken);
}
