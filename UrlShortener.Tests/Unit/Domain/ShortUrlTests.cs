using Shouldly;
using UrlShortener.Domain.Entities;
using Xunit;

namespace UrlShortener.UnitTests.Domain;

public sealed class ShortUrlTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_sets_core_properties()
    {
        var url = ShortUrl.Create("abc123", "https://example.com", Now, null);

        url.Code.ShouldBe("abc123");
        url.LongUrl.ShouldBe("https://example.com");
        url.CreatedAt.ShouldBe(Now);
        url.ExpiresAt.ShouldBeNull();
        url.ClickCount.ShouldBe(0);
    }

    [Fact]
    public void Create_throws_when_expiry_is_not_in_the_future()
    {
        Should.Throw<ArgumentException>(() =>
            ShortUrl.Create("abc123", "https://example.com", Now, Now));
    }

    [Fact]
    public void IsExpired_is_false_when_there_is_no_expiry()
    {
        var url = ShortUrl.Create("abc123", "https://example.com", Now, null);

        url.IsExpired(Now.AddYears(10)).ShouldBeFalse();
    }

    [Fact]
    public void IsExpired_is_true_after_the_expiry_instant()
    {
        var url = ShortUrl.Create("abc123", "https://example.com", Now, Now.AddDays(1));

        url.IsExpired(Now.AddDays(2)).ShouldBeTrue();
    }

    [Fact]
    public void IsExpired_is_false_before_the_expiry_instant()
    {
        var url = ShortUrl.Create("abc123", "https://example.com", Now, Now.AddDays(5));

        url.IsExpired(Now.AddDays(1)).ShouldBeFalse();
    }

    [Fact]
    public void RegisterClick_increments_count_and_records_last_access()
    {
        var url = ShortUrl.Create("abc123", "https://example.com", Now, null);

        url.RegisterClick(Now.AddHours(1));

        url.ClickCount.ShouldBe(1);
        url.LastAccessedAt.ShouldBe(Now.AddHours(1));
    }
}
