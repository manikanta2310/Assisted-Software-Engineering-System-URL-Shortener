using UrlShortener.Application.DTOs;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Interfaces;

/// <summary>
/// Persistence contract for short URLs. Defined in the Application layer and
/// implemented in Infrastructure (Dependency Inversion) so the core never
/// depends on EF Core or any specific database.
/// </summary>
public interface IUrlRepository
{
    Task<ShortUrl?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken);

    Task AddAsync(ShortUrl shortUrl, CancellationToken cancellationToken);

    /// <summary>
    /// Daily click counts for a code since the given instant, used by analytics.
    /// </summary>
    Task<IReadOnlyList<DailyClickCount>> GetDailyClickCountsAsync(
        string code, DateTimeOffset since, CancellationToken cancellationToken);
}
