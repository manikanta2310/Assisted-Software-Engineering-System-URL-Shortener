using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Domain.Exceptions;

namespace UrlShortener.Api.Middleware;

/// <summary>
/// Centralised exception handling (.NET IExceptionHandler). Maps domain
/// exceptions to clean ProblemDetails responses and ensures 5xx responses never
/// leak stack traces or internals (threat S7). Every response carries a traceId
/// for correlation without disclosure.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            ShortCodeGenerationException => (StatusCodes.Status500InternalServerError, "Could not generate a unique code"),
            DomainException => (StatusCodes.Status400BadRequest, "Invalid request"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred"),
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception on {Path}", httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning("Handled domain exception on {Path}: {Message}",
                httpContext.Request.Path, exception.Message);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            // Never echo internal exception details on server errors.
            Detail = status >= StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred. See server logs."
                : exception.Message,
            Instance = httpContext.Request.Path,
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
