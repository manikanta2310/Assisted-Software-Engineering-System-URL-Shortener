using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using UrlShortener.Application.DTOs;
using Xunit;

namespace UrlShortener.IntegrationTests;

public sealed class UrlsApiTests : IClassFixture<CustomWebAppFactory>
{
    private readonly HttpClient _client;

    public UrlsApiTests(CustomWebAppFactory factory) =>
        // Do not auto-follow the 302, or the test would chase the external target.
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Create_then_redirect_then_stats_round_trip()
    {
        var create = await _client.PostAsJsonAsync("/api/urls",
            new { longUrl = "https://example.com/docs", expiresInDays = 30 });
        create.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await create.Content.ReadFromJsonAsync<ShortUrlResponse>();
        created.ShouldNotBeNull();
        created!.Code.ShouldNotBeNullOrWhiteSpace();

        var redirect = await _client.GetAsync($"/{created.Code}");
        redirect.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        redirect.Headers.Location!.ToString().ShouldBe("https://example.com/docs");

        var stats = await _client.GetFromJsonAsync<UrlStatsResponse>($"/api/urls/{created.Code}/stats");
        stats.ShouldNotBeNull();
        stats!.Code.ShouldBe(created.Code);
    }

    [Fact]
    public async Task Create_with_invalid_url_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/urls",
            new { longUrl = "javascript:alert(1)" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Redirect_to_unknown_code_returns_404()
    {
        var response = await _client.GetAsync("/nope404");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Duplicate_alias_returns_409()
    {
        var first = await _client.PostAsJsonAsync("/api/urls",
            new { longUrl = "https://example.com/a", customAlias = "dupealias" });
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        var second = await _client.PostAsJsonAsync("/api/urls",
            new { longUrl = "https://example.com/b", customAlias = "dupealias" });
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Health_endpoint_returns_200()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
