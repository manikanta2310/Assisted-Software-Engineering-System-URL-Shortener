using Shouldly;
using UrlShortener.Application;
using UrlShortener.Application.DTOs;
using UrlShortener.Application.Validation;
using Xunit;

namespace UrlShortener.UnitTests.Validation;

public sealed class CreateShortUrlRequestValidatorTests
{
    private static bool IsValid(CreateShortUrlRequest request)
    {
        var validator = new CreateShortUrlRequestValidator(new UrlShortenerOptions { MaxUrlLength = 2048 });
        return validator.Validate(request).IsValid;
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com/path?q=1")]
    [InlineData("https://sub.domain.example.co.uk/a/b/c")]
    public void Accepts_valid_http_urls(string url) =>
        IsValid(new CreateShortUrlRequest { LongUrl = url }).ShouldBeTrue();

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com")]
    [InlineData("not a url")]
    [InlineData("")]
    public void Rejects_non_http_or_malformed_urls(string url) =>
        IsValid(new CreateShortUrlRequest { LongUrl = url }).ShouldBeFalse();

    [Theory]
    [InlineData("http://localhost/admin")]
    [InlineData("http://127.0.0.1")]
    [InlineData("http://192.168.0.5")]
    [InlineData("http://10.0.0.1")]
    [InlineData("http://169.254.1.1")]
    public void Rejects_internal_hosts(string url) =>
        IsValid(new CreateShortUrlRequest { LongUrl = url }).ShouldBeFalse();

    [Fact]
    public void Rejects_url_over_max_length()
    {
        var longUrl = "https://example.com/" + new string('a', 3000);

        IsValid(new CreateShortUrlRequest { LongUrl = longUrl }).ShouldBeFalse();
    }

    [Theory]
    [InlineData("admin")]      // reserved word
    [InlineData("ab")]         // too short
    [InlineData("has space")]  // invalid character
    [InlineData("bad/slash")]  // invalid character
    public void Rejects_invalid_aliases(string alias) =>
        IsValid(new CreateShortUrlRequest { LongUrl = "https://example.com", CustomAlias = alias })
            .ShouldBeFalse();

    [Fact]
    public void Accepts_valid_alias() =>
        IsValid(new CreateShortUrlRequest { LongUrl = "https://example.com", CustomAlias = "myLink7" })
            .ShouldBeTrue();

    [Fact]
    public void Rejects_negative_expiry() =>
        IsValid(new CreateShortUrlRequest { LongUrl = "https://example.com", ExpiresInDays = -1 })
            .ShouldBeFalse();
}
