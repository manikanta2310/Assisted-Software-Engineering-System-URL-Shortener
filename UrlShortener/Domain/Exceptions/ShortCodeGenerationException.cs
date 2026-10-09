namespace UrlShortener.Domain.Exceptions;

/// <summary>
/// Raised when a unique short code could not be generated within the configured
/// number of retries. Extremely unlikely in practice; signals the keyspace is
/// saturated or something is wrong upstream. Maps to HTTP 500.
/// </summary>
public sealed class ShortCodeGenerationException : DomainException
{
    public ShortCodeGenerationException(int attempts)
        : base($"Failed to generate a unique short code after {attempts} attempts.")
    {
        Attempts = attempts;
    }

    public int Attempts { get; }
}
