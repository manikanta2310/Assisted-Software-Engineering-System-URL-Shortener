using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UrlShortener.Application.Common;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application;
using UrlShortener.Application.DTOs;

namespace UrlShortener.Api.Controllers;

/// <summary>
/// Create and inspect short URLs. Controllers stay thin: validate, delegate to a
/// service, shape the response. No business logic lives here.
/// </summary>
[ApiController]
[Route("api/urls")]
public sealed class UrlsController : ControllerBase
{
    private readonly IUrlService _urlService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IValidator<CreateShortUrlRequest> _validator;
    private readonly UrlShortenerOptions _options;

    public UrlsController(
        IUrlService urlService,
        IAnalyticsService analyticsService,
        IValidator<CreateShortUrlRequest> validator,
        UrlShortenerOptions options)
    {
        _urlService = urlService;
        _analyticsService = analyticsService;
        _validator = validator;
        _options = options;
    }

    /// <summary>Create a short URL.</summary>
    [HttpPost]
    [EnableRateLimiting("create")]
    [RequestSizeLimit(16 * 1024)]
    [ProducesResponseType(typeof(ShortUrlResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateShortUrlRequest request, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        var result = await _urlService.CreateAsync(request, cancellationToken);
        if (result.Status == CreateStatus.AliasAlreadyTaken)
        {
            return Problem(
                detail: $"The alias '{request.CustomAlias}' is already taken.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Alias already in use");
        }

        var response = EnsureAbsoluteShortUrl(result.Response!);
        return CreatedAtAction(nameof(GetStats), new { code = response.Code }, response);
    }

    /// <summary>Get analytics for a short URL.</summary>
    [HttpGet("{code}/stats")]
    [ProducesResponseType(typeof(UrlStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStats(string code, CancellationToken cancellationToken)
    {
        var stats = await _analyticsService.GetStatsAsync(code, cancellationToken);
        return stats is null ? NotFound() : Ok(stats);
    }

    // When no BaseUrl is configured, compose an absolute short URL from the
    // request origin so the response is directly usable.
    private ShortUrlResponse EnsureAbsoluteShortUrl(ShortUrlResponse response)
    {
        if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            return response;
        }

        var absolute = $"{Request.Scheme}://{Request.Host}/{response.Code}";
        return response with { ShortUrl = absolute };
    }
}
