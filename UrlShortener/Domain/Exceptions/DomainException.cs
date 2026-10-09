namespace UrlShortener.Domain.Exceptions;

/// <summary>
/// Base type for all domain-rule violations. The API's global exception handler
/// maps these to clean HTTP responses, so controllers never leak stack traces.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}
