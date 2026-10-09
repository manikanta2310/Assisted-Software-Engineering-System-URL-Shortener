using UrlShortener.Application.Interfaces;
using UrlShortener.Application.Common;
using UrlShortener.Application.DTOs;
using UrlShortener.Domain.Entities;
using UrlShortener.Domain.Exceptions;

namespace UrlShortener.Application.Services;

/// <summary>
/// Core orchestration for creating and resolving short URLs. Contains no HTTP or
/// database code — it coordinates domain objects through abstractions, which is
/// what keeps it unit-testable without any infrastructure.
/// </summary>
public sealed class UrlService : IUrlService
{
    private readonly IUrlRepository _repository;
    private readonly ICodeGenerator _codeGenerator;
    private readonly IClock _clock;
    private readonly UrlShortenerOptions _options;

    public UrlService(
        IUrlRepository repository,
        ICodeGenerator codeGenerator,
        IClock clock,
        UrlShortenerOptions options)
    {
        _repository = repository;
        _codeGenerator = codeGenerator;
        _clock = clock;
        _options = options;
    }

    public async Task<CreateUrlResult> CreateAsync(CreateShortUrlRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string code;
        if (!string.IsNullOrWhiteSpace(request.CustomAlias))
        {
            // A taken alias is an expected outcome, returned as a result (→ 409),
            // not thrown. This keeps expected conditions off the exception path.
            if (await _repository.CodeExistsAsync(request.CustomAlias, cancellationToken))
            {
                return CreateUrlResult.AliasAlreadyTaken;
            }

            code = request.CustomAlias;
        }
        else
        {
            code = await GenerateUniqueCodeAsync(cancellationToken);
        }

        var now = _clock.UtcNow;
        var expiresAt = ResolveExpiry(request.ExpiresInDays, now);

        var shortUrl = ShortUrl.Create(code, request.LongUrl, now, expiresAt);
        await _repository.AddAsync(shortUrl, cancellationToken);

        return CreateUrlResult.Created(ToResponse(shortUrl));
    }

    public async Task<ResolveResult> ResolveAsync(string code, CancellationToken cancellationToken)
    {
        var shortUrl = await _repository.GetByCodeAsync(code, cancellationToken);
        if (shortUrl is null)
        {
            return ResolveResult.NotFound;
        }

        return shortUrl.IsExpired(_clock.UtcNow)
            ? ResolveResult.Expired
            : ResolveResult.Found(shortUrl.LongUrl);
    }

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= _options.MaxGenerationRetries; attempt++)
        {
            var candidate = _codeGenerator.Generate();
            if (!await _repository.CodeExistsAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new ShortCodeGenerationException(_options.MaxGenerationRetries);
    }

    private DateTimeOffset? ResolveExpiry(int? requestedDays, DateTimeOffset now)
    {
        if (requestedDays.HasValue)
        {
            return now.AddDays(requestedDays.Value);
        }

        return _options.DefaultExpiryDays.HasValue
            ? now.AddDays(_options.DefaultExpiryDays.Value)
            : null;
    }

    private ShortUrlResponse ToResponse(ShortUrl shortUrl) => new()
    {
        Code = shortUrl.Code,
        ShortUrl = BuildShortUrl(shortUrl.Code),
        LongUrl = shortUrl.LongUrl,
        CreatedAt = shortUrl.CreatedAt,
        ExpiresAt = shortUrl.ExpiresAt,
    };

    private string BuildShortUrl(string code) =>
        string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? $"/{code}"
            : $"{_options.BaseUrl.TrimEnd('/')}/{code}";
}
