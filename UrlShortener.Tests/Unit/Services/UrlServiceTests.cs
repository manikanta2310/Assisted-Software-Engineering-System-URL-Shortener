using Shouldly;
using UrlShortener.Application.Common;
using UrlShortener.Application;
using UrlShortener.Application.DTOs;
using UrlShortener.Application.Services;
using UrlShortener.Domain.Entities;
using UrlShortener.Domain.Exceptions;
using UrlShortener.UnitTests.TestDoubles;
using Xunit;

namespace UrlShortener.UnitTests.Services;

public sealed class UrlServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static UrlService CreateService(
        InMemoryUrlRepository repository,
        SequenceCodeGenerator generator,
        UrlShortenerOptions? options = null) =>
        new(repository, generator, new FakeClock(Now),
            options ?? new UrlShortenerOptions { MaxGenerationRetries = 5 });

    [Fact]
    public async Task CreateAsync_generates_code_when_no_alias_is_given()
    {
        var repository = new InMemoryUrlRepository();
        var service = CreateService(repository, new SequenceCodeGenerator("gen0001"));

        var result = await service.CreateAsync(
            new CreateShortUrlRequest { LongUrl = "https://example.com" }, CancellationToken.None);

        result.Status.ShouldBe(CreateStatus.Created);
        result.Response!.Code.ShouldBe("gen0001");
        repository.Store.ContainsKey("gen0001").ShouldBeTrue();
    }

    [Fact]
    public async Task CreateAsync_uses_the_custom_alias_when_provided()
    {
        var repository = new InMemoryUrlRepository();
        var service = CreateService(repository, new SequenceCodeGenerator());

        var result = await service.CreateAsync(
            new CreateShortUrlRequest { LongUrl = "https://example.com", CustomAlias = "promo2026" },
            CancellationToken.None);

        result.Status.ShouldBe(CreateStatus.Created);
        result.Response!.Code.ShouldBe("promo2026");
    }

    [Fact]
    public async Task CreateAsync_returns_alias_taken_when_the_alias_exists()
    {
        var repository = new InMemoryUrlRepository();
        repository.Seed(ShortUrl.Create("taken", "https://other.com", Now, null));
        var service = CreateService(repository, new SequenceCodeGenerator());

        var result = await service.CreateAsync(
            new CreateShortUrlRequest { LongUrl = "https://example.com", CustomAlias = "taken" },
            CancellationToken.None);

        result.Status.ShouldBe(CreateStatus.AliasAlreadyTaken);
        result.Response.ShouldBeNull();
    }

    [Fact]
    public async Task CreateAsync_retries_on_collision_then_succeeds()
    {
        var repository = new InMemoryUrlRepository();
        repository.Seed(ShortUrl.Create("dup0001", "https://other.com", Now, null));
        var service = CreateService(repository, new SequenceCodeGenerator("dup0001", "fresh01"));

        var result = await service.CreateAsync(
            new CreateShortUrlRequest { LongUrl = "https://example.com" }, CancellationToken.None);

        result.Response!.Code.ShouldBe("fresh01");
    }

    [Fact]
    public async Task CreateAsync_throws_when_generation_is_exhausted()
    {
        var repository = new InMemoryUrlRepository();
        repository.Seed(ShortUrl.Create("dup", "https://other.com", Now, null));
        var options = new UrlShortenerOptions { MaxGenerationRetries = 3 };
        var service = CreateService(repository, new SequenceCodeGenerator("dup", "dup", "dup"), options);

        await Should.ThrowAsync<ShortCodeGenerationException>(() =>
            service.CreateAsync(
                new CreateShortUrlRequest { LongUrl = "https://example.com" }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_applies_the_requested_expiry()
    {
        var repository = new InMemoryUrlRepository();
        var service = CreateService(repository, new SequenceCodeGenerator("gen0001"));

        var result = await service.CreateAsync(
            new CreateShortUrlRequest { LongUrl = "https://example.com", ExpiresInDays = 10 },
            CancellationToken.None);

        result.Response!.ExpiresAt.ShouldBe(Now.AddDays(10));
    }

    [Fact]
    public async Task ResolveAsync_returns_found_for_a_live_link()
    {
        var repository = new InMemoryUrlRepository();
        repository.Seed(ShortUrl.Create("live", "https://example.com", Now, null));
        var service = CreateService(repository, new SequenceCodeGenerator());

        var result = await service.ResolveAsync("live", CancellationToken.None);

        result.Status.ShouldBe(ResolveStatus.Found);
        result.LongUrl.ShouldBe("https://example.com");
    }

    [Fact]
    public async Task ResolveAsync_returns_not_found_for_an_unknown_code()
    {
        var repository = new InMemoryUrlRepository();
        var service = CreateService(repository, new SequenceCodeGenerator());

        var result = await service.ResolveAsync("missing", CancellationToken.None);

        result.Status.ShouldBe(ResolveStatus.NotFound);
    }

    [Fact]
    public async Task ResolveAsync_returns_expired_for_an_expired_link()
    {
        var repository = new InMemoryUrlRepository();
        repository.Seed(ShortUrl.Create("old", "https://example.com", Now.AddDays(-10), Now.AddDays(-1)));
        var service = CreateService(repository, new SequenceCodeGenerator());

        var result = await service.ResolveAsync("old", CancellationToken.None);

        result.Status.ShouldBe(ResolveStatus.Expired);
    }
}
