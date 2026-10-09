using Microsoft.AspNetCore.Mvc;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.Common;

namespace UrlShortener.Api.Controllers;

/// <summary>
/// The redirect hot path: resolve a code and 302 to the target. This is the
/// highest-impact behaviour in the system, so the logic is deliberately minimal
/// and click tracking is fired off-path so it can never delay or break a redirect.
/// </summary>
[ApiController]
public sealed class RedirectController : ControllerBase
{
    private readonly IUrlService _urlService;
    private readonly IClickTracker _clickTracker;
    private readonly IClock _clock;

    public RedirectController(IUrlService urlService, IClickTracker clickTracker, IClock clock)
    {
        _urlService = urlService;
        _clickTracker = clickTracker;
        _clock = clock;
    }

    [HttpGet("/{code}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> RedirectToTarget(string code, CancellationToken cancellationToken)
    {
        var result = await _urlService.ResolveAsync(code, cancellationToken);

        switch (result.Status)
        {
            case ResolveStatus.Found:
                FireClickTracking(code);
                return Redirect(result.LongUrl!);

            case ResolveStatus.Expired:
                return StatusCode(StatusCodes.Status410Gone);

            case ResolveStatus.NotFound:
            default:
                return NotFound();
        }
    }

    // Fire-and-forget: the tracker owns its own scope and swallows its own errors.
    private void FireClickTracking(string code)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var referer = Request.Headers.Referer.ToString();
        var record = new ClickRecord(
            code,
            _clock.UtcNow,
            ip,
            string.IsNullOrEmpty(referer) ? null : referer);

        _ = _clickTracker.TrackAsync(record, CancellationToken.None);
    }
}
