using UrlShortener.Application.Common;
using UrlShortener.Application.DTOs;

namespace UrlShortener.Application.Interfaces;

/// <summary>
/// Orchestrates the core use cases: creating a short URL and resolving one for
/// redirect. This is where the "generate → check uniqueness → persist" and
/// "look up → check expiry" workflows live.
/// </summary>
public interface IUrlService
{
    Task<CreateUrlResult> CreateAsync(CreateShortUrlRequest request, CancellationToken cancellationToken);

    Task<ResolveResult> ResolveAsync(string code, CancellationToken cancellationToken);
}
